using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crystallic.Skill;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic;

public class ImbueGravityBehavior : ImbueBehavior
{
    [ModOption("Joint Spring", "The spring applied to the joint connecting two physicBodies, this is the value that decides how tightly two limbs are bound, from loosely floaty to tight."), ModOptionCategory("Lithohammer", 23), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
    public static float spring = 550f;

    [ModOption("Joint Damper", "The damping applied to the joint connecting two physicBodies, this acts as a smoother, damping out movement to act floaty."), ModOptionCategory("Lithohammer", 23), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
    public static float damper = 30f;

    [ModOption("Min Joint Distance", "The min distance two physicBodies can be from one another."), ModOptionCategory("Lithohammer", 23), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float minDistance = 1f;

    [ModOption("Max Joint Distance", "The max distance two physicBodies can be from one another."), ModOptionCategory("Lithohammer", 23), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float maxDistance = 15f;
    
    [ModOption("Joint Lifetime", "The lifetime of each joint."), ModOptionCategory("Lithohammer", 23), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float lifetime = 3f;


    public StatusData statusData;
    public EffectData tetherEffectData;
    public EffectData snapEffectData;
    public string snapEffectId = "GravitySnap";
    public SpellCastGravity spellCastGravity;
    public string tetherEffectId = "GravityTether";
    public Dictionary<Creature, JointEffect> jointedBodies = new();

    public override void Activate(Imbue imbue, SkillCrystalImbueHandler handler)
    {
        base.Activate(imbue, handler);
        EventManager.onCreatureDespawn += OnCreatureDespawn;
        tetherEffectData = Catalog.GetData<EffectData>(tetherEffectId);
        snapEffectData = Catalog.GetData<EffectData>(snapEffectId);
        statusData = Catalog.GetData<StatusData>("Floating");
        spellCastGravity = Catalog.GetData<SpellCastCharge>("Gravity") as SpellCastGravity;
    }

    private void OnCreatureDespawn(Creature creature, EventTime eventTime)
    {
        if (jointedBodies.ContainsKey(creature))
        {
            Destroy(jointedBodies[creature].configurableJoint);
            jointedBodies[creature].effectInstance.End();
            jointedBodies.Remove(creature);
        }
    }

    public override void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge, Creature hitCreature = null, Item hitItem = null)
    {
        base.Hit(collisionInstance, spellCastCharge, hitCreature, hitItem);
        var item = collisionInstance?.sourceColliderGroup?.collisionHandler?.item;
        var part = collisionInstance?.targetColliderGroup?.collisionHandler?.ragdollPart;
        if (part && item && !part.ragdoll.creature.isPlayer && collisionInstance.impactVelocity.magnitude > 18) TryCreateJoint(collisionInstance, item, part);
    }

    public IEnumerator JointExpirationRoutine(Item source, RagdollPart ragdollPart)
    {
        var jointEffect = jointedBodies[ragdollPart.ragdoll.creature];
        yield return Yielders.ForSeconds(1);
        float startTime = Time.time;
        bool velocityMet = false;
        while (Time.time - startTime < 2f)
        {
            if (source.physicBody.velocity.magnitude < 12.5f)
            {
                velocityMet = true;
                break;
            }

            yield return Yielders.EndOfFrame;
        }
        if (velocityMet)
        {
            var currentPart = ragdollPart;

            while (currentPart != null)
            {
                if (currentPart.sliceAllowed)
                {
                    currentPart?.TrySlice();
                    currentPart.ragdoll.creature.Kill();
                    yield return Yielders.ForSeconds(lifetime);
                    break;
                }
                currentPart = currentPart.parentPart;
                yield return Yielders.EndOfFrame;
            }
        }
        snapEffectData.Spawn(jointEffect.configurableJoint.transform).Play();
        jointEffect.effectInstance.End(); 
        spellCastGravity.readyEffectData.Spawn(jointEffect.configurableJoint.transform).Play();
        jointEffect.configurableJoint.breakForce = 0f;
        Destroy(jointEffect.configurableJoint);
        jointedBodies.Remove(ragdollPart.ragdoll.creature);
    }

    public void TryCreateJoint(CollisionInstance collisionInstance, Item source, RagdollPart target)
    {
        if (jointedBodies.ContainsKey(target.ragdoll.creature)) return;
        var effectInstance = tetherEffectData.Spawn(collisionInstance.sourceCollider.transform);
        effectInstance.SetSource(collisionInstance.sourceCollider.transform);
        effectInstance.SetTarget(target.transform);
        effectInstance.Play();
        var joint = Utils.CreateConfigurableJoint(source.physicBody.rigidBody, target?.physicBody.rigidBody, spring, damper, minDistance, maxDistance, 1);
        jointedBodies.Add(target.ragdoll.creature, new JointEffect(effectInstance, joint));
        target.ragdoll.creature.Remove(statusData, this);
        StartCoroutine(JointExpirationRoutine(source, target));
    }

    public override void Deactivate()
    {
        base.Deactivate();
        foreach (var kvp in jointedBodies.ToList())
        {
            var creature = kvp.Key;
            var jointEffect = kvp.Value;
            if (jointEffect != null && jointEffect.effectInstance != null) jointEffect.effectInstance.End();
            if (jointEffect?.configurableJoint != null)
            {
                jointEffect.configurableJoint.breakForce = 0f;
                Destroy(jointEffect.configurableJoint);
            }

            jointedBodies.Remove(creature);
        }
    }
}

[Serializable]
public class JointEffect
{
    public EffectInstance effectInstance;
    public ConfigurableJoint configurableJoint;

    public JointEffect(EffectInstance effectInstance, ConfigurableJoint configurableJoint)
    {
        this.effectInstance = effectInstance;
        this.configurableJoint = configurableJoint;
    }
}