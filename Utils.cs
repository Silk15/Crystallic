using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThunderRoad;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public static class Utils
{
    public static int GetProjectileRaycastMask()
    {
        int raycastMask = 0;
        raycastMask |= 1 << GameManager.GetLayer(LayerName.Default);
        raycastMask |= 1 << GameManager.GetLayer(LayerName.PlayerLocomotionObject);
        raycastMask |= 1 << GameManager.GetLayer(LayerName.NPC);
        raycastMask |= 1 << GameManager.GetLayer(LayerName.Ragdoll);
        return raycastMask;
    }
    
    public static bool Validate(Func<bool> predicate, Action onTrue = null, Action onFalse = null)
    {
        if (predicate())
        {
            onTrue?.Invoke();
            return true;
        }

        onFalse?.Invoke();
        return false;
    }

    public static void RunOnValid<T>(IEnumerable<T> collection, Func<T, bool> predicate, Action<T> onConditionMet)
    {
        foreach (var item in collection)
            if (predicate(item))
                onConditionMet(item);
    }

    public static T Retry<T>(Func<T> func, int retries, Func<Exception, bool> shouldRetry = null)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                if (i == retries - 1 || (shouldRetry != null && !shouldRetry(ex))) throw;
            }
        }

        return default;
    }

    public static void DoWhen(Func<bool> predicate, Action action, int maxIterations = 1)
    {
        int i = 0;
        while (predicate() && i++ < maxIterations) action();
    }


    public static T Tap<T>(T value, Action<T> sideEffect)
    {
        sideEffect(value);
        return value;
    }

    public static T TryOrDefault<T>(Func<T> func, T fallback = default)
    {
        try
        {
            return func();
        }
        catch
        {
            return fallback;
        }
    }

    public static List<ThunderEntity> AddExplosionForceInRadius(Vector3 position, float radius, float force,
        float upwardsModifier, ForceMode forceMode, Func<ThunderEntity, bool> filter)
    {
        var alloc = ThunderEntity.InRadius(position, radius, filter);
        foreach (ThunderEntity entity in alloc)
            entity.AddExplosionForce(force, position, radius, upwardsModifier, forceMode);
        return alloc;
    }

    public static void AddForce(this Player player, Vector3 direction, float speed)
    {
        Player.local.locomotion.velocity = direction.normalized * Player.local.locomotion.velocity.magnitude;
        Player.local.locomotion.physicBody.AddForce(direction.normalized * speed, ForceMode.VelocityChange);
    }
    
    public static Vector3 GetRandomVelocityInCone(Vector3 centerDirection, float coneAngle, float magnitude, float minAngle = 0f)
    {
        centerDirection.Normalize();

        float angle = UnityEngine.Random.Range(minAngle, coneAngle);
        float angleInRads = angle * Mathf.Deg2Rad;
        float azimuthAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);

        Vector3 randomDirection = new Vector3(
            Mathf.Cos(azimuthAngle) * Mathf.Sin(angleInRads),
            Mathf.Sin(azimuthAngle) * Mathf.Sin(angleInRads),
            Mathf.Cos(angleInRads)
        );

        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, centerDirection);
        randomDirection = rotation * randomDirection;

        return randomDirection * magnitude;
    }

    public static Vector3 GetRandomDirectionInCone(Vector3 forward, float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float z = Mathf.Cos(angleRadians * Random.value);
        float sinT = Mathf.Sqrt(1 - z * z);
        float phi = Random.Range(0, 2 * Mathf.PI);
        float x = sinT * Mathf.Cos(phi);
        float y = sinT * Mathf.Sin(phi);
        Vector3 localDirection = new Vector3(x, y, z);
        return Quaternion.FromToRotation(Vector3.forward, forward) * localDirection;
    }

    public static Vector3 GenerateOffsetVector(Vector3 origin, Vector2 minMaxX, Vector2 minMaxZ,
        float minDistFromOrigin, float yOffset)
    {
        float xOffset, zOffset;
        do
        {
            xOffset = Random.Range(minMaxX.x, minMaxX.y);
            zOffset = Random.Range(minMaxZ.x, minMaxZ.y);
        } while (Mathf.Sqrt(xOffset * xOffset + zOffset * zOffset) < minDistFromOrigin);

        var targetPosition = new Vector3(origin.x + xOffset, origin.y + yOffset, origin.z + zOffset);
        return targetPosition;
    }

    public static Vector3 GenerateOffsetVectorBasedOn(Transform origin, Vector2 minMaxX, Vector2 minMaxZ, float yOffset)
    {
        var playerForward = origin.forward;
        var playerRight = origin.right;
        var forwardOffset = Random.Range(minMaxZ.x, minMaxZ.y);
        var lateralOffset = Random.Range(minMaxX.x, minMaxX.y);
        var offset = playerForward * forwardOffset + playerRight * lateralOffset;
        var targetPosition = origin.position + offset;
        return new Vector3(targetPosition.x, origin.position.y * yOffset, targetPosition.z);
    }

    public static List<Vector3> GetListedPointsOnHemisphere(Vector3 origin, Vector3 direction, Vector2 minMaxCount,
        float exclusionRadius, float offsetFromOrigin)
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
            if (Raycast(Quaternion.AngleAxis(120f * index, Vector3.forward) * Vector3.up * 0.01f, origin, mask,
                    beamLength, out hit)) raycastHitList.Add(hit);
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

    public static bool Raycast(Vector3 offset, Transform origin, LayerMask mask, float distance, out RaycastHit hit) =>
        Physics.Raycast(origin.position + origin.TransformPoint(offset), origin.forward, out hit, distance, mask,
            QueryTriggerInteraction.Ignore);

    public static ConfigurableJoint CreateConfigurableJoint(
        Rigidbody source,
        Rigidbody target,
        float spring = 50f,
        float damper = 5f,
        float minDistance = 0f,
        float maxDistance = 1f,
        float massScale = 1f,
        bool enableCollision = true,
        ConfigurableJointMotion motion = ConfigurableJointMotion.Free,
        Action<ConfigurableJoint> customSetup = null)
    {
        var joint = target.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = source;
        joint.enableCollision = enableCollision;

        var limit = new SoftJointLimit { contactDistance = minDistance, limit = maxDistance };
        joint.linearLimit = limit;

        var drive = new JointDrive
        {
            positionSpring = spring,
            positionDamper = damper,
            maximumForce = Mathf.Infinity
        };

        joint.xDrive = joint.yDrive = joint.zDrive = drive;
        joint.massScale = massScale;

        joint.angularXMotion = motion;
        joint.angularYMotion = motion;
        joint.angularZMotion = motion;

        customSetup?.Invoke(joint);
        return joint;
    }
    
    public static HingeJoint CreateHingeJoint(
        Rigidbody source,
        Rigidbody target,
        Vector3 anchor,
        Vector3 axis,
        bool useLimits = false,
        JointLimits? limits = null,
        Action<HingeJoint> customSetup = null)
    {
        var joint = target.gameObject.AddComponent<HingeJoint>();
        joint.connectedBody = source;
        joint.anchor = anchor;
        joint.axis = axis;

        joint.useLimits = useLimits;
        if (useLimits && limits.HasValue)
            joint.limits = limits.Value;

        customSetup?.Invoke(joint);
        return joint;
    }
    
    public static SpringJoint CreateSpringJoint(
        Rigidbody source,
        Rigidbody target,
        float spring = 10f,
        float damper = 1f,
        float minDistance = 0f,
        float maxDistance = 0f,
        Action<SpringJoint> customSetup = null)
    {
        var joint = target.gameObject.AddComponent<SpringJoint>();
        joint.connectedBody = source;
        joint.spring = spring;
        joint.damper = damper;
        joint.minDistance = minDistance;
        joint.maxDistance = maxDistance;

        customSetup?.Invoke(joint);
        return joint;
    }
    
    public static FixedJoint CreateFixedJoint(
        Rigidbody source,
        Rigidbody target,
        bool enableCollision = false,
        Action<FixedJoint> customSetup = null)
    {
        var joint = target.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = source;
        joint.enableCollision = enableCollision;

        customSetup?.Invoke(joint);
        return joint;
    }
    
    public static T CreateOrUpdateJoint<T>(
        object owner,
        string id,
        Func<T> factory,
        Action<T> setup = null) where T : Joint
    {
        return JointManager.CreateJoint(owner, id, () =>
        {
            var joint = factory();
            setup?.Invoke(joint);
            return joint;
        });
    }
}