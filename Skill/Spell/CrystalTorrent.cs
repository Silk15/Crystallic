using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Pools;
using ThunderRoad.Skill.Spell;
using UnityEngine;
using UnityEngine.Serialization;

namespace Crystallic.Skill.Spell;

public class CrystalTorrent : ThunderBehaviour
{ 
    public float hapticDelayMultiplier;
    public float buildupTime = 5f;
    
    [NonSerialized]
    public float lastHapticTime;
    
    [NonSerialized]
    public float nextHapticDelay;
    
    [NonSerialized]
    public bool active;
    
    [NonSerialized]
    public SkillCrystalTorrent skillCrystalTorrent;
    
    #if !SDK
    [NonSerialized]
    public EffectInstance torrentEffect;
    #endif
    
    [NonSerialized]
    public Transform torrentTransform;
    
    [NonSerialized]
    public Transform overrideTarget;
    
    [NonSerialized]
    public Coroutine chargeCoroutine;
    
    [NonSerialized]
    public ThunderRoad.Imbue imbue;
    
    [NonSerialized]
    public ThunderEntity caster;
    
    [NonSerialized]
    public Item item;
    
    private List<ParticleCollisionEvent> collisionEvents = new();

    #if !SDK
    public override ManagedLoops EnabledManagedLoops => ManagedLoops.FixedUpdate;

    public void Fire(SkillCrystalTorrent skillCrystalTorrent, ThunderEntity caster, ThunderRoad.Imbue imbue, bool active)
    {
        this.skillCrystalTorrent = skillCrystalTorrent;
        this.active = active;
        this.caster = caster;
        this.imbue = imbue;

        if (imbue != null) 
            item = imbue.colliderGroup.collisionHandler.item;
        hapticDelayMultiplier = 6f;
        nextHapticDelay = 0.1f;

        if (active)
            chargeCoroutine = StartCoroutine(ChargeCoroutine());
        
        else
        {
            StopCoroutine(chargeCoroutine);
            if (torrentEffect != null)
            {
                PoolUtils.GetTransformPoolManager().Release(torrentTransform);
                torrentEffect.OnParticleCollisionEvent -= OnParticleCollisionEvent;
                torrentEffect.End();
                torrentEffect = null;
            }
        }
    }

    public IEnumerator ChargeCoroutine()
    {
        torrentTransform = PoolUtils.GetTransformPoolManager().Get();
        
        torrentEffect = skillCrystalTorrent.torrentEffectData.Spawn(torrentTransform.transform);
        torrentEffect.OnParticleCollisionEvent -= OnParticleCollisionEvent;
        torrentEffect.OnParticleCollisionEvent += OnParticleCollisionEvent;
        torrentEffect.Play();
    
        float current = 0f;

        while (current < buildupTime)
        {
            hapticDelayMultiplier = buildupTime - current;
            
            if (!Mathf.Approximately(torrentEffect.GetIntensity(), 1f)) 
                torrentEffect.SetIntensity(current / buildupTime);
            
            current += Time.deltaTime;
            yield return null;
        }
        
    }

    protected override void ManagedFixedUpdate()
    {
        base.ManagedFixedUpdate();
        if (!active)
            return;
        
        if (overrideTarget != null) 
            torrentTransform.SetPositionAndRotation(overrideTarget.position, overrideTarget.rotation);
        
        if (!active || !item || !caster) 
            return;
        
        if (Time.time - lastHapticTime > nextHapticDelay)
        {
            nextHapticDelay = UnityEngine.Random.Range(0.04f, 0.02f) * hapticDelayMultiplier;
            lastHapticTime = Time.time;
            item.Haptic(3f, true);
        }
        
        torrentTransform?.SetPositionAndRotation(imbue.colliderGroup.imbueShoot.transform.position, imbue.colliderGroup.imbueShoot.transform.rotation);
    }

    private void OnParticleCollisionEvent(GameObject other)
    {
        if (other.TryGetComponentInParent(out Creature creature) && creature != caster && creature != caster)
        {
            if (!skillCrystalTorrent.lastPushTimes.ContainsKey(creature) || Time.time - skillCrystalTorrent.lastPushTimes[creature] >= 0.5f)
            {
                creature.TryPush(Creature.PushType.Magic, (creature.ragdoll.targetPart.transform.position - transform.position).normalized, 1);
                skillCrystalTorrent.lastPushTimes[creature] = Time.time;
                
                if (skillCrystalTorrent.crystallisationChance.RandomRangeInt() == 0)
                    creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
            }
        }
        
        int numEvents = torrentEffect.GetParticleSystem("SmallCrystals").GetCollisionEvents(other, collisionEvents);
        for (int i = 0; i < numEvents; i++)
        {
            ParticleCollisionEvent collisionEvent = collisionEvents[i];
            skillCrystalTorrent.hitShardEffectData.Spawn(collisionEvent.intersection, Quaternion.LookRotation(-collisionEvent.normal, collisionEvent.colliderComponent.transform.up), collisionEvent.colliderComponent.transform).Play();
        }
    }
    #endif
}