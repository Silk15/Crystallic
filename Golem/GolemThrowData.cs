using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Golem;

public class GolemThrowData<T> : GolemAbilityData<T> where T : GolemThrow
{
    public AnimationCurve objectEffectIntensityCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1f, 1f);
    public List<ThunderRoad.Golem.InflictedStatus> appliedStatuses = new();
    public LayerMask objectSpawnRaycastMask= 232799233;
    public LayerMask explosionLayerMask = 131073;
    public Vector3 holdPosition;
    public ForceMode forceMode;
    public Side grabArmSide;
    public float throwCooldownDuration;
    public float throwMaxDistance;
    public float gravityMultiplier;
    public float upwardForceMult;
    public float explosionRadius;
    public float explosionDamage;
    public float explosionForce;
    public float throwMaxAngle;
    public float throwVelocity;
    public float holdDamper;
    public float holdForce;
    public string summonEffectID;
    public string objectEffectID;
    public string throwObjectID;
    
    public override GolemAbility GetGolemAbility()
    {
        GolemAbility golemAbility = base.GetGolemAbility();
        if (golemAbility is GolemThrow golemThrow)
        {
            golemThrow.summonEffectID = summonEffectID;
            golemThrow.throwObjectID = throwObjectID;
            golemThrow.objectEffectID = objectEffectID;
            golemThrow.objectEffectIntensityCurve = objectEffectIntensityCurve;
            golemThrow.objectSpawnRaycastMask = objectSpawnRaycastMask;
            golemThrow.throwVelocity = throwVelocity;
            golemThrow.throwCooldownDuration = throwCooldownDuration;
            golemThrow.throwMaxDistance = throwMaxDistance;
            golemThrow.throwMaxAngle = throwMaxAngle;
            golemThrow.gravityMultiplier = gravityMultiplier;
            golemThrow.grabArmSide = grabArmSide;
            golemThrow.holdPosition = holdPosition;
            golemThrow.holdForce = holdForce;
            golemThrow.holdDamper = holdDamper;
            golemThrow.explosionRadius = explosionRadius;
            golemThrow.explosionDamage = explosionDamage;
            golemThrow.explosionForce = explosionForce;
            golemThrow.forceMode = forceMode;
            golemThrow.upwardForceMult = upwardForceMult;
            golemThrow.appliedStatuses = appliedStatuses;
            golemThrow.explosionLayerMask = explosionLayerMask;
        }
        return golemAbility;
    }
}