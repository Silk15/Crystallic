#if !SDK
using System;
using System.Collections.Generic;
using UnityEngine;

public static class JointManager
{
    private static readonly Dictionary<object, Dictionary<string, Joint>> joints = new();

    public static T CreateJoint<T>(object owner, string id, Func<T> creationFunc) where T : Joint
    {
        if (!joints.TryGetValue(owner, out var ownerJoints))
        {
            ownerJoints = new Dictionary<string, Joint>();
            joints[owner] = ownerJoints;
        }

        if (ownerJoints.TryGetValue(id, out var existingJoint))
            UnityEngine.Object.Destroy(existingJoint);

        T newJoint = creationFunc.Invoke();
        ownerJoints[id] = newJoint;

        return newJoint;
    }

    public static bool TryGetJoint<T>(object owner, string id, out T joint) where T : Joint
    {
        joint = null;
        if (joints.TryGetValue(owner, out var ownerJoints) && ownerJoints.TryGetValue(id, out var existingJoint))
        {
            joint = existingJoint as T;
            return joint != null;
        }

        return false;
    }

    public static void DestroyJoint(object owner, string id)
    {
        if (joints.TryGetValue(owner, out var ownerJoints) && ownerJoints.TryGetValue(id, out var existingJoint))
        {
            UnityEngine.Object.Destroy(existingJoint);
            ownerJoints.Remove(id);
            if (ownerJoints.Count == 0) joints.Remove(owner);
        }
    }

    public static void DestroyAllJoints(object owner)
    {
        if (joints.TryGetValue(owner, out var ownerJoints))
        {
            foreach (var joint in ownerJoints.Values)
                UnityEngine.Object.Destroy(joint);
            joints.Remove(owner);
        }
    }
}
#endif