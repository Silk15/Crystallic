using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crystallic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public static class Utils
{
    public static void AddForce(this Player player, Vector3 direction, float speed)
    {
        Player.local.locomotion.velocity = direction.normalized * Player.local.locomotion.velocity.magnitude;
        Player.local.locomotion.physicBody.AddForce(direction.normalized * speed, ForceMode.VelocityChange);
    }

    public static Vector3 GenerateOffsetVector(Vector3 origin, Vector2 minMaxX, Vector2 minMaxZ, float minDistFromOrigin, float yOffset, bool log = false)
    {
        float xOffset, zOffset;
        do
        {
            xOffset = Random.Range(minMaxX.x, minMaxX.y);
            zOffset = Random.Range(minMaxZ.x, minMaxZ.y);
        } while (Mathf.Sqrt(xOffset * xOffset + zOffset * zOffset) < minDistFromOrigin);

        var targetPosition = new Vector3(origin.x + xOffset, origin.y + yOffset, origin.z + zOffset);
        if (log) Debug.Log($"X Offset: {xOffset}, Z Offset: {zOffset}");
        return targetPosition;
    }

    public static Vector3 GenerateVectorBasedOn(Transform origin, Vector2 minMaxX, Vector2 minMaxZ, float yOffset, bool log = false)
    {
        var playerForward = origin.forward;
        var playerRight = origin.right;
        var forwardOffset = Random.Range(minMaxZ.x, minMaxZ.y);
        var lateralOffset = Random.Range(minMaxX.x, minMaxX.y);
        var offset = playerForward * forwardOffset + playerRight * lateralOffset;
        var targetPosition = origin.position + offset;
        if (log) Debug.Log($"Forward Offset: {forwardOffset}, Lateral Offset: {lateralOffset}");
        return new Vector3(targetPosition.x, origin.position.y * yOffset, targetPosition.z);
    }

    public static List<Vector3> GetListedPointsOnHemisphere(Vector3 origin, Vector3 direction, Vector2 minMaxCount, float exclusionRadius, float offsetFromOrigin)
    {
        var pointsList = new List<Vector3>();
        var pointsCount = Random.Range(Mathf.FloorToInt(minMaxCount.x), Mathf.FloorToInt(minMaxCount.y) + 1);
        for (var i = 0; i < pointsCount; i++)
        {
            Vector3 randomDirection;
            do
            {
                randomDirection = Random.onUnitSphere;
            } while (Vector3.Dot(randomDirection, direction) < Mathf.Cos(exclusionRadius * Mathf.Deg2Rad));

            randomDirection = origin + randomDirection * offsetFromOrigin;
            pointsList.Add(randomDirection);
        }

        return pointsList;
    }

    public static Vector3 GetDeflectNormal(float beamLength, Vector3 defaultNormal, Transform origin, LayerMask mask)
    {
        var raycastHitList = new List<RaycastHit>();
        for (var index = 0; index < 3; ++index)
        {
            RaycastHit hit;
            if (Raycast(Quaternion.AngleAxis(120f * index, Vector3.forward) * Vector3.up * 0.01f, origin, mask, beamLength, out hit)) raycastHitList.Add(hit);
        }

        switch (raycastHitList.Count)
        {
            case 0:
                return defaultNormal;
            case 3:
                var plane = new Plane();
                ref var local = ref plane;
                var raycastHit = raycastHitList[0];
                var point1 = raycastHit.point;
                raycastHit = raycastHitList[1];
                var point2 = raycastHit.point;
                raycastHit = raycastHitList[2];
                var point3 = raycastHit.point;
                local = new Plane(point1, point2, point3);
                if (Vector3.Dot(plane.normal, origin.forward) > 0.0) plane.Flip();
                return plane.normal;
            default:
                var vector3 = defaultNormal;
                for (var index = 0; index < raycastHitList.Count; ++index) vector3 += raycastHitList[index].normal;
                return vector3 / (raycastHitList.Count + 1);
        }
    }

    public static bool Raycast(Vector3 offset, Transform origin, LayerMask mask, float distance, out RaycastHit hit)
    {
        return Physics.Raycast(origin.position + origin.TransformPoint(offset), origin.forward, out hit, distance, mask, QueryTriggerInteraction.Ignore);
    }
    
    public static ConfigurableJoint CreateConfigurableJoint(Rigidbody source, Rigidbody target, float spring, float damper, float minDistance, float maxDistance, float massScale, bool enableCollision = true, ConfigurableJointMotion motion = ConfigurableJointMotion.Free)
    {
        ConfigurableJoint configurableJoint = target.gameObject.AddComponent<ConfigurableJoint>();
        configurableJoint.connectedBody = source;
        configurableJoint.enableCollision = enableCollision;
        SoftJointLimit linearLimit = new SoftJointLimit
        {
            contactDistance = minDistance,
            limit = maxDistance
        };
        configurableJoint.linearLimit = linearLimit;
        JointDrive drive = new JointDrive
        {
            positionSpring = spring,
            positionDamper = damper,
            maximumForce = Mathf.Infinity
        };
        configurableJoint.xDrive = drive;
        configurableJoint.yDrive = drive;
        configurableJoint.zDrive = drive;
        configurableJoint.massScale = massScale;
        configurableJoint.angularXMotion = ConfigurableJointMotion.Free;
        configurableJoint.angularYMotion = ConfigurableJointMotion.Free;
        configurableJoint.angularZMotion = ConfigurableJointMotion.Free;

        return configurableJoint;
    }
}

public static class GameObjectExtensions
{
    public static bool TryGetComponents<T>(this GameObject gameObject, out T[] components) where T : Component
    {
        components = gameObject.GetComponents<T>();
        return components.Length > 0;
    }

    public static bool TryGetComponentInParent<T>(this GameObject gameObject, out T component) where T : Component
    {
        component = gameObject.GetComponentInParent<T>();
        return component != null;
    }

    public static bool TryGetComponentsInParent<T>(this GameObject gameObject, out T[] components) where T : Component
    {
        components = gameObject.GetComponentsInParent<T>();
        return components.Length > 0;
    }

    public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T component) where T : Component
    {
        component = gameObject.GetComponentInChildren<T>();
        return component != null;
    }

    public static bool TryGetComponentsInChildren<T>(this GameObject gameObject, out T[] components) where T : Component
    {
        components = gameObject.GetComponentsInChildren<T>();
        return components.Length > 0;
    }

    public static List<T> GetComponentsInImmediateChildren<T>(this Transform origin) where T : Component
    {
        var components = new List<T>();
        foreach (Transform child in origin)
        {
            var component = child.GetComponent<T>();
            if (component != null) components.Add(component);
        }

        return components;
    }

    public static T GetComponentInImmediateChildren<T>(this Transform origin) where T : Component
    {
        var component = (T)null;
        foreach (Transform child in origin)
        {
            var component1 = child.GetComponent<T>();
            if (component1 != null) component = component1;
        }

        return component;
    }

    public static List<T> GetComponentsInImmediateParent<T>(this Transform origin) where T : Component
    {
        var components = new List<T>();
        components.AddRange(origin.parent.GetComponents<T>());
        return components;
    }

    public static T GetComponentInImmediateParent<T>(this Transform origin) where T : Component
    {
        var component = (T)null;
        component = origin.parent.GetComponent<T>();
        return component;
    }

    public static Transform GetChildByNameRecursive(this Transform parent, string nameToCheck)
    {
        foreach (Transform child in parent)
        {
            if (child.name == nameToCheck) return child;
            var foundInDescendants = GetChildByNameRecursive(child, nameToCheck);
            if (foundInDescendants != null) return foundInDescendants;
        }
        return null;
    }

    public static List<Transform> GetChildrenByNameRecursive(this Transform parent, string nameToCheck)
    {
        var children = new List<Transform>();
        foreach (Transform child in parent)
        {
            if (child.name == nameToCheck) children.Add(child);
            children.AddRange(child.GetChildrenByNameRecursive(nameToCheck));
        }

        return children;
    }

    public static Transform GetMatchingChild(this Transform origin, string keyword)
    {
        foreach (Transform child in origin)
            if (child.name.Contains(keyword))
                return child;
        return null;
    }

    public static List<Transform> GetMatchingChildren(this Transform origin, string keyword)
    {
        var children = new List<Transform>();
        foreach (Transform child in origin)
            if (child.name.Contains(keyword))
                children.Add(child);
        return children;
    }

    public static void SmoothLookAt(this Transform transform, Transform target, float duration)
    {
        GameManager.local.StartCoroutine(SmoothLookRoutine(transform, target, duration));
    }

    private static IEnumerator SmoothLookRoutine(Transform transform, Transform target, float duration)
    {
        var timeElapsed = 0f;
        var initialRotation = transform.rotation;
        var targetRotation = Quaternion.LookRotation(target.position - transform.position);
        while (timeElapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRotation;
    }
}

public static class ThunderEntityExtensions
{
    public static void Despawn(this ThunderEntity entity, float time)
    {
        entity.RunAfter(() => { entity.Despawn(); }, time);
    }
}

public static class CreatureExtensions
{
    public static bool GetClosestPart(this Ragdoll ragdoll, Vector3 position, float maxDistance, out RagdollPart ragdollPart)
    {
        ragdollPart = null;
        var closestDistance = maxDistance;

        foreach (var part in ragdoll.parts)
        {
            var distance = Vector3.Distance(part.transform.position, position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                ragdollPart = part;
            }
        }

        return ragdollPart != null;
    }

    public static bool Active(this Creature creature)
    {
        return !creature.isKilled && !creature.isCulled;
    }

    public static void DamagePatched(this Creature creature, float damage, DamageType damageType)
    {
        // TODO: replace all usages of this with vanilla damage method once game is patched, and remove this method
        creature.Damage(new CollisionInstance(new DamageStruct(damageType, damage) { hitRagdollPart = creature.ragdoll.targetPart }));
    }

    public static void Disarm(this Creature creature)
    {
        creature.handLeft.TryRelease();
        creature.handRight.TryRelease();
    }

    public static Creature GetClosestCreature(this Creature creature, float maxDistance)
    {
        Creature closestCreature = null;
        var lastCreatureDist = Mathf.Infinity;
        foreach (var creature1 in Creature.allActive)
        {
            if (creature.isPlayer) continue;
            var creatureDist = Vector3.Distance(creature.ragdoll.targetPart.transform.position, creature.transform.position);
            if (creatureDist < lastCreatureDist && creatureDist <= maxDistance)
            {
                closestCreature = creature1;
                lastCreatureDist = creatureDist;
            }
        }

        return closestCreature;
    }

    public static void Shred(this Creature creature)
    {
        creature.Kill();
        for (var index = creature.ragdoll.parts.Count - 1; index >= 0; --index)
        {
            var part = creature.ragdoll.parts[index];
            if (creature.ragdoll.rootPart != part && part.sliceAllowed) part.TrySlice();
        }
    }
}

public static class ItemExtensions
{
    public static bool HasImbue(this List<Imbue> imbues, string id)
    {
        var containsImbue = false;
        for (var i = 0; i < imbues.Count; i++)
            if (imbues[i].spellCastBase.id == id)
                containsImbue = true;
        return containsImbue;
    }

    public static void PlayHapticClip(this Item item, AnimationCurve curve, float time)
    {
        foreach (var hand in item.handlers) hand.PlayHapticClipOver(curve, time);
    }

    public static void PointItemFlyRefAtTarget(this Item item, Vector3 target, float lerpFactor, Vector3? upDir = null)
    {
        var up = upDir ?? Vector3.up;
        if (item.flyDirRef)
        {
            item.transform.rotation = Quaternion.Slerp(item.transform.rotation * item.flyDirRef.localRotation, Quaternion.LookRotation(target, up), lerpFactor) * Quaternion.Inverse(item.flyDirRef.localRotation);
        }
        else if (item.holderPoint)
        {
            item.transform.rotation = Quaternion.Slerp(item.transform.rotation * item.holderPoint.localRotation, Quaternion.LookRotation(target, up), lerpFactor) * Quaternion.Inverse(item.holderPoint.localRotation);
        }
        else
        {
            var pointDir = Quaternion.LookRotation(item.transform.up, up);
            item.transform.rotation = Quaternion.Slerp(item.transform.rotation * pointDir, Quaternion.LookRotation(target, up), lerpFactor) * Quaternion.Inverse(pointDir);
        }
    }

    public static Quaternion GetFlyDirRefLocalRotation(this Item item)
    {
        return Quaternion.Inverse(item.transform.rotation) * item.flyDirRef.rotation;
    }

    public static void IgnoreCollider(this Item item, Collider collider, bool ignore)
    {
        foreach (var group in item.colliderGroups)
        foreach (var itemCollider in group.colliders)
            Physics.IgnoreCollision(collider, itemCollider, ignore);
    }

    public static Collider GetFurthestCollider(this ColliderGroup colliderGroup, Vector3 point)
    {
        Collider result = null;
        foreach (var collider in colliderGroup.colliders)
            if (result == null || Vector3.Distance(collider.transform.position, point) > Vector3.Distance(result.transform.position, point))
                result = collider;
        return result;
    }

    public static Collider GetClosestCollider(this ColliderGroup colliderGroup, Vector3 point)
    {
        Collider result = null;
        foreach (var collider in colliderGroup.colliders)
            if (result == null || Vector3.Distance(collider.transform.position, point) < Vector3.Distance(result.transform.position, point))
                result = collider;
        return result;
    }
}

public static class ReflectionExtensions
{
    public static object GetField(this object obj, string fieldName, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    {
        var field = obj.GetType().GetField(fieldName, flags);
        if (field != null) return field.GetValue(obj);
        return null;
    }

    public static MethodInfo GetMethod(this object obj, string methodName, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    {
        var method = obj.GetType().GetMethod(methodName, flags);
        if (method != null) return method;
        return null;
    }

    public static void InvokeMethod(this object obj, string methodName, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    {
        var method = obj.GetType().GetMethod(methodName, flags);
        if (method != null) method.Invoke(obj, null);
    }
}

public static class EffectInstanceExtensions
{
    public static void SetParticleTarget(this EffectInstance effectInstance, ForceFieldPresetData presetData, Transform transform, bool useTrigger = true, float triggerRadius = 0.05f)
    {
        var forceField = presetData.Create(effectInstance, transform);
        var sphereCollider = useTrigger ? forceField.gameObject.AddComponent<SphereCollider>() : null;
        if (sphereCollider != null)
        {
            sphereCollider.radius = triggerRadius;
            sphereCollider.isTrigger = true;
        }

        foreach (var particleSystem in effectInstance.GetParticleSystems())
        {
            var trigger = particleSystem.trigger;
            var externalForces = particleSystem.externalForces;
            if (sphereCollider != null) trigger.AddCollider(sphereCollider);
            if (forceField != null) externalForces.AddInfluence(forceField);
        }
    }

    public static void ClearParticleTarget(this EffectInstance effectInstance)
    {
        var obj = effectInstance?.GetRootParticleSystem()?.gameObject;
        var forceField = obj?.GetComponent<ParticleSystemForceField>();
        var sphereCollider = obj?.gameObject.GetComponent<SphereCollider>();
        foreach (var particleSystem in effectInstance.GetParticleSystems())
        {
            var trigger = particleSystem.trigger;
            var externalForces = particleSystem.externalForces;
            if (sphereCollider != null) trigger.RemoveCollider(sphereCollider);
            if (forceField != null) externalForces.RemoveInfluence(forceField);
            GameObject.Destroy(forceField);
            GameObject.Destroy(sphereCollider);
        }
    }

    public static void SetColorImmediate(this EffectInstance effectInstance, Color color)
    {
        var particleSystems = effectInstance.GetParticleSystems();
        foreach (var particleSystem in particleSystems)
        {
            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var minMaxGradient = colorOverLifetime.color;
            minMaxGradient.color = color;
            colorOverLifetime.color = color;
        }
    }

    public static VisualEffect GetVfxGraph(this EffectInstance instance)
    {
        VisualEffect visualEffect = null;
        if (instance != null)
            if (instance.effects != null && instance.effects.Count > 0)
                foreach (var effectVfx in instance.effects.OfType<EffectVfx>())
                    visualEffect = effectVfx.vfx;
        return visualEffect;
    }

    public static void LerpIntensity(this EffectInstance effectInstance, float a, float b, float duration, bool endOnFinish)
    {
        GameManager.local.StartCoroutine(FadeIntensity(effectInstance, a, b, duration, endOnFinish));
    }

    private static IEnumerator FadeIntensity(EffectInstance effectInstance, float a, float b, float duration, bool endOnFinished)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            var intensity = Mathf.Lerp(a, b, elapsed / duration);
            effectInstance.SetIntensity(intensity);
            elapsed += Time.deltaTime;
            yield return null;
        }

        effectInstance.SetIntensity(b);
        if (endOnFinished) effectInstance.End();
    }

    public static ParticleSystem[] GetParticleSystems(this EffectInstance instance)
    {
        var particleSystems = new List<ParticleSystem>();
        if (instance != null)
            if (instance.effects != null && instance.effects.Count > 0)
                foreach (var effectParticle in instance.effects.OfType<EffectParticle>())
                    if (effectParticle?.rootParticleSystem?.gameObject != null)
                        particleSystems.AddRange(effectParticle.rootParticleSystem.gameObject.GetComponentsInChildren<ParticleSystem>());
        return particleSystems.ToArray();
    }

    public static ParticleSystem[] GetParticleSystems(this List<EffectInstance> instances)
    {
        var particleSystems = new List<ParticleSystem>();
        if (instances != null && instances.Count > 0)
            for (var index = 0; index < instances.Count; index++)
            {
                var instance = instances[index];
                if (instance != null && instance.effects != null && instance.effects.Count > 0)
                    foreach (var effectParticle in instance.effects.OfType<EffectParticle>())
                        if (effectParticle?.rootParticleSystem?.gameObject != null)
                            particleSystems.AddRange(effectParticle.rootParticleSystem.gameObject.GetComponentsInChildren<ParticleSystem>());
            }

        return particleSystems.ToArray();
    }

    public static ParticleSystem GetRootParticleSystem(this EffectInstance instance)
    {
        if (instance != null)
            if (instance.effects != null && instance.effects.Count > 0)
                foreach (var effectParticle in instance.effects.OfType<EffectParticle>())
                    if (effectParticle?.rootParticleSystem?.gameObject != null)
                        return effectParticle.rootParticleSystem;
        return null;
    }

    public static ParticleSystem GetParticleSystem(this EffectInstance instance, string name)
    {
        var systems = instance.GetParticleSystems();
        foreach (var system in systems)
            if (system.name == name)
                return system;
        return null;
    }

    public static void SetMaxParticles(this List<EffectInstance> effectInstances, int max)
    {
        var particles = effectInstances.GetParticleSystems();
        if (particles != null && particles.Length > 0)
            foreach (var particle in particles)
            {
                var main = particle.main;
                main.maxParticles = 45;
            }
    }

    public static void SetMaxParticles(this EffectInstance effectInstance, int max)
    {
        var particles = effectInstance.GetParticleSystems();
        if (particles != null && particles.Length > 0)
            foreach (var particle in particles)
            {
                var main = particle.main;
                main.maxParticles = 45;
            }
    }

    public static bool isEmitting(this EffectInstance effectInstance)
    {
        var isEmitting = false;
        foreach (var particleSystem in effectInstance.GetParticleSystems())
            if (particleSystem != null && particleSystem.isEmitting)
                isEmitting = true;
        return isEmitting;
    }

    public static void SetSpeed(this EffectInstance effectInstance, float value, string name)
    {
        foreach (var particleSystem in effectInstance.GetParticleSystems())
            if (particleSystem != null && particleSystem.gameObject.name == name)
            {
                var particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
                var numParticlesAlive = particleSystem.GetParticles(particles);
                for (var i = 0; i < numParticlesAlive; i++)
                {
                    var particle = particles[i];
                    particle.velocity = particle.velocity.normalized * value;
                    particles[i] = particle;
                }

                particleSystem.SetParticles(particles, numParticlesAlive);
            }
    }

    public static void SetLifetime(this EffectInstance effectInstance, float lifetimeValue, string name)
    {
        foreach (var particleSystem in effectInstance.GetParticleSystems())
            if (particleSystem != null && particleSystem.gameObject.name == name)
            {
                var particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
                var numParticlesAlive = particleSystem.GetParticles(particles);
                for (var i = 0; i < numParticlesAlive; i++)
                {
                    var particle = particles[i];
                    particle.remainingLifetime = lifetimeValue;
                    particles[i] = particle;
                }

                particleSystem.SetParticles(particles, numParticlesAlive);
            }
    }

    public static void SetConeAngle(this EffectInstance effectInstance, float coneAngleValue, string name)
    {
        foreach (var particleSystem in effectInstance.GetParticleSystems())
            if (particleSystem != null && particleSystem.gameObject.name == name)
            {
                var shape = particleSystem.shape;
                shape.angle = coneAngleValue;
            }
    }

    public static void ForceStop(this EffectInstance effectInstance, ParticleSystemStopBehavior stopBehavior)
    {
        foreach (var particleSystem in effectInstance.GetParticleSystems()) particleSystem.Stop(true, stopBehavior);
    }

    public static void ForceStop(this List<EffectInstance> effectInstances, ParticleSystemStopBehavior stopBehavior)
    {
        foreach (var particleSystem in effectInstances.GetParticleSystems()) particleSystem.Stop(true, stopBehavior);
    }
}