using System;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

[Serializable]
public class ForceFieldPresetData : CustomData
{
    public ParticleSystemForceFieldShape shape;
    public float startRange;
    public float endRange;
    public Vector3 direction;
    public float gravityStrength;
    public float gravityFocus;
    public float rotationSpeed;
    public float rotationAttraction;
    public Vector2 rotationRandomness;
    public float dragStrength;
    public bool mutliplyBySize;
    public bool multiplyByVelocity;

    public ParticleSystemForceField Create(EffectInstance effectInstance, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var forceField = Setup(effectInstance?.GetRootParticleSystem()?.gameObject);
        forceField.transform.position = position;
        forceField.transform.rotation = rotation;
        forceField.transform.parent = parent;
        return forceField;
    }

    public ParticleSystemForceField Create(EffectInstance effectInstance, Transform transform)
    {
        var forceField = Setup(effectInstance?.GetRootParticleSystem()?.gameObject);
        forceField.transform.position = transform.position;
        forceField.transform.rotation = transform.rotation;
        forceField.transform.parent = transform;
        return forceField;
    }

    public ParticleSystemForceField Setup(GameObject gameObject)
    {
        var forceField = gameObject?.AddComponent<ParticleSystemForceField>();
        if (forceField == null) return null;
        forceField.shape = shape;
        forceField.startRange = startRange;
        forceField.endRange = endRange;
        forceField.directionX = direction.x;
        forceField.directionY = direction.y;
        forceField.directionZ = direction.z;
        forceField.gravity = gravityStrength;
        forceField.rotationSpeed = rotationSpeed;
        forceField.rotationAttraction = rotationAttraction;
        forceField.rotationRandomness = rotationRandomness;
        forceField.drag = dragStrength;
        forceField.multiplyDragByParticleSize = mutliplyBySize;
        forceField.multiplyDragByParticleVelocity = multiplyByVelocity;
        return forceField;
    }
}