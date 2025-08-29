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
    public static List<Shard> allActive = new();
    public int reflectCount = 0;
    public int maxReflects = 2;
    public bool invokeCollisionEvents = true;
    public bool allowReflect = true;
    public Coroutine despawnCoroutine;
    public SpellCastCrystallic spellCastCrystallic;

    public override void Fire(Vector3 velocity, EffectData effectData, Item shooterItem = null, Ragdoll shooterRagdoll = null, HapticDevice haptic = HapticDevice.None, bool homing = false)
    {
        base.Fire(velocity, effectData, shooterItem, shooterRagdoll, haptic, homing);
        for (int i = 0; i < allActive?.Count; i++) item?.IgnoreItemCollision(allActive[i]?.item, true);
        allActive?.Add(this);
    }

    public new void FixedUpdate()
    {
        if (!homing) return;
        item.physicBody.velocity = Vector3.ProjectOnPlane(item.physicBody.velocity, item.transform.up);
    }

    public void Reflect(CollisionInstance collisionInstance, EffectData reflectEffectData, bool aimAssist, float targetMoveBias, float maxDistance, float maxAngle)
    {
        if (allowReflect && reflectCount < maxReflects && collisionInstance.targetColliderGroup?.collisionHandler?.ragdollPart == null)
        {
            Vector3 reflect = Vector3.Reflect(collisionInstance.impactVelocity, collisionInstance.contactNormal);
            Vector3 position = collisionInstance.contactPoint + reflect.normalized * 0.2f;
            if (aimAssist)
            {
                ThunderEntity thunderEntity = Creature.AimAssist(position, reflect, maxDistance, maxAngle, out Transform targetPoint, Filter.EnemyOf(spellCastCrystallic.spellCaster.mana.creature), CreatureType.Golem);
                if (thunderEntity != null)
                    reflect = (targetPoint.position + (thunderEntity is Creature creature ? creature.locomotion.moveDirection : Vector3.zero) * targetMoveBias - position).normalized * reflect.magnitude;
            }

            reflectEffectData?.Spawn(position, Quaternion.LookRotation(reflect)).Play();
            spellCastCrystallic.FireShard(spellCastCrystallic.shardEffectData, position, reflect, 0.75f, newShard =>
            {
                if (collisionInstance.targetCollider)
                    newShard.item.IgnoreColliderCollision(collisionInstance.targetCollider);
                newShard.StartCoroutine(ResetCollision(newShard));
                newShard.reflectCount = reflectCount + 1;
                newShard.triggeredEvents.Add(this);
                newShard.item.lastHandler = item.lastHandler;
                newShard.thresholds = thresholds;
                newShard.throwTime = throwTime;
                newShard.damageCurve = damageCurve;
                newShard.damageMultHandler = damageMultHandler;
                newShard.thresholdEffects = thresholdEffects;
                newShard.triggeredEvents.Add(this);
            }, reflectCount < 2);
        }

        IEnumerator ResetCollision(Shard shard)
        {
            yield return Yielders.ForSeconds(0.2f);
            shard.item.ResetColliderCollision();
        }
    }

    public void DelayedDespawn(Action onComplete, float delay)
    {
        if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
        despawnCoroutine = StartCoroutine(DespawnCoroutine());

        IEnumerator DespawnCoroutine()
        {
            yield return Yielders.ForSeconds(delay);
            onComplete?.Invoke();
            despawnCoroutine = null;
            if (spellCastCrystallic.activeShards.Contains(this)) spellCastCrystallic.activeShards.Remove(this);
            item.physicBody.angularVelocity = Vector3.zero;
            item.physicBody.velocity = Vector3.zero;
            allActive.Remove(this);
            End();
        }
    }

    public void Load(SpellCastCrystallic spellCastCrystallic) => this.spellCastCrystallic = spellCastCrystallic;
}