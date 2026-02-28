#if !SDK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Crystallic.Skill.Spell;
using Newtonsoft.Json;
using ThunderRoad;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Crystallic.Skill;

public class Shard : ItemMagicProjectile
{
    public static List<Shard> AllActive { get; } = new();
    
    public float nextRandomInvokeTime;
    public float homingRetargetDelay = 0.5f; 
    public float lastRetargetTime = Time.time;
    public bool hasDespawned;
    public bool canImplode;
    public bool hasCollided;
    
    public Vector2 minMaxRandomInvokeDelay = new(0.3f, 0.175f);
    public Vector3 targetDirection;

    [NonSerialized]
    public EffectData shardHitEffectData;
    
    [NonSerialized]
    public SpellCastCrystallic linkedSpell;
    
    [NonSerialized]
    public Coroutine despawnCoroutine;
    
    [NonSerialized]
    public Func<Creature, bool> crystalliseFunc;
    
    [NonSerialized]
    public Action<Shard> onLoad;

    public string shardHitEffectId = "HitShard";
    
    public event RandomInvokeDelegate onRandomActionInvoked;
    public event CollisionDelegate onCollision;
    public event DespawnDelegate onDespawn;
    public event FireDelegate onFire;

    [JsonIgnore]
    public List<Action<Shard>> RandomInvokeActions { get; protected set; } = new();
    
    [JsonIgnore]
    public List<EffectInstance> LinkedEffects { get; protected set;  } = new();
    
    [JsonIgnore]
    public float ElapsedLifetime { get; set; }
    
    [JsonIgnore]
    public float Lifetime { get; set; }

    public void Load(SpellCastCrystallic spellCastCrystallic)
    {
        linkedSpell = spellCastCrystallic;
        onLoad?.Invoke(this);
    }

    public override void Fire(Vector3 velocity, EffectData effectData, Item shooterItem = null, Ragdoll shooterRagdoll = null, HapticDevice haptic = HapticDevice.None, bool homing = false)
    {
        if (effectInstance != null)
        {
            effectInstance.Stop();
            effectInstance = null;
        }

        shardHitEffectData = Catalog.GetData<EffectData>(shardHitEffectId, false);
        
        base.Fire(velocity, effectData, shooterItem, shooterRagdoll, haptic, homing);
        hasDespawned = false;
        canImplode = true;
        hasCollided = false;
        AllActive?.Add(this);

        nextRandomInvokeTime = Time.time + minMaxRandomInvokeDelay.RandomRange();

        if (!AllActive.IsNullOrEmpty()) for (int i = 0; i < AllActive?.Count; i++)
        {
            if (!AllActive[i] || AllActive[i] == this) continue;
            bool foundNullGroup = false;
            
            if (item != null && !item.colliderGroups.IsNullOrEmpty())
                foreach (ColliderGroup colliderGroup in item.colliderGroups)
                    if (colliderGroup == null) 
                        foundNullGroup = true;
            
            if (!foundNullGroup)
                item?.IgnoreItemCollision(AllActive[i]?.item);
        }

        onFire?.Invoke(this);
        RandomInvokeActions.Clear();
        LinkedEffects.Clear();
        
        item.physicBody.useGravity = false;
    }

    public void Update()
    {
        if (!alive || RandomInvokeActions.IsNullOrEmpty() || Time.time < nextRandomInvokeTime) return;
        
        nextRandomInvokeTime = Time.time + minMaxRandomInvokeDelay.RandomRange();

        for (int i = 0; i < RandomInvokeActions.Count; i++)
        {
            InvokeRandomAction(RandomInvokeActions[i]);
            RandomInvokeActions[i].Invoke(this);
        }
    }

    public void DelayedDespawn(Action onComplete, float delay)
    {
        if (despawnCoroutine != null)
            StopCoroutine(despawnCoroutine);
        item.physicBody.useGravity = false;
        Lifetime = delay;
        despawnCoroutine = StartCoroutine(DespawnCoroutine());

        IEnumerator DespawnCoroutine()
        {
            InvokeDespawn(EventTime.OnStart);
            ElapsedLifetime = 0f;

            while (ElapsedLifetime < Lifetime)
            {
                ElapsedLifetime += Time.deltaTime;
                yield return null;
            }
            
            InvokeDespawn(EventTime.OnEnd);
            Lifetime = delay;
            ElapsedLifetime = 0;
            onComplete?.Invoke();
            despawnCoroutine = null;
            hasDespawned = true;
            item.physicBody.angularVelocity = Vector3.zero;
            item.physicBody.velocity = Vector3.zero;
            AllActive.Remove(this);

            if (linkedSpell != null)
                linkedSpell.InvokeShardDespawn(this);

            int finishedEffects = 0;
            int totalEffects = LinkedEffects.Count;
            foreach (EffectInstance effect in LinkedEffects)
            {
                effect.onEffectFinished += OnLinkedEffectEnd;

                void OnLinkedEffectEnd(EffectInstance linkedEffect)
                {
                    finishedEffects++;
                    effect.onEffectFinished -= OnLinkedEffectEnd;

                    if (finishedEffects == totalEffects)
                        effect.End();
                }
            }

            LinkedEffects.Clear();
            End();
        }
    }

    protected override void OnProjectileCollision(CollisionInstance collisionInstance)
    {
        base.OnProjectileCollision(collisionInstance);
        ShardArgs shardArgs = new(this);
        hasCollided = true;
        InvokeCollision(collisionInstance, shardArgs);
        shardArgs.effectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.targetCollider.transform).Play();
        
        if (despawnOnHit)
            InvokeDespawn(EventTime.OnEnd);
    }

    public void InvokeRandomAction(Action<Shard> action) => onRandomActionInvoked?.Invoke(this, action);
    public void InvokeCollision(CollisionInstance collision, ShardArgs shardArgs)
    {
        onCollision?.Invoke(this, collision, shardArgs);
        linkedSpell?.InvokeShardHit(this, collision);
    }

    public void InvokeDespawn(EventTime eventTime)
    {
        onDespawn?.Invoke(this, eventTime);
        if (eventTime == EventTime.OnEnd) 
            linkedSpell?.InvokeShardDespawn(this);
    }

    public void InvokeFire()
    {
        onFire?.Invoke(this);
        linkedSpell?.InvokeShardSpawn(this);
    }

    public class ShardArgs : EventArgs
    {
        public EffectData effectData;
        public Shard shard;
        
        public bool WasChanged => effectData != shard.shardHitEffectData;

        public ShardArgs(Shard shard)
        {
            this.shard = shard;
            effectData = shard.shardHitEffectData;
        }
        
        public void SetEffect(EffectData effectData) => this.effectData = effectData;
    }

    public delegate void CollisionDelegate(Shard shard, CollisionInstance collisionInstance, ShardArgs customHitEffect);
    public delegate void RandomInvokeDelegate(Shard shard, Action<Shard> randomInvoke);
    public delegate void DespawnDelegate(Shard shard, EventTime eventTime);
    public delegate void FireDelegate(Shard shard);
}
#endif