using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Crystallic.Skill.Spell;

public class SpellCastCrystallic : SpellCastCharge
{
    public AnimationCurve pulseCurve = new(new Keyframe(0.0f, 10f), new Keyframe(0.05f, 25f), new Keyframe(0.1f, 10f));
    public EffectData imbueCollisionEffectData;
    public EffectData shardEffectData;
    public EffectData pulseEffectData;
    public DamagerData shardDamagerData;
    public EffectData staffSlamEffectData;
    public ItemData shardItemData;

    public List<Shard> activeShards = new();

    public string staffSlamEffectId;
    public string imbueCollisionEffectId;
    public string shardDamagerId;
    public string shardItemId;
    public string shardEffectId;
    public string pulseEffectId;

    public float slamUpwardsForceMult = 0.35f;
    public float staffSlamMinForce = 10f;
    public float staffSlamMaxForce = 60f;
    public float staffSlamMaxRadius = 7.5f;
    public float efficiencyPerSkill = 0.5f;
    public float intensityPerSkill = 0.1f;
    public float lastShardshotTime = 0f;
    private float cooldown = 0.1f;
    private float lastTime = 1;

    public int defaultShardCount = 3;
    public int shardCount;

    public event ShardHitEvent onShardHit;
    public event ShardEvent onShardSpawn;
    public event ShardEvent onShardDespawn;
    public event ShardshotStartEvent onShardshotStart;
    public event ShardshotEndEvent onShardshotEnd;

    public delegate void ShardHitEvent(SpellCastCrystallic spellCastCrystallic, Shard shard, CollisionInstance collisionInstance);

    public delegate void ShardEvent(SpellCastCrystallic spellCastCrystallic, Shard shard);

    public delegate void ShardshotStartEvent(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards = null);

    public delegate void ShardshotEndEvent(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance);

    public SpellCastCrystallic Clone() => MemberwiseClone() as SpellCastCrystallic;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        staffSlamEffectData = Catalog.GetData<EffectData>(staffSlamEffectId);
        pulseEffectData = Catalog.GetData<EffectData>(pulseEffectId);
        shardEffectData = Catalog.GetData<EffectData>(shardEffectId);
        shardDamagerData = Catalog.GetData<DamagerData>(shardDamagerId);
        imbueCollisionEffectData = Catalog.GetData<EffectData>(imbueCollisionEffectId);
        shardItemData = Catalog.GetData<ItemData>(shardItemId);
    }

    public override void Load(SpellCaster spellCaster)
    {
        base.Load(spellCaster);
        shardCount = defaultShardCount;
    }

    public override void LoadSkillPassives(int skillCount)
    {
        base.LoadSkillPassives(skillCount);
        AddModifier(this, Modifier.Intensity, 1.0f + intensityPerSkill * skillCount);
        AddModifier(this, Modifier.Efficiency, Mathf.Max(25f, 40f - efficiencyPerSkill * skillCount));
    }

    public override void Fire(bool active)
    {
        base.Fire(active);
        if (active) EventManager.InvokeSpellUsed("Crystallic", spellCaster.ragdollHand.creature, spellCaster.side);
    }

    public override void Throw(Vector3 velocity)
    {
        base.Throw(velocity);
        int total = 0;
        spellCaster.ragdollHand.PlayHapticClipOver(pulseCurve, 1);
        spellCaster.ragdollHand.HapticTick(1);
        Vector3 origin = spellCaster.magicSource.position + velocity.normalized * 0.1f;
        var effectInstance = pulseEffectData.Spawn(origin, Quaternion.LookRotation(velocity));
        effectInstance.onEffectFinished += OnEffectFinished;
        onShardshotStart?.Invoke(this, effectInstance, EventTime.OnStart, velocity);
        lastShardshotTime = Time.time;
        effectInstance.Play();

        Quaternion baseRot = Quaternion.LookRotation(velocity);
        for (int i = 0; i < shardCount; i++)
        {
            Vector2 point = GetUniformPointOnDisk(i, shardCount) * Mathf.Tan(GetModifier(Modifier.Efficiency) * Mathf.Deg2Rad);
            Vector3 dir = (baseRot * new Vector3(point.x, point.y, 1)).normalized;
            Vector3 pos = origin + dir * 0.2f;
            FireShard(shardEffectData, pos, dir * (velocity.magnitude * 2.5f), 0.75f, (shard) =>
            {
                total++;
                if (total == shardCount)
                    onShardshotStart?.Invoke(this, effectInstance, EventTime.OnEnd, velocity, activeShards);
            });
        }

        void OnEffectFinished(EffectInstance effectInstance)
        {
            effectInstance.onEffectFinished -= OnEffectFinished;
            onShardshotEnd?.Invoke(this, effectInstance);
        }

        Vector2 GetUniformPointOnDisk(int index, int total)
        {
            if (index == 0) return new Vector2(0.01f, 0.01f);

            float angle = (index - 1) * Mathf.PI * 2f / (total - 1);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    public void FireShard(EffectData shardEffect, Vector3 shootPos, Vector3 shootVelocity, float lifetime, Action<Shard> onSpawned = null, bool invoke = true)
    {
        shardItemData.SpawnAsync(shard =>
        {
            shard.SetColliders(false);
            shard.RunAfter(() => shard.SetColliders(true), 0.5f);
            shard.transform.position = shootPos;
            shard.transform.rotation = Quaternion.LookRotation(shootVelocity);
            RagdollHand ragdollHand = imbue?.colliderGroup.collisionHandler.item.lastHandler ?? spellCaster?.ragdollHand;
            if (ragdollHand?.ragdoll) shard.IgnoreRagdollCollision(ragdollHand.ragdoll);
            FloatHandler floatHandler = new FloatHandler();
            floatHandler.Add(this, 1);
            foreach (CollisionHandler collisionHandler in shard.collisionHandlers)
            foreach (Damager damager in collisionHandler.damagers)
            {
                damager.Load(shardDamagerData, collisionHandler);
                damager.skillDamageMultiplierHandler = floatHandler;
            }

            Shard component = shard.GetComponent<Shard>();
            if (component)
            {
                activeShards.Add(component);
                component.destroyInWater = true;
                component.guidance = GuidanceMode.NonGuided;
                component.speed = 20;
                component.item.lastHandler = ragdollHand;
                component.item.physicBody.useGravity = false;
                component.allowDeflect = false;
                component.imbueSpellCastCharge = this;
                component.Load(this);
                component.invokeCollisionEvents = invoke;
                component.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
                component.OnProjectileCollisionEvent += OnProjectileCollisionEvent;
                component.Fire(shootVelocity, shardEffect, imbue?.colliderGroup.collisionHandler.item, imbue?.colliderGroup.collisionHandler.ragdollPart?.ragdoll ?? spellCaster?.ragdollHand?.ragdoll, homing: false);
                onSpawned?.Invoke(component);
                component.item.physicBody.angularVelocity = Vector3.zero;
                if (invoke) onShardSpawn?.Invoke(this, component);
                component.DelayedDespawn(() =>
                {
                    if (activeShards.Contains(component)) activeShards.Remove(component);
                    if (invoke) onShardDespawn?.Invoke(this, component);
                }, lifetime);
            }
        });
    }

    public override bool OnCrystalSlam(CollisionInstance collisionInstance)
    {
        base.OnCrystalSlam(collisionInstance);
        var instance = staffSlamEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal), null, collisionInstance, true, null, false, collisionInstance.intensity, 0.0f);
        instance?.Play();
        var owner = imbue?.colliderGroup?.collisionHandler?.item?.mainHandler?.creature;
        foreach (var entity in ThunderEntity.InRadius(collisionInstance.contactPoint, staffSlamMaxRadius))
            switch (entity)
            {
                case Creature creature when creature != null && owner != null && creature.factionId != 0 && creature != owner && creature.factionId != owner.factionId:
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

                    creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
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

    private void OnProjectileCollisionEvent(ItemMagicProjectile projectile, CollisionInstance collisionInstance)
    {
        var shard = projectile as Shard;
        if (shard.despawnCoroutine != null) shard.StopCoroutine(shard.despawnCoroutine);
        if (shard.invokeCollisionEvents)
        {
            onShardHit?.Invoke(this, shard, collisionInstance);
            onShardDespawn?.Invoke(this, shard);
        }

        projectile.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
        projectile.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
        if (collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && creature != spellCaster.mana.creature && creature.factionId != spellCaster.mana.creature.factionId)
            creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
    }

    public override bool OnImbueCollisionStart(CollisionInstance collisionInstance)
    {
        if (Time.time - lastTime > cooldown && collisionInstance.impactVelocity.magnitude > 7.5f)
        {
            lastTime = Time.time;
            imbueCollisionEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.targetCollider.transform).Play();
            if (collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && creature != null && creature != spellCaster.mana.creature && collisionInstance.targetMaterial != null && !collisionInstance.targetMaterial.IsMetal())
                creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
        }

        return base.OnImbueCollisionStart(collisionInstance);
    }
}