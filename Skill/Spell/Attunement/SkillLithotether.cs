using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crystallic.Skill.Spell.Attunement;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill.Spell.Attunement;

public class SkillLithotether : AttunementSkillData
{
    [ModOption("Lithotether Spring", "The spring applied to the tether connecting two physicBodies, this is the value that decides how tightly two limbs are bound.", order = 0), ModOptionCategory("Lithotether", 3), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
    public static float spring = 100f;

    [ModOption("Lithotether Damper", "The damping applied to the tether connecting two physicBodies, this acts as a smoother, damping out movement to act floaty.", order = 1), ModOptionCategory("Lithotether", 3), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
    public static float damper = 50f;

    [ModOption("Min Lithotether Distance", "The min distance two physicBodies can be from one another.", order = 2), ModOptionCategory("Lithotether", 3), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float minDistance = 5f;

    [ModOption("Max Lithotether Distance", "The max distance two physicBodies can be from one another.", order = 3), ModOptionCategory("Lithotether", 3), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float maxDistance = 10f;

    [ModOption("Gravity Well Lifetime", "Controls the time the gravity-attuned implosion lasts.", order = 3), ModOptionCategory("Lithotether", 3), ModOptionSlider, ModOptionFloatValues(1f, 100, 1f)]
    public static float gravityWellLifetime = 2f;

    public Vector2 minMaxTetherLifetime = new(2, 6);

    public StatusData statusData;
    public EffectData snapEffectData;
    public EffectData tetherEffectData;
    public EffectData lithotetherEffectData;
    public EffectData impactPulseEffectData;
    public EffectData gravityWellEffectData;

    public string snapEffectId = "GravitySnap";
    public string tetherEffectId = "GravityTether";
    public string impactPulseEffectId = "ImpactPulse";
    public string lithotetherEffectId = "Lithotether";
    public string gravityWellEffectId = "GravityWell";

    public SpellCastGravity spellCastGravity;
    public SkillShardImplosion skillShardImplosion;
    public Dictionary<Rigidbody, JointEffect> jointedParts = new();


    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        gravityWellEffectData = Catalog.GetData<EffectData>(gravityWellEffectId);
        impactPulseEffectData = Catalog.GetData<EffectData>(impactPulseEffectId);
        lithotetherEffectData = Catalog.GetData<EffectData>(lithotetherEffectId);
        tetherEffectData = Catalog.GetData<EffectData>(tetherEffectId);
        snapEffectData = Catalog.GetData<EffectData>(snapEffectId);
        statusData = Catalog.GetData<StatusData>("Floating");
    }

    public override void OnLateSkillsLoaded(SkillData skillData, Creature creature)
    {
        base.OnLateSkillsLoaded(skillData, creature);
        if (creature.TryGetSkill("ShardImplosion", out skillShardImplosion))
        {
            skillShardImplosion.onImplode -= OnImplode;
            skillShardImplosion.onImplode += OnImplode;
        }
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        if (skillShardImplosion != null) 
            skillShardImplosion.onImplode -= OnImplode;
    }

    private void OnImplode(SpellCastCrystallic spellCastCrystallic, Vector3 position, EffectInstance effectInstance, (ThunderEntity, Vector3)[] hitEntities)
    {
        if (wasAttunedLastThrow)
            GameManager.local.StartCoroutine(ImplosionCoroutine(spellCastCrystallic, position, hitEntities));
    }

    protected override void OnAttunementStart(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        base.OnAttunementStart(crystallic, other);
        if (other is SpellCastGravity spellCastGravity)
        {
            spellCastGravity.readyEffectData.Spawn(crystallic.spellCaster.Orb).Play();
            this.spellCastGravity = spellCastGravity;
        }

        crystallic.onShardshotStart += OnShardshotStart;
    }

    protected override void OnAttunementStop(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        base.OnAttunementStop(crystallic, other);
        crystallic.onShardshotStart -= OnShardshotStart;
    }

    private void OnShardshotStart(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards)
    {
        if (eventTime == EventTime.OnStart) return;
        foreach (Shard shard in shards)
        {
            shard.onCollision += OnShardHit;
            shard.onDespawn += OnDespawn;
        }
    }

    private void OnDespawn(Shard shard, EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd)
        {
            shard.onDespawn -= OnDespawn;
            shard.onCollision -= OnShardHit;
        }
    }

    private void OnShardHit(Shard shard, CollisionInstance collisionInstance)
    {
        if (collisionInstance.targetColliderGroup?.collisionHandler?.ragdollPart is RagdollPart ragdollPart && !jointedParts.ContainsKey(ragdollPart.physicBody.rigidBody))
        {
            impactPulseEffectData.Spawn(collisionInstance.contactPoint, Quaternion.identity).Play();
            ragdollPart.ragdoll.creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(ragdollPart.ragdoll.creature.GetCurrentCrystallisationId(), "Gravity"), "Gravity"));
            Rigidbody rigidbody = new GameObject("Lithotether Part").AddComponent<Rigidbody>();
            rigidbody.transform.SetPositionAndRotation(shard.transform.position, Quaternion.identity);
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            ragdollPart.ragdoll.creature.AddForce((ragdollPart.transform.position - collisionInstance.contactPoint).normalized * 60, ForceMode.Impulse);

            if (Random.Range(0, 2) == 1)
            {
                ragdollPart.Slice();
                ragdollPart.physicBody.AddForce((ragdollPart.transform.position - collisionInstance.contactPoint).normalized * 50, ForceMode.Impulse);
            }

            TryCreateJoint(rigidbody, ragdollPart.physicBody.rigidBody, ragdollPart.ragdoll.creature);
            shard.Lifetime = 0;
        }
    }

    public JointEffect TryCreateJoint(Rigidbody source, Rigidbody target, Creature creature = null, bool startCoroutine = true)
    {
        if (jointedParts.ContainsKey(target))
            return jointedParts[target];

        var effectInstance = tetherEffectData.Spawn(source.transform);
        effectInstance.SetSource(source.transform);
        effectInstance.SetTarget(target.transform);
        effectInstance.Play();

        var lithotetherEffect = lithotetherEffectData.Spawn(source.transform.position, Quaternion.identity);
        lithotetherEffect.Play();
        lithotetherEffect.SetIntensity(1);

        var joint = Utils.CreateConfigurableJoint(target, source, spring, damper, minDistance, maxDistance, 0.35f);

        var jointEffect = new JointEffect(joint);
        jointEffect.effectInstances.Add(effectInstance);
        jointEffect.effectInstances.Add(lithotetherEffect);
        jointedParts.Add(target, jointEffect);
        if (creature)
            creature.Inflict(statusData, this, parameter: new FloatingParams(0f, 0.1f));
        if (startCoroutine)
            creature.StartCoroutine(TetherRoutine(creature.ragdoll.parts.FirstOrDefault(r => r.physicBody.rigidBody == target)));
        return jointEffect;
    }

    public IEnumerator TetherRoutine(RagdollPart ragdollPart)
    {
        yield return Yielders.ForSeconds(Random.Range(minMaxTetherLifetime.x, minMaxTetherLifetime.y));
        var jointEffect = jointedParts[ragdollPart.physicBody.rigidBody];
        jointedParts.Remove(ragdollPart.physicBody.rigidBody);
        ragdollPart.ragdoll.creature.Remove(statusData, this);
        foreach (EffectInstance effectInstance in jointEffect.effectInstances)
        {
            effectInstance.SetSourceAndTarget(null, null);
            effectInstance.SetParent(null);
            effectInstance.End();
        }

        snapEffectData.Spawn(jointEffect.configurableJoint.transform.position, Quaternion.identity).Play();
        Object.Destroy(jointEffect.configurableJoint.gameObject);
    }

    public IEnumerator ImplosionCoroutine(SpellCastCrystallic spellCastCrystallic, Vector3 position, (ThunderEntity, Vector3)[] hitEntities)
    {
        yield return Yielders.ForSeconds(0.25f);
        EffectData pushEffectData = Catalog.GetData<EffectData>(spellCastGravity.pushEffectId);
        Rigidbody source = new GameObject("Gravity Well").AddComponent<Rigidbody>();
        source.transform.SetPositionAndRotation(position, Quaternion.identity);
        source.isKinematic = true;
        source.useGravity = false;

        EffectInstance gravityWell = gravityWellEffectData.Spawn(position, Quaternion.identity);
        gravityWell.SetIntensity(1);
        gravityWell.Play();

        List<JointEffect> jointEffects = new();

        foreach (var entity in hitEntities)
        {
            Rigidbody target = null;

            switch (entity.Item1)
            {
                case Creature creature when !creature.isPlayer && creature.ragdoll.GetClosestPart(entity.Item2, 2f, out var part):
                    target = part.physicBody.rigidBody;
                    creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    pushEffectData.Spawn(creature.Center + Vector3.down, Quaternion.LookRotation(Vector3.up)).Play();
                    creature.AddForce(Vector3.up * 60, ForceMode.Impulse);
                    break;
            }

            if (target != null)
            {
                entity.Item1.Inflict(statusData, this, parameter: new FloatingParams(0f, 0.1f));
                impactPulseEffectData.Spawn(target.transform).Play();
                var effectInstance = tetherEffectData.Spawn(target.transform);
                effectInstance.SetSource(source.transform);
                effectInstance.SetTarget(target.transform);
                effectInstance.Play();

                var joint = Utils.CreateConfigurableJoint(source, target, 1500, damper, 2f, 5f, 0.35f);

                var jointEffect = new JointEffect(joint);
                jointEffect.effectInstances.Add(effectInstance);

                jointEffects.Add(jointEffect);
                yield return Yielders.ForSeconds(Random.Range(0.4f, 0.65f));
            }
        }

        float elapsed = 0f;
        while (elapsed < gravityWellLifetime)
        {
            elapsed += Time.deltaTime;
            yield return Yielders.EndOfFrame;
            gravityWell.SetIntensity(Mathf.Lerp(1, 0, elapsed / gravityWellLifetime));
        }

        foreach (var jointEffect in jointEffects)
        {
            foreach (EffectInstance effectInstance in jointEffect.effectInstances)
            {
                effectInstance.End();
                effectInstance.SetParent(null);
            }

            Object.Destroy(jointEffect.configurableJoint);
        }

        foreach (var thunderEntity in hitEntities)
            thunderEntity.Item1.Remove(statusData, this);

        gravityWell.End();
        impactPulseEffectData.Spawn(position, Quaternion.identity).Play();
        snapEffectData.Spawn(position, Quaternion.identity).Play();
    }
}