using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using ThunderRoad.Skill.SpellMerge;
using UnityEngine;

namespace Crystallic.Skill.Spell.Attunement;

public class SkillShardpyre : AttunementSkillData
{
    public Dictionary<Shard, List<ItemMagicProjectile>> spawnedProjectiles = new();
    protected SkillShardImplosion skillShardImplosion;
    public SpellCastProjectile spellCastProjectile;
    public EffectData shardpyreDetonationEffectData;
    public string shardpyreDetonationEffectId = "ShardpyreDetonation";
    public float maxSpreadAngle = 10f;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        spawnedProjectiles.Clear();
        shardpyreDetonationEffectData = Catalog.GetData<EffectData>(shardpyreDetonationEffectId);
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
        if (skillShardImplosion != null) 
            skillShardImplosion.onExplode -= OnExplode;
    }

    private void OnExplode(SpellCastCrystallic spellCastCrystallic, Vector3 position, EffectInstance effectInstance, (ThunderEntity, Vector3)[] hitEntities)
    {
        if (wasAttunedLastThrow)
        {
            string id = $"implosion{Time.time}";
            ReflectiveParticles.Inject(effectInstance, id, colorModifier);
            List<ItemMagicProjectile> projectiles = new();

            foreach (KeyValuePair<Shard, List<ItemMagicProjectile>> kvp in spawnedProjectiles)
                if (kvp.Key.linkedSpell == spellCastCrystallic)
                    projectiles.AddRange(kvp.Value);

            shardpyreDetonationEffectData.Spawn(position, Quaternion.identity).Play();
            int num = Random.Range(5, 9);

            for (int index = 0; index < num; ++index)
            {
                Vector3 vector3 = Random.insideUnitSphere;
                FireProjectile(position + vector3, position + vector3.normalized * 8f);
            }

            GameManager.local.RunAfter(() => ReflectiveParticles.Remove(effectInstance, id), 1f);
            GameManager.local.StartCoroutine(HomeInCoroutine(projectiles, position));
        }
    }

    protected override void OnAttunementStart(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        base.OnAttunementStart(crystallic, other);
        other.readyEffectData.Spawn(crystallic.spellCaster.Orb).Play();
        spellCastProjectile = other as SpellCastProjectile;
        crystallic.onShardshotStart -= OnShardshotStart;
        crystallic.onShardshotStart += OnShardshotStart;
    }

    protected override void OnAttunementStop(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        base.OnAttunementStop(crystallic, other);
        crystallic.onShardshotStart -= OnShardshotStart;
    }

    private void OnShardshotStart(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards)
    {
        if (eventTime == EventTime.OnStart || shards.IsNullOrEmpty()) return;
        for (int i = 0; i < shards.Count; i++)
        {
            Shard shard = shards[i];
            shard.homing = false;
            shard.guidanceFunc = null;
            shard.guidance = GuidanceMode.NonGuided;
            spawnedProjectiles[shard] = new List<ItemMagicProjectile>();
            shard.RandomInvokeActions.Add(FireShardProjectile);
            shard.onDespawn += OnShardDespawn;
        }
    }

    private void OnShardDespawn(Shard shard, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart) 
            return;
        
        shard.RandomInvokeActions.Remove(FireShardProjectile);
        shard.onDespawn -= OnShardDespawn;
    }

    public void FireShardProjectile(Shard shard)
    {
        Vector3 direction = Vector3.RotateTowards(current: shard.transform.forward, target: Random.insideUnitSphere, maxRadiansDelta: maxSpreadAngle * Mathf.Deg2Rad, maxMagnitudeDelta: 0.0f);
        FireProjectile(shard.transform.position, direction * shard.speed, shard.item, spawnedProjectiles[shard]);
    }

    public void FireProjectile(Vector3 position, Vector3 direction, Item ignoredItem = null, List<ItemMagicProjectile> allocList = null)
    {
        spellCastProjectile.ShootFireSpark(spellCastProjectile.imbueHitProjectileEffectData, position, direction, true, onSpawnEvent: projectile =>
        {
            projectile.guidance = GuidanceMode.NonGuided;
            projectile.guidanceFunc = null;
            
            allocList?.Add(projectile);
            projectile.OnProjectileCollisionEvent += OnProjectileCollisionEvent;
            
            if (ignoredItem != null) 
                projectile.item.IgnoreItemCollision(ignoredItem);
            
            if (allocList != null)
                foreach (ItemMagicProjectile other in allocList)
                {
                    if (other == projectile) 
                        continue;
                    
                    if (ignoredItem) other.item.IgnoreItemCollision(ignoredItem);
                    other.item.IgnoreItemCollision(projectile.item);
                }
        });
    }

    private void OnProjectileCollisionEvent(ItemMagicProjectile projectile, CollisionInstance collisionInstance)
    {
        if (collisionInstance.targetColliderGroup?.collisionHandler?.Entity is Creature creature && !creature.isPlayer)
            creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Fire"), "Fire"));
            
        projectile.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
        foreach (List<ItemMagicProjectile> projectiles in spawnedProjectiles.Values)
            if (projectiles.Contains(projectile)) projectiles.Remove(projectile);
    }

    public IEnumerator HomeInCoroutine(List<ItemMagicProjectile> projectiles, Vector3 position)
    {
        while (projectiles.Count > 0)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                ItemMagicProjectile projectile = projectiles[i];

                projectile.homing = false;
                projectile.guidance = GuidanceMode.FullyGuided;
                projectile.guidanceFunc = () => (position - projectile.transform.position).normalized;

                if (Vector3.Distance(position, projectile.transform.position) < 0.1f)
                    Clear();

                if (!projectile.alive || Time.time - projectile.item.spawnTime < 0.5f)
                    Clear();

                void Clear()
                {
                    projectile.Despawn();
                    projectiles.RemoveAt(i);
                    projectile.guidance = GuidanceMode.NonGuided;
                    projectile.guidanceFunc = null;
                    
                    foreach (List<ItemMagicProjectile> projectileList in spawnedProjectiles.Values)
                        if (projectileList.Contains(projectile))
                            projectileList.Remove(projectile);
                }
            }

            yield return null;
        }
    }
}