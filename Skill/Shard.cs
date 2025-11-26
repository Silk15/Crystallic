using System;
using System.Collections;
using System.Collections.Generic;
using Crystallic.Skill.Spell;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Serialization;

namespace Crystallic.Skill;

public class Shard : ItemMagicProjectile
{
    public static List<Shard> AllActive { get; }= new();
    
    public SpellCastCrystallic linkedSpell;
    public Coroutine despawnCoroutine;
    
    public bool invokeCollisionEvents = true;
    public bool hasCollided;
    public bool hasDespawned;

    public event CollisionDelegate onCollision;
    public event DespawnDelegate onDespawn;
    public event FireDelegate onFire;
    
    public List<EffectInstance> LinkedEffects { get; } = new();
    public float ElapsedLifetime { get; set; }
    public float Lifetime { get; set; }
    
    public void Load(SpellCastCrystallic spellCastCrystallic) => this.linkedSpell = spellCastCrystallic;
    
    public override void Fire(Vector3 velocity, EffectData effectData, Item shooterItem = null, Ragdoll shooterRagdoll = null, HapticDevice haptic = HapticDevice.None, bool homing = false)
    {
        if (effectInstance != null)
        {
            effectInstance.Stop();
            effectInstance = null;
        }
        
        base.Fire(velocity, effectData, shooterItem, shooterRagdoll, haptic, homing);
        hasDespawned = false;
        hasCollided = false;
        AllActive?.Add(this);
        for (int i = 0; i < AllActive?.Count; i++) item?.IgnoreItemCollision(AllActive[i]?.item);
        onFire?.Invoke(this);
    }

    public new void FixedUpdate()
    {
        if (!homing) return;
        item.physicBody.velocity = Vector3.ProjectOnPlane(item.physicBody.velocity, item.transform.up);
    }
    
    public void DelayedDespawn(Action onComplete, float delay)
    {
        if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
        Lifetime = delay;
        despawnCoroutine = StartCoroutine(DespawnCoroutine());

        IEnumerator DespawnCoroutine()
        {
            onDespawn?.Invoke(this, EventTime.OnStart);
            ElapsedLifetime = 0f;
            while (ElapsedLifetime < Lifetime)
            {
                ElapsedLifetime += Time.deltaTime;
                yield return Yielders.EndOfFrame;
            }
            onDespawn?.Invoke(this, EventTime.OnEnd);
            Lifetime = delay;
            ElapsedLifetime = 0;
            onComplete?.Invoke();
            despawnCoroutine = null;
            hasDespawned = true;
            item.physicBody.angularVelocity = Vector3.zero;
            item.physicBody.velocity = Vector3.zero;
            AllActive.Remove(this);
            if (linkedSpell != null) linkedSpell.InvokeShardDespawn(this);
            int finishedEffects = 0;
            int totalEffects = LinkedEffects.Count;
            foreach (EffectInstance effect in LinkedEffects)
            {
                effect.onEffectFinished += OnLinkedEffectEnd;
                void OnLinkedEffectEnd(EffectInstance linkedEffect)
                {
                    finishedEffects++;
                    effect.onEffectFinished -= OnLinkedEffectEnd;
                    if (finishedEffects == totalEffects) effect.End();
                }
            }
            LinkedEffects.Clear();
            End();
        }
    }

    protected override void OnProjectileCollision(CollisionInstance collision)
    {
        base.OnProjectileCollision(collision);
        hasCollided = true;
        if (despawnOnHit)
            InvokeDespawn(EventTime.OnEnd);
    }

    public void InvokeCollision(CollisionInstance collision) => onCollision?.Invoke(this, collision);
    public void InvokeDespawn(EventTime eventTime) => onDespawn?.Invoke(this, eventTime);
    public void InvokeFire() => onFire?.Invoke(this);

    public delegate void CollisionDelegate(Shard shard, CollisionInstance collisionInstance);
    public delegate void DespawnDelegate(Shard shard, EventTime eventTime);
    public delegate void FireDelegate(Shard shard);
}