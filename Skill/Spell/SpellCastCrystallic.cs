using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crystallic.AI;
using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill.Spell;

public class SpellCastCrystallic : SpellCastCharge
{
    public delegate void ShardshotEvent(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance);

    public delegate void ShardshotHitEvent(SpellCastCrystallic spellCastCrystallic, ThunderEntity entity, ShardshotHit hitInfo);

    public delegate void SprayEvent(SpellCastCrystallic spellCastCrystallic);

    [ModOption("Imbue Hit Velocity Requirement", "Controls how hard you need to hit a surface for the imbue to work"), ModOptionCategory("Spell", 2), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float imbueHitVelocity = 7.5f;

    [ModOption("Slam Upwards Force Multiplier", "When you slam the staff into a surface, creatures are launched up by a value of (0, 1, 0), world up. the value gets multiplied by this."), ModOptionCategory("Spell", 2), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float slamUpwardsForceMult = 2.5f;
    
    [ModOption("Shoot Shardshot", "Controls whether throwing the spell will shoot a shardshot or not, this is for those of you who do not like the spray shot."), ModOptionCategory("Spell", 2)]
    public static bool shootShardshot = true;

    [ModOption("Shoot Stingers", "Controls whether throwing the spell will shoot a stinger or not, for those of you that dislike the stinger, but want the T4 skills."), ModOptionCategory("Spell", 2)]
    public static bool shootStinger = true;

    public bool canDrain = true;
    public string hitTransferEffectId;
    public string imbueCollisionEffectId;
    public string fingerEffectPresetId;
    public string pulseEffectId;
    public string staffSlamEffectId;
    public float defaultShardshotAngle = 25;
    public float cooldown = 0.1f;
    public float intensityPerSkill;
    public float lastTime = 1;
    public float hitDamage = 1.5f;
    public float particleAngle = 25;
    public float staffSlamMaxForce = 100f;
    public float staffSlamMaxRadius = 5f;
    public float staffSlamMinForce = 60f;
    public float forceMultiplier = 1f;
    public Vector3 lastVelocity;
    public AnimationCurve forceCurve = new(new Keyframe(0.0f, 0.05f), new Keyframe(1f, 10f));
    public AnimationCurve pulseCurve = new(new Keyframe(0.0f, 10f), new Keyframe(0.05f, 25f), new Keyframe(0.1f, 10f));
    public AnimationCurve imbueHitCurve = new(new Keyframe(0.0f, 0.05f), new Keyframe(0.05f, 5f), new Keyframe(0.1f, 0.05f));
    private readonly List<ParticleCollisionEvent> collisionEvents = new();
    public List<EffectInstance> fingerEffects = new();
    public EffectInstance shardshotEffectInstance;
    public EffectInstance transferEffectInstance;
    public EffectData staffSlamEffectData;
    public EffectData hitTransferEffectData;
    public EffectData pulseEffectData;
    public EffectData imbueCollisionEffectData;
    public ForceFieldPresetData fingerEffectPresetData;
    public Gradient defaultMainGradient;
    public Color currentColor = Color.white;
    public string spellId = "Crystallic";
    public bool speedUpByTimeScale;

    public event SprayEvent onSprayStart;

    public event SprayEvent onSprayLoop;

    public event SprayEvent onSprayEnd;

    public event ShardshotEvent OnShardshotStart;

    public event ShardshotEvent OnShardshotEnd;

    public event ShardshotHitEvent OnShardHit;

    public SpellCastCrystallic Clone() => MemberwiseClone() as SpellCastCrystallic;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        pulseEffectData = Catalog.GetData<EffectData>(pulseEffectId);
        imbueCollisionEffectData = Catalog.GetData<EffectData>(imbueCollisionEffectId);
        fingerEffectPresetData = Catalog.GetData<ForceFieldPresetData>(fingerEffectPresetId);
        hitTransferEffectData = Catalog.GetData<EffectData>(hitTransferEffectId);
        staffSlamEffectData = Catalog.GetData<EffectData>(staffSlamEffectId);
        new Harmony("com.silk.crystallic").PatchAll();
    }

    public override void UpdateGripCast(HandleRagdoll handle)
    {
        base.UpdateGripCast(handle);
        if (isGripCasting && gripCastEffectInstance != null && handle != null && handle?.ragdollPart?.ragdoll?.creature != null)
        {
            var brainModuleCrystal = handle.ragdollPart.ragdoll.creature.brain.instance.GetModule<BrainModuleCrystal>();
            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, "Body"), "Body");
        } 
    }
    
    public override void LoadSkillPassives(int skillCount)
    {
        base.LoadSkillPassives(skillCount);
        AddModifier(this, Modifier.Intensity, (float)(1.0 + intensityPerSkill * skillCount));
    }

    public override void Load(Imbue imbue)
    {
        base.Load(imbue);
        spellCaster.mana.OnImbueUnloadEvent += Unload;
    }

    private void Unload(SpellCastCharge spellData, Imbue unloadedImbue)
    {
        if (imbue == unloadedImbue) imbueEffect.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
        spellCaster.mana.OnImbueUnloadEvent -= Unload;
    }

    public void TryDrainCharge(float drainDurationMult)
    {
        if (canDrain) currentCharge -= 1 * Time.deltaTime * drainDurationMult;
    }

    public void SetShardshotAngle(float angle)
    {
        particleAngle = angle;
    }

    public void SetForceMultiplier(float multiplier)
    {
        float targetValue = forceCurve.Evaluate(multiplier / 0.1f);
        targetValue = Mathf.Clamp(targetValue, 0, 1);
        forceMultiplier = targetValue;
    }

    public void ResetForceMultiplier() => forceMultiplier = 1.0f;

    public void SetColor(Color color, string spellId, float time = 0.5f)
    {
        List<EffectInstance> particleSystems = new();
        particleSystems.Add(chargeEffectInstance);
        particleSystems.AddRange(fingerEffects);
        particleSystems.Add(SkillHyperintensity.overchargeLeftLoopEffect);
        particleSystems.Add(SkillHyperintensity.overchargeRightLoopEffect);
        particleSystems.Add(SkillCrystallicQuasar.beamLeftEffectInstance);
        particleSystems.Add(SkillCrystallicQuasar.beamRightEffectInstance);
        particleSystems.Add(SkillCrystallicQuasar.beamLeftImpactEffectInstance);
        particleSystems.Add(SkillCrystallicQuasar.beamRightImpactEffectInstance);
        currentColor = color;
        this.spellId = spellId;
        for (int i = 0; i < particleSystems.Count; i++) particleSystems[i].SetColorImmediate(currentColor);
    }

    public override void Fire(bool active)
    {
        base.Fire(active);
        SkillHyperintensity.ToggleDrain(spellCaster.side, true);
        SetColor(Dye.GetEvaluatedColor(spellId, spellId), spellId, 0.01f);
        allowCharge = true;
        allowSpray = false;
        if (!active)
        {
            DisableFingerEffects();
            spellCaster.AllowSpellWheel(this);
        }
        else
        {
            spellCaster.DisableSpellWheel(this);
            EnableFingerEffects();
            EventManager.InvokeSpellUsed("Crystallic", spellCaster.ragdollHand.creature, spellCaster.side);
        }
    }

    public override void Throw(Vector3 velocity)
    {
        base.Throw(velocity);
        DisableFingerEffects();
        spellCaster.ragdollHand.PlayHapticClipOver(pulseCurve, 1);
        if (shootShardshot) Shoot(velocity);
    }

    public void Shoot(Vector3 velocity)
    {
        lastVelocity = velocity;
        var origin = spellCaster.magicSource.position + velocity.normalized * 0.1f;
        var effectInstance = pulseEffectData?.Spawn(origin, Quaternion.LookRotation(velocity), null, null, true, null, spellCaster.mana.creature.isPlayer);
        if (effectInstance != null)
        {
            shardshotEffectInstance = effectInstance;
            effectInstance.SetColorImmediate(currentColor);
            if (spellCaster.mana.creature.isPlayer) effectInstance.SetHaptic(spellCaster.side, Catalog.gameData.haptics.telekinesisThrow);
            if (shootShardshot)
            {
                OnShardshotStart?.Invoke(this, effectInstance);
                effectInstance.SetConeAngle(particleAngle, "Beam");
                effectInstance?.Play();
                if (speedUpByTimeScale && SkillSlowTimeData.timeSlowed) effectInstance.SetSpeed(8, "Beam");
                float force = 1 * GetModifier(Modifier.Intensity) * forceMultiplier;
                if (spellCaster.mana.creature.isPlayer) Player.local.AddForce(-spellCaster.magicSource.transform.forward, force);
                effectInstance.OnParticleCollisionEvent += ShardHit;
                effectInstance.onEffectFinished += OnEffectFinished;
            }
        }

        void OnEffectFinished(EffectInstance effectInstance)
        {
            shardshotEffectInstance = null;
            effectInstance.OnParticleCollisionEvent -= ShardHit;
            effectInstance.onEffectFinished -= OnEffectFinished;
            OnShardshotEnd?.Invoke(this, effectInstance);
        }
    }

    /// <summary>
    ///     Translates a single gameObject into a precise hit point and several other nice components using particle collision events, which saves me a lot of time.
    /// </summary>
    /// <param name="other"></param>
    public void ShardHit(GameObject other)
    {
        var numCollisionEvents = shardshotEffectInstance?.GetParticleSystem("Beam")?.GetCollisionEvents(other, collisionEvents);
        if (numCollisionEvents > 0)
            if (other.TryGetComponentInParent<ThunderEntity>(out var entity))
            {
                var collisionEvent = collisionEvents[0];
                var hitPart = entity is Creature creature1 && creature1.ragdoll.GetClosestPart(collisionEvent.intersection, 0.15f, out var part) ? part : null;
                var hit = new ShardshotHit(shardshotEffectInstance, collisionEvent.colliderComponent is Collider ? collisionEvent.colliderComponent as Collider : null, collisionEvent.intersection, collisionEvent.normal, collisionEvent.velocity, collisionEvent, other, entity, hitPart, hitDamage, hitPart != null && hitPart.hasMetalArmor);
                OnShardHit?.Invoke(this, entity, hit);
                switch (entity)
                {
                    case Creature creature when creature != spellCaster.mana.creature && creature.factionId != spellCaster.mana.creature.factionId:
                    {
                        if (!creature.isPlayer) creature.TryPush(Creature.PushType.Hit, creature.ragdoll.targetPart.transform.position - spellCaster.magicSource.transform.position, 1);
                        creature.DamagePatched(hitDamage, DamageType.Energy);
                        spellCaster.ragdollHand.HapticTick(10);
                        if (!spellCaster.mana.creature.isPlayer)
                        {
                            var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                            brainModuleCrystal.Crystallise(5, "Crystallic");
                            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, "Crystallic"), "Crystallic");
                        }
                    }
                        break;
                    case Item item:
                    {
                        var directionToContact = item.transform.position - hit.hitPoint;
                        var distance = directionToContact.magnitude;
                        if (distance > 0)
                        {
                            directionToContact.Normalize();
                            var forceAmount = Mathf.Lerp(staffSlamMinForce, staffSlamMaxForce, 1 - Mathf.Clamp01(distance / staffSlamMaxRadius));
                            item.AddForce(directionToContact * forceAmount, ForceMode.Impulse);
                            foreach (var colliderGroup in item.colliderGroups)
                                if (colliderGroup != null && colliderGroup.allowImbueEffect && colliderGroup.imbue != null)
                                    colliderGroup?.imbue?.Transfer(this, forceAmount, spellCaster.mana.creature);
                        }

                        if (item.breakable is Breakable breakable)
                        {
                            breakable.Explode(30, hit.hitPoint, staffSlamMaxRadius, 0.25f, ForceMode.Impulse);

                            for (var i = 0; i < breakable.subBrokenItems.Count; ++i)
                            {
                                var subItem = breakable.subBrokenItems[i];
                                var physicBody = subItem.physicBody;
                                if (physicBody)
                                {
                                    var forceDirection = physicBody.transform.position - hit.hitPoint;
                                    forceDirection.Normalize();
                                    physicBody.AddForceAtPosition(forceDirection * 10 * GetModifier(Modifier.Intensity), physicBody.transform.position, ForceMode.Impulse);
                                }
                            }

                            for (var k = 0; k < breakable.subBrokenBodies.Count; ++k)
                            {
                                var subBrokenBody = breakable.subBrokenBodies[k];
                                if (subBrokenBody)
                                {
                                    var forceDirection = subBrokenBody.transform.position - hit.hitPoint;
                                    forceDirection.Normalize();
                                    subBrokenBody.AddForceAtPosition(forceDirection * 10 * GetModifier(Modifier.Intensity), subBrokenBody.transform.position, ForceMode.Impulse);
                                }
                            }
                        }
                    }
                        break;
                }
            }
    }

    public override bool OnImbueCollisionStart(CollisionInstance collisionInstance)
    {
        if (Time.time - lastTime > cooldown && collisionInstance.impactVelocity.magnitude > imbueHitVelocity)
        {
            lastTime = Time.time;
            var entity = collisionInstance?.targetColliderGroup?.collisionHandler?.Entity;
            var item = collisionInstance?.sourceColliderGroup?.collisionHandler?.Entity as Item;
            var other = collisionInstance.targetColliderGroup?.collisionHandler?.Entity as Item;
            if (other && item)
            {
                hitTransferEffectData?.Spawn((item.GetLocalBounds().center + other.GetLocalBounds().center) / 2, Quaternion.identity).Play();
                foreach (var colliderGroup in item.colliderGroups)
                    if (colliderGroup != null && colliderGroup.allowImbueEffect)
                        colliderGroup?.imbue?.Transfer(this, collisionInstance.impactVelocity.magnitude, spellCaster.mana.creature);
            }

            if (item) item.PlayHapticClip(imbueHitCurve, 0.25f);
            var instance = imbueCollisionEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.targetCollider.transform); 
            instance?.Play();
            instance.SetColorImmediate(currentColor);
            if (entity is Creature creature && creature != null && creature != spellCaster.mana.creature && !collisionInstance.targetMaterial.isMetal)
            {
                var brainModuleCrystal = creature?.brain?.instance?.GetModule<BrainModuleCrystal>();
                brainModuleCrystal?.Crystallise(5);
                brainModuleCrystal?.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, spellId), spellId);
            }
        }

        return base.OnImbueCollisionStart(collisionInstance);
    }

    public override bool OnCrystalSlam(CollisionInstance collisionInstance)
    {
        base.OnCrystalSlam(collisionInstance);
        var instance = staffSlamEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal), null, collisionInstance, true, null, false, collisionInstance.intensity, 0.0f);
        instance?.Play();
        instance?.SetColorImmediate(currentColor);
        var owner = imbue?.colliderGroup?.collisionHandler?.item?.mainHandler?.creature;
        foreach (var entity in ThunderEntity.InRadius(collisionInstance.contactPoint, staffSlamMaxRadius))
            switch (entity)
            {
                case Creature creature when creature != owner && creature.factionId != owner.factionId:
                {
                    creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    var directionToContact = creature.transform.position - collisionInstance.contactPoint;
                    var distance = directionToContact.magnitude;
                    if (distance > 0)
                    {
                        directionToContact.Normalize();
                        var forceAmount = Mathf.Lerp(staffSlamMinForce, staffSlamMaxForce, 1 - Mathf.Clamp01(distance / staffSlamMaxRadius));
                        creature.AddForce(directionToContact * forceAmount, ForceMode.Impulse);
                        creature.AddForce(Vector3.up * slamUpwardsForceMult, ForceMode.Impulse);
                    }

                    var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                    brainModuleCrystal.Crystallise(5);
                    brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, spellId), spellId);
                }
                    break;

                case Item item when item != imbue.colliderGroup.collisionHandler.item:
                {
                    var directionToContact = item.transform.position - collisionInstance.contactPoint;
                    var distance = directionToContact.magnitude;
                    if (distance > 0)
                    {
                        directionToContact.Normalize();
                        var forceAmount = Mathf.Lerp(staffSlamMinForce, staffSlamMaxForce, 1 - Mathf.Clamp01(distance / staffSlamMaxRadius));
                        item.AddForce(directionToContact * forceAmount, ForceMode.Impulse);
                        foreach (var colliderGroup in item.colliderGroups)
                            if (colliderGroup != null && colliderGroup.allowImbueEffect && colliderGroup.imbue != null && colliderGroup.collisionHandler.item.holder == null)
                                colliderGroup?.imbue?.Transfer(this, forceAmount, owner);
                    }

                    if (item.breakable is Breakable breakable)
                    {
                        breakable.Explode(80, collisionInstance.contactPoint, staffSlamMaxRadius, 0.25f, ForceMode.Impulse);

                        for (var i = 0; i < breakable.subBrokenItems.Count; ++i)
                        {
                            var subItem = breakable.subBrokenItems[i];
                            var physicBody = subItem.physicBody;

                            if (subItem.breakable is Breakable breakable1)
                                for (var j = 0; j < breakable.subBrokenItems.Count; ++j)
                                {
                                    var subPhysicBody = breakable.subBrokenItems[j].physicBody;
                                    if (subPhysicBody)
                                    {
                                        var forceDirection = subPhysicBody.transform.position - collisionInstance.contactPoint;
                                        forceDirection.Normalize();
                                        subPhysicBody.AddForceAtPosition(forceDirection * 10 * GetModifier(Modifier.Intensity), subPhysicBody.transform.position, ForceMode.Impulse);
                                    }
                                }

                            if (physicBody)
                            {
                                var forceDirection = physicBody.transform.position - collisionInstance.contactPoint;
                                forceDirection.Normalize();
                                physicBody.AddForceAtPosition(forceDirection * 10 * GetModifier(Modifier.Intensity), physicBody.transform.position, ForceMode.Impulse);
                            }
                        }

                        for (var k = 0; k < breakable.subBrokenBodies.Count; ++k)
                        {
                            var subBrokenBody = breakable.subBrokenBodies[k];
                            if (subBrokenBody)
                            {
                                var forceDirection = subBrokenBody.transform.position - collisionInstance.contactPoint;
                                forceDirection.Normalize();
                                subBrokenBody.AddForceAtPosition(forceDirection * 10 * GetModifier(Modifier.Intensity), subBrokenBody.transform.position, ForceMode.Impulse);
                            }
                        }
                    }
                }
                    break;
            }

        return true;
    }

    public void EnableFingerEffects()
    {
        foreach (var finger in spellCaster.ragdollHand.fingers)
        {
            var effectInstance = fingerEffectData?.Spawn(finger.tip.transform);
            if (effectInstance != null)
            {
                fingerEffects.Add(effectInstance);
                effectInstance.Play();
                effectInstance?.SetParticleTarget(fingerEffectPresetData, spellCaster.Orb, true, 0.04f);
            }
        }
    }

    public void DisableFingerEffects()
    {
        foreach (var effectInstance in fingerEffects)
        {
            effectInstance.End();
            effectInstance.ClearParticleTarget();
            effectInstance.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public override void UpdateSpray()
    {
        var shouldStop = isSpraying && currentCharge < sprayStopMinCharge;
        base.UpdateSpray();
        if (shouldStop) spellCaster.Fire(false);
    }

    public override void OnSprayStart()
    {
        base.OnSprayStart();
        allowCharge = false;
        Player.local.physicRangeModifier.Add(this, 10);
        SkillHyperintensity.ToggleDrain(spellCaster.side, false);
        onSprayStart?.Invoke(this);
        isSpraying = true;
    }

    public override void OnSprayLoop()
    {
        base.OnSprayLoop();
        onSprayLoop?.Invoke(this);
    }

    public override void OnSprayStop()
    {
        base.OnSprayStop();
        Player.local.physicRangeModifier.Remove(this);
        SkillHyperintensity.ToggleDrain(spellCaster.side, true);
        onSprayEnd?.Invoke(this);
        isSpraying = false;
    }

    public struct ShardshotHit
    {
        public EffectInstance effectInstance;
        public Collider hitCollider;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public Vector3 velocity;
        public ParticleCollisionEvent baseCollision;
        public GameObject hitObject;
        public ThunderEntity hitEntity;
        public RagdollPart hitPart;
        public float hitDamage;
        public bool wasMetal;

        public ShardshotHit(EffectInstance effectInstance, Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal, Vector3 velocity, ParticleCollisionEvent baseCollision, GameObject hitObject, ThunderEntity hitEntity, RagdollPart hitPart, float hitDamage, bool wasMetal)
        {
            this.effectInstance = effectInstance;
            this.hitCollider = hitCollider;
            this.hitPoint = hitPoint;
            this.hitNormal = hitNormal;
            this.velocity = velocity;
            this.baseCollision = baseCollision;
            this.hitObject = hitObject;
            this.hitEntity = hitEntity;
            this.hitPart = hitPart;
            this.hitDamage = hitDamage;
            this.wasMetal = wasMetal;
        }
    }
}