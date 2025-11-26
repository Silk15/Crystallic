using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Pools;
using ThunderRoad.Skill.Spell;
using ThunderRoad.Skill.SpellMerge;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crystallic.Skill.Spell.Attunement;

public class SkillArcfan : AttunementSkillData
{
    [ModOption("Allow Arcfan Electrocution", "Controls whether Arcfan projectiles electrocute enemies.", order = 0), ModOptionCategory("Arcfan", 4)]
    public static bool allowArcfanElectrocution = true;
    
    [ModOption("Allow Arcfan Crystallisation", "Controls whether Arcfan projectiles modify a hit enemy's crystallisation colour.", order = 1), ModOptionCategory("Arcfan", 4)]
    public static bool allowArcfanCrystallisation = true;

    public string skillArcwireId = "Arcwire";
    
    protected SkillArcwire skillArcwire;

    protected SpellCastLightning spellCastLightning;
    protected EffectData minorEffectData;
    
    protected SkillShardImplosion skillShardImplosion;
    protected SkillThunderbolt skillThunderbolt;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        skillThunderbolt = Catalog.GetData<SkillThunderbolt>("Thunderbolt");
        skillArcwire = Catalog.GetData<SkillArcwire>(skillArcwireId);
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
        if (skillShardImplosion != null) skillShardImplosion.onImplode -= OnImplode;
    }

    private void OnImplode(SpellCastCrystallic spellCastCrystallic, Vector3 position, EffectInstance effectInstance, (ThunderEntity, Vector3)[] hitEntities)
    {
        if (!wasAttunedLastThrow) return;
        string id = $"implosion{Time.time}";
        spellCastLightning.readyEffectData.Spawn(position, Quaternion.identity).Play();
        ReflectiveParticles.Inject(effectInstance, id, colorModifier);
        GameManager.local.RunAfter(() =>
        {
            Transform sourceTransform = new GameObject().transform;
            sourceTransform.position = position + Vector3.up * 3f;
            sourceTransform.rotation = Quaternion.identity;
            skillThunderbolt.FireBoltAt(sourceTransform, position);

            foreach (var hitEntity in hitEntities)
            {
                spellCastLightning.PlayBolt(position, hitEntity.Item2);
                if (hitEntity.Item1 is Creature creature && !creature.isPlayer)
                {
                    if (allowArcfanCrystallisation) creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Lightning"), "Lightning"));
                    if (allowArcfanElectrocution) creature.Inflict("Electrocute", this, 5);
                }
            }
            
            GameManager.local.RunAfter(() => ReflectiveParticles.Remove(effectInstance, id), 1f);
        }, 0.25f);
    }

    protected override void OnAttunementStart(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        base.OnAttunementStart(crystallic, other);
        spellCastLightning = other as SpellCastLightning;
        spellCastLightning.PlayBolt(crystallic.spellCaster.Orb, other.spellCaster.Orb);
        minorEffectData = Catalog.GetData<EffectData>(spellCastLightning.readyMinorEffectId);
        other.readyEffectData.Spawn(crystallic.spellCaster.Orb).Play();
        crystallic.onShardshotStart -= OnShardshotStart;
        crystallic.onShardshotStart += OnShardshotStart;
    }

    protected override void OnAttunementStop(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        base.OnAttunementStop(crystallic, other);
        crystallic.onShardshotStart -= OnShardshotStart;
    }

    private void OnShardHit(Shard shard, CollisionInstance collisionInstance)
    {
        minorEffectData.Spawn(collisionInstance.contactPoint, Quaternion.identity, collisionInstance.targetCollider.transform).Play();
        if (collisionInstance.targetColliderGroup?.collisionHandler?.Entity is Creature creature)
        {
            if (allowArcfanCrystallisation) creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Lightning"), "Lightning"));
            if (allowArcfanElectrocution) creature.Inflict("Electrocute", this, 5);
        }
        shard.onCollision -= OnShardHit;
    }
    
    
    private void OnShardDespawn(Shard shard, EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd) return;
        shard.onDespawn -= OnShardDespawn;
        shard.onCollision -= OnShardHit;
    }
    
    private void OnShardshotStart(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards)
    {
        if (eventTime == EventTime.OnStart) return;
        Shard lastShard = null;
        foreach (Shard shard in shards)
        {
            shard.onCollision -= OnShardHit;
            shard.onCollision += OnShardHit;
            shard.onDespawn -= OnShardDespawn;
            shard.onDespawn += OnShardDespawn;
            if (lastShard == null)
            {
                lastShard = shard;
                continue;
            }
            CreateNodes(shard, lastShard, skillArcwire, spellCastCrystallic.spellCaster, spellCastCrystallic.ShardLifetime);
            lastShard = shard;
        }
    }

    public static void CreateNodes(Shard sourceShard, Shard targetShard, SkillArcwire skillArcwire, SpellCaster spellCaster, float lifetime)
    {
        LightningTrailNode prevNode = LightningTrailNode.New(sourceShard.transform.position, skillArcwire, null, spellCaster.mana.creature, sourceShard.transform, arcTips: true);
        prevNode.duration = lifetime;
        LightningTrailNode newNode = LightningTrailNode.New(targetShard.transform.position, skillArcwire, null, spellCaster.mana.creature, targetShard.transform, prevNode, true);
        newNode.duration = lifetime;
        newNode.ForceActivate();
        prevNode.rb.isKinematic = true;
        newNode.rb.isKinematic = true;
    }
}