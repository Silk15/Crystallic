using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class GolemPart : ThunderBehaviour
{
    public Part part;
    public List<GolemCrystal> crystals = new();
    public List<Collider> colliders = new();
    public List<Collider> hitboxes = new();
    public List<Handle> ladders = new();
    public List<GolemPart> childParts = new();

    public virtual void Awake()
    {
        Clear();
        var crystalsInChild = transform.GetMatchingChild("Crystal");
        var laddersInChild = transform.GetMatchingChild("Ladder");
        var collidersInChild = transform.GetMatchingChild("Collider");
        if (crystalsInChild != null)
            foreach (var crystal in crystalsInChild.GetComponentsInChildren<GolemCrystal>())
                crystals.Add(crystal);
        if (laddersInChild != null)
            foreach (var handle in laddersInChild.GetComponentsInChildren<Handle>())
                ladders.Add(handle);
        if (collidersInChild != null)
            foreach (var collider1 in collidersInChild.GetComponentsInChildren<Collider>(transform))
                if (!collider1.isTrigger)
                    colliders.Add(collider1);
        foreach (var collider in transform.GetComponentsInImmediateChildren<Collider>())
            if (collider.isTrigger)
                hitboxes.Add(collider);
    }

    public void OnDestroy()
    {
        Clear();
    }

    public virtual EffectInstance SpawnEffect(EffectData effectData, bool useTransform, bool parentToPart)
    {
        if (useTransform) return effectData.Spawn(transform);
        var parent = parentToPart ? transform : null;
        return effectData.Spawn(transform.position, transform.rotation, parent);
    }

    public virtual void Clear()
    {
        crystals.Clear();
        ladders.Clear();
        colliders.Clear();
        hitboxes.Clear();
        childParts.Clear();
    }

    public virtual void UpdateChildParts()
    {
        childParts.AddRange(GetComponentsInChildren<GolemPart>());
    }
}