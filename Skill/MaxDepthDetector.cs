using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill;

public class MaxDepthDetector : MonoBehaviour
{
    public delegate void OnPenetrateMaxDepth(Damager damager, CollisionInstance collisionInstance, Vector3 velocity, float depth);

    public Damager damager;
    public bool active;
    public bool hasReachedMaxDepth;
    public Vector2 allowance;

    public void Update()
    {
        if (active && damager != null)
            foreach (var collisionInstance in damager.collisionHandler.collisions)
                if (collisionInstance.damageStruct.damager == damager && collisionInstance.damageStruct.penetration != DamageStruct.Penetration.None)
                {
                    if (collisionInstance.damageStruct.lastDepth >= damager.penetrationDepth - allowance.x && !hasReachedMaxDepth)
                    {
                        hasReachedMaxDepth = true;
                        onPenetrateMaxDepth?.Invoke(damager, collisionInstance, damager.collisionHandler.item.Velocity, collisionInstance.damageStruct.lastDepth);
                    }
                    else if (hasReachedMaxDepth && collisionInstance.damageStruct.lastDepth + allowance.y < damager.penetrationDepth)
                    {
                        hasReachedMaxDepth = false;
                    }
                }
    }

    public event OnPenetrateMaxDepth onPenetrateMaxDepth;

    public void Activate(Damager damager, Vector2 allowance)
    {
        active = true;
        this.allowance = allowance;
        this.damager = damager;
    }

    public void Deactivate()
    {
        active = false;
    }
}