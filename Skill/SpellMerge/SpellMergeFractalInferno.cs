using System.Collections;
using System.Collections.Generic;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using ThunderRoad.Skill.SpellMerge;
using UnityEngine;

namespace Crystallic.Skill.SpellMerge;

public class SpellMergeFractalInferno : SpellMergeData
{
    public Vector2Int minMaxProjectileCount = new(1, 4);
    
    public float trailRadius = 2f;
    public float trailDuration = 5f;

    public float statusDuration = 5f;
    public float statusHeatTransfer = 100f;

    public string flamewallDropEffectId;
    public EffectData flamewallDropEffectData;

    public string flamewallEffectId;
    public EffectData flamewallEffectData;

    public string fireEffectId = "InfernalShardshot";
    public EffectData fireEffectData;
    
    public string shardEffectId = "InfernalShard";
    public EffectData shardEffectData;

    public string statusId = "Burning";
    public StatusData statusData;
    
    public string shardHitEffectId = "HitShardInfernal";
    public EffectData shardHitEffectData;
    
    protected EffectData fireballHitEffectData;
    public string fireballHitEffectId = "HitComboFire";
    
    public int shardCount = 3;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        fireballHitEffectData = Catalog.GetData<EffectData>(fireballHitEffectId);
        shardHitEffectData = Catalog.GetData<EffectData>(shardHitEffectId);
        flamewallDropEffectData = Catalog.GetData<EffectData>(flamewallDropEffectId);
        flamewallEffectData = Catalog.GetData<EffectData>(flamewallEffectId);
        fireEffectData = Catalog.GetData<EffectData>(fireEffectId);
        shardEffectData = Catalog.GetData<EffectData>(shardEffectId);
        statusData = Catalog.GetData<StatusData>(statusId);
    }

    public override void Throw(Vector3 velocity)
    {
        base.Throw(velocity);
        
        mana.casterLeft.ragdollHand.PlayHapticClipOver(Catalog.gameData.haptics.telekinesisThrow.curveIntensity, 0.5f);
        mana.casterRight.ragdollHand.PlayHapticClipOver(Catalog.gameData.haptics.telekinesisThrow.curveIntensity, 0.5f);
        
        Vector3 origin = mana.mergePoint.position + velocity.normalized * 0.175f; 
        fireEffectData.Spawn(origin, Quaternion.LookRotation(velocity)).Play();
        
        List<(Vector3 pos, Vector3 dir)> shardData = new();
        Quaternion baseRot = Quaternion.LookRotation(velocity);

        for (int i = 0; i < shardCount; i++)
        {
            float angleRad = shardCount == 1 ? 0f : Mathf.Lerp(-30, 30, (float)i / (shardCount - 1)) * Mathf.Deg2Rad;
            Vector3 dirLocal = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
            Vector3 direction = (baseRot * dirLocal).normalized;
            shardData.Add((origin + direction * 0.2f, direction));
        }

        shardData.Sort((a, b) => Vector3.Dot(a.pos - origin, mana.mergePoint.right).CompareTo(Vector3.Dot(b.pos - origin, mana.mergePoint.right)));
        foreach (var (position, direction) in shardData)
        {
            SpellCastCrystallic spellCastCrystallic = (mana.casterLeft.spellInstance is SpellCastCrystallic ? mana.casterLeft.spellInstance : mana.casterRight.spellInstance) as SpellCastCrystallic;
            
            mana.casterLeft.ragdollHand.PlayHapticClipOver(spellCastCrystallic.pulseCurve, 0.3f);
            mana.casterRight.ragdollHand.PlayHapticClipOver(spellCastCrystallic.pulseCurve, 0.3f);
            
            spellCastCrystallic.FireShard(shardEffectData, position, direction * (velocity.magnitude * 3f), 3.0f, 1.0f, shard =>
            {
                shard.RandomInvokeActions.Add(SpawnFlamewall);
                shard.onCollision += OnCollision;
                shard.onDespawn += OnDespawn;
                shard.item.SetColliderLayer(GameManager.GetLayer(LayerName.Avatar));
            });
        }
    }

    private void OnDespawn(Shard shard, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart)
            return;
        shard.onDespawn -= OnDespawn;
        shard.onCollision -= OnCollision;
    }

    private void OnCollision(Shard shard, CollisionInstance collisionInstance, Shard.ShardArgs customHitEffect)
    {
        shard.onCollision -= OnCollision;
        customHitEffect.SetEffect(shardHitEffectData);
    }

    public void SpawnFlamewall(Shard shard)
    {
        SpellCastProjectile spellCastProjectile = (mana.casterLeft.spellInstance is SpellCastProjectile ? mana.casterLeft.spellInstance : mana.casterRight.spellInstance) as SpellCastProjectile;
        if (spellCastProjectile != null)
        {
            FlameWall flameWall = FlameWall.Create(shard.transform.position + Vector3.down * 0.15f);
            flameWall.Init(flamewallDropEffectData, flamewallEffectData, trailRadius, trailRadius, trailRadius, 2.0f, trailDuration, statusData, statusDuration, statusHeatTransfer, drop: true);
            
            int projectiles = Random.Range(minMaxProjectileCount.x, minMaxProjectileCount.y);
            for (int index = 0; index < projectiles; ++index)
                spellCastProjectile.ShootFireSpark(spellCastProjectile.imbueHitProjectileEffectData, flameWall.transform.position + Vector3.up * 0.3f, (flameWall.transform.up * 2f + Random.insideUnitSphere) * 3f, damageMultiplier: 0.4f, onSpawnEvent: OnProjectileSpawn);
        }
    }

    private void OnProjectileSpawn(ItemMagicProjectile spawned)
    {
        spawned.item.SetColliders(false);
        spawned.item.StartCoroutine(HomingCoroutine(spawned));
        spawned.OnProjectileCollisionEvent += OnProjectileCollision;
    }

    private void OnProjectileCollision(ItemMagicProjectile projectile, CollisionInstance collisionInstance)
    {
        projectile.OnProjectileCollisionEvent -= OnProjectileCollision;
        if (collisionInstance.targetColliderGroup?.collisionHandler?.Entity is Creature creature && creature != mana.creature)
            creature.Inflict("Crystallised", this, 5f, new CrystallisedParams(Dye.GetEvaluatedColor("Fire", "Fire"), "Fire"));
        
        fireballHitEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.targetCollider.transform).Play();
    }

    public IEnumerator HomingCoroutine(ItemMagicProjectile projectile)
    {
        yield return new WaitForSeconds(0.4f);
        projectile.item.SetColliders(true);
        projectile.homing = true;
    }
}