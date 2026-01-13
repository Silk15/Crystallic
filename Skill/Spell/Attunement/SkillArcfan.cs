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
    public string skillArcwireId = "Arcwire";
    protected SkillArcwire skillArcwire;
    protected SpellCastLightning spellCastLightning;
    protected EffectData minorEffectData;
    protected SkillShardImplosion skillShardImplosion;
    protected SkillThunderbolt skillThunderbolt;
    
    protected EffectData shardHitEffectData;
    public string shardHitEffectId = "HitComboLightning";

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        shardHitEffectData = Catalog.GetData<EffectData>(shardHitEffectId);
        skillThunderbolt = Catalog.GetData<SkillThunderbolt>("Thunderbolt");
        skillArcwire = Catalog.GetData<SkillArcwire>(skillArcwireId);
    }

    public override void OnLateSkillsLoaded(SkillData skillData, Creature creature)
    {
        base.OnLateSkillsLoaded(skillData, creature);
        if (creature.TryGetSkill("ShardImplosion", out skillShardImplosion))
        {
            skillShardImplosion.onExplode -= OnExplode;
            skillShardImplosion.onExplode += OnExplode;
        }
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        if (skillShardImplosion != null) skillShardImplosion.onExplode -= OnExplode;
    }

    private void OnExplode(SpellCastCrystallic spellCastCrystallic, Vector3 position, EffectInstance effectInstance, (ThunderEntity, Vector3)[] hitEntities)
    {
        if (!wasAttunedLastThrow)
            return;
        
        string id = $"implosion{Time.time}";
        spellCastLightning.readyEffectData.Spawn(position, Quaternion.identity).Play();
        
        ReflectiveParticles.Inject(effectInstance, id, colorModifier);
        Transform sourceTransform = new GameObject().transform;
        sourceTransform.position = position + Vector3.up * 3f;
        sourceTransform.rotation = Quaternion.identity;
        skillThunderbolt.FireBoltAt(sourceTransform, position);

        foreach (var hitEntity in hitEntities)
        {
            spellCastLightning.PlayBolt(position, hitEntity.Item2);
            if (hitEntity.Item1 is Creature creature && !creature.isPlayer)
            {
                creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Lightning"), "Lightning"));
                creature.Inflict("Electrocute", this, 5);
            }
        }

        GameManager.local.RunAfter(() => ReflectiveParticles.Remove(effectInstance, id), 1f);
    }

    protected override void OnAttunementStart(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        base.OnAttunementStart(crystallic, other);
        spellCastLightning = other as SpellCastLightning;
        spellCastLightning?.PlayBolt(crystallic.spellCaster.Orb, other.spellCaster.Orb);
        minorEffectData = Catalog.GetData<EffectData>(spellCastLightning?.readyMinorEffectId);
        other.readyEffectData.Spawn(crystallic.spellCaster.Orb).Play();
        crystallic.onShardshotStart -= OnShardshotStart;
        crystallic.onShardshotStart += OnShardshotStart;
    }

    protected override void OnAttunementStop(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        base.OnAttunementStop(crystallic, other);
        crystallic.onShardshotStart -= OnShardshotStart;
    }

    private void OnShardHit(Shard shard, CollisionInstance collisionInstance, Shard.ShardArgs customHitEffect)
    {
        customHitEffect.SetEffect(shardHitEffectData);
        minorEffectData.Spawn(collisionInstance.contactPoint, Quaternion.identity, collisionInstance.targetCollider.transform).Play();
        if (collisionInstance.targetColliderGroup?.collisionHandler?.Entity is Creature creature)
        {
            creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Lightning"), "Lightning"));
            creature.Inflict("Electrocute", this, 5);
        }
        shard.onCollision -= OnShardHit;
    }
    
    
    private void OnShardDespawn(Shard shard, EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd) 
            return;
        
        shard.onDespawn -= OnShardDespawn;
        shard.onCollision -= OnShardHit;
    }
    
    private void OnShardshotStart(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards)
    {
        if (eventTime == EventTime.OnStart) 
            return;
        
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