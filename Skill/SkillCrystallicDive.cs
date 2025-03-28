using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystallicDive : SkillSpellPunch
{
    public delegate void OnDive();

    [ModOption("Dive Force", "Controls the force applied to creatures, multiplied by distance."), ModOptionCategory("Crystallic Dive", 6), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float force = 4f;

    [ModOption("Dive Angle", "Controls the hand angle to trigger a dive."), ModOptionCategory("Crystallic Dive", 6), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float maxAngle = 70f;

    [ModOption("Dive Radius", "Controls the radius of force applied to creatures."), ModOptionCategory("Crystallic Dive", 6), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float radius = 4f;

    public static SkillStatusPair active;
    public float distanceMult = 1f;
    public float fallDamageScale;
    protected float groundDistance;
    protected bool isDiving;
    public float maxHeightDistance = 3f;
    public float minDownwardVelocity = 5f;
    public float minHeight = 4f;
    public Vector2 minMaxShakeIntensity = new(0.005f, 0.01f);
    protected EffectInstance playerEffect;
    protected EffectData playerEffectData;
    public string playerEffectId;
    public bool shake = true;
    public AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0.0f, 1f, 1f, 0.0f);
    public float shakeDuration = 0.5f;
    public float shakeIntensity = 1f;
    public float shockwaveDamage = 30f;
    protected EffectData shockwaveEffectData;
    public string shockwaveEffectId;
    public int shockwavePushLevel = 2;
    public float upwardsModifier;
    protected EffectInstance whooshEffect;
    protected EffectData whooshEffectData;
    public static string spellId = "Body";
    public string whooshEffectId;
    public event OnDive OnDiveStop;

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        whooshEffect?.End();
    }

    public override void OnLateSkillsLoaded(SkillData skillData, Creature creature)
    {
        base.OnLateSkillsLoaded(skillData, creature);
        SkillThickSkin.SetRandomness(new Vector2(0, 3));
    }

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        shockwaveEffectData = Catalog.GetData<EffectData>(shockwaveEffectId);
        playerEffectData = Catalog.GetData<EffectData>(playerEffectId);
        whooshEffectData = Catalog.GetData<EffectData>(whooshEffectId);
    }

    public override void OnFist(PlayerHand hand, bool gripping)
    {
        if (!gripping || Player.currentCreature.airHelper.inAir) base.OnFist(hand, gripping);
        if (!gripping || !Player.currentCreature.airHelper.inAir || isDiving || !(hand.ragdollHand?.caster?.spellInstance is SpellCastCrystallic)) return;
        hand.ragdollHand.HapticTick(oneFrameCooldown: true);
    }

    public override void OnPunchStart(RagdollHand hand, Vector3 velocity)
    {
        if (!Player.currentCreature.airHelper.inAir || isDiving || !(hand.caster?.spellInstance is SpellCastCrystallic) || Vector3.Angle(velocity, Vector3.down) >= maxAngle) return;
        Player.local.locomotion.physicBody.AddForce(velocity * playerDashForce, ForceMode.VelocityChange);
        whooshEffect = whooshEffectData?.Spawn(hand.transform);
        whooshEffect?.Play();
        RaycastHit raycastHit;
        groundDistance = Player.local.locomotion.SphereCastGround(minHeight + maxHeightDistance, out raycastHit, out _) ? raycastHit.distance : minHeight + maxHeightDistance;
        Player.fallDamageScale.Add(this, fallDamageScale);
        Player.currentCreature.airHelper.OnGroundEvent -= OnGround;
        Player.currentCreature.airHelper.OnGroundEvent += OnGround;
        playerEffect?.End();
        playerEffect = playerEffectData?.Spawn(Player.local.transform.position - Vector3.up * 1.5f + Vector3.ProjectOnPlane(Player.local.head.transform.forward, Vector3.up).normalized, Quaternion.identity, Player.local.transform);
        playerEffect?.Play();
    }

    public void OnGround(Creature playerCreature)
    {
        Player.currentCreature.airHelper.OnGroundEvent -= OnGround;
        Player.currentCreature.RunAfter(() => Player.fallDamageScale.Remove(this), 0.5f);
        playerEffect?.End();
        playerEffect = null;
        if (Player.currentCreature.airHelper.Climbing || Player.local.locomotion.velocity.y > -minDownwardVelocity || groundDistance <= minHeight) return;
        var intensity = Mathf.InverseLerp(minHeight, minHeight + maxHeightDistance, groundDistance);
        var num = 1.0 + intensity * distanceMult;
        whooshEffect?.End();
        var instance = shockwaveEffectData?.Spawn(Player.currentCreature.transform.position, Quaternion.LookRotation(Vector3.forward, Vector3.up), null, null, true, null, false, intensity);
        instance.Play();
        instance.SetColorImmediate(Dye.GetEvaluatedColor("Body", spellId));
        var thunderEntityList = ThunderEntity.InRadiusNaive(Player.currentCreature.transform.position, radius, Filter.AllButPlayer);
        for (var index = 0; index < thunderEntityList.Count; ++index)
        {
            var thunderEntity = thunderEntityList[index];
            var direction = thunderEntityList[index].Center - Player.currentCreature.ragdoll.targetPart.transform.position;
            thunderEntity.AddExplosionForce(force * (float)num, Player.currentCreature.ragdoll.targetPart.transform.position, radius * (float)num, upwardsModifier, ForceMode.VelocityChange);
            if (thunderEntity is Item obj)
            {
                var breakable = obj.breakable;
                if (breakable != null && !breakable.contactBreakOnly) breakable.Break();
            }

            if (thunderEntity is Creature creature)
            {
                if (shockwavePushLevel > 0) creature.TryPush(Creature.PushType.Magic, direction, shockwavePushLevel);
                if (shockwaveDamage > 0.0) creature.Damage(shockwaveDamage * Mathf.Clamp01(direction.magnitude / radius));
                var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                brainModuleCrystal.Crystallise(5);
                brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, spellId), spellId);
                if (active != null) creature.Inflict(active.statusId, this, active.statusDuration, active.statusParameter, active.playEffects);
            }
        }

        OnDiveStop?.Invoke();
        if (!shake) return;
        Shaker.ShakePlayer(shakeDuration, shakeIntensity, shakeCurve, minMaxShakeIntensity);
    }
}