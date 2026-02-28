using System;
using System.Collections;
using System.Collections.Generic;
using Crystallic.Skill.Spell;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Crystallic.Golem.Ability.Throw;

public class GolemCrystalCore : GolemThrow
{
    public string coreFireEffectId;
    public string fireHitEffectId;
    public string coreEffectId;
    public float projectileVelocity = 0.4f;
    public float coreFireLifetime = 6f;
    public float coreFireDelay = 2f;
    public float dragDelay;
    public float drag;

    [NonSerialized]
    public List<ParticleCollisionEvent> collisionEvents = new();

    [NonSerialized]
    public Dictionary<Creature, float> lastPushTimes = new();

    #if !SDK
    [NonSerialized]
    public ItemMagicProjectile itemMagicProjectile;

    [NonSerialized]
    public EffectInstance coreEffectInstance;
    #endif

    [NonSerialized]
    public EffectData coreFireEffectData;

    [NonSerialized]
    public EffectData fireHitEffectData;

    [NonSerialized]
    public EffectData coreEffectData;

    #if !SDK

    public override void Begin(GolemController golem)
    {
        base.Begin(golem);
        coreFireEffectData = Catalog.GetData<EffectData>(coreFireEffectId);
        fireHitEffectData = Catalog.GetData<EffectData>(fireHitEffectId);
        coreEffectData = Catalog.GetData<EffectData>(coreEffectId);
        lastPushTimes.Clear();
    }

    public override bool Allow(GolemController golem) => base.Allow(golem) && coreEffectInstance == null;
    
    public override void ReleaseObject(bool launch = true)
    {
        base.ReleaseObject(launch);
        if (throwingObject.TryGetComponent(out itemMagicProjectile))
        {
            Item item = itemMagicProjectile.item;

            itemMagicProjectile.guidance = GuidanceMode.NonGuided;
            itemMagicProjectile.guidanceAmount = 0f;
            itemMagicProjectile.homing = false;

            itemMagicProjectile.speed = projectileVelocity;
            itemMagicProjectile.destroyInWater = true;
            itemMagicProjectile.despawnOnHit = true;
            
            item.SetPhysicModifier(this, 0.0f);
            item.SetColliders(false, true);

            if (throwingObject.physicBody.CalculateBodyLaunchVector(golem.transform.position + golem.transform.forward * 15f, out Vector3 velocity, throwVelocity, gravityMultiplier))
            {
                item.physicBody.AddForce(velocity * projectileVelocity, ForceMode.Impulse);
                itemMagicProjectile.Fire(velocity, coreEffectData);

                itemMagicProjectile.StartCoroutine(ActivationCoroutine(itemMagicProjectile));
                
                item.OnDespawnEvent -= OnItemDespawnEvent;
                item.OnDespawnEvent += OnItemDespawnEvent;
                
                itemMagicProjectile.OnProjectileCollisionEvent += OnProjectileCollisionEvent;
            }
        }
    }

    private void OnProjectileCollisionEvent(ItemMagicProjectile projectile, CollisionInstance collisionInstance)
    {
        projectile.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
        ClearCore();
    }

    private void OnItemDespawnEvent(EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd)
        {
            itemMagicProjectile.item.OnDespawnEvent -= OnItemDespawnEvent;
            ClearCore();
        }
    }
    
    private void OnParticleCollisionEvent(GameObject other)
    {
        if (other.TryGetComponentInParent(out Creature creature) && itemMagicProjectile && coreEffectInstance != null)
        {
            if (!lastPushTimes.ContainsKey(creature) || Time.time - lastPushTimes[creature] >= 0.5f)
            {
                creature.TryPush(Creature.PushType.Magic, (creature.ragdoll.targetPart.transform.position - itemMagicProjectile.transform.position).normalized, 1);
                lastPushTimes[creature] = Time.time;
                creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
            }
        }

        int numEvents = coreEffectInstance.GetParticleSystem("SmallCrystals").GetCollisionEvents(other, collisionEvents);
        for (int i = 0; i < numEvents; i++)
        {
            ParticleCollisionEvent collisionEvent = collisionEvents[i];
            fireHitEffectData.Spawn(collisionEvent.intersection, Quaternion.LookRotation(-collisionEvent.normal, collisionEvent.colliderComponent.transform.up), collisionEvent.colliderComponent.transform).Play();
        }
    }

    public IEnumerator ActivationCoroutine(ItemMagicProjectile projectile)
    {
        yield return Yielders.ForSeconds(dragDelay);
        projectile.item.physicBody.drag = drag;

        yield return Yielders.ForSeconds(coreFireDelay - dragDelay);
        coreEffectInstance = coreFireEffectData.Spawn(projectile.transform);
        coreEffectInstance.OnParticleCollisionEvent -= OnParticleCollisionEvent;
        coreEffectInstance.OnParticleCollisionEvent += OnParticleCollisionEvent;
        coreEffectInstance.Play();
        coreEffectInstance.SetIntensity(1f);

        SpellCastCrystallic spellCastCrystallic = Catalog.GetData<SpellCastCrystallic>("Crystallic");
        int num = Random.Range(5, 9);

        for (int index = 0; index < num; ++index)
        {
            Vector3 vector3 = Random.insideUnitSphere * 1.25f;
            spellCastCrystallic.FireShard(spellCastCrystallic.shardEffectData, projectile.transform.position + vector3, projectile.transform.position + vector3.normalized * 8f, 2f, 1.0f);
        }

        yield return Yielders.ForSeconds(coreFireLifetime);
        ClearCore();
    }

    public void ClearCore()
    {
        if (coreEffectInstance == null)
            return;
        
        coreEffectInstance.OnParticleCollisionEvent -= OnParticleCollisionEvent;
        coreEffectInstance.SetParent(null);
        coreEffectInstance.End();
        coreEffectInstance = null;

        if (!itemMagicProjectile)
            return;
        
        itemMagicProjectile.item.DisallowDespawn = false;
        itemMagicProjectile.effectInstance.End();
    }
    #endif
}