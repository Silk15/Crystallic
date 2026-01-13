using Crystallic.Golem.Ability.Throw;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Golem;

public class GolemCrystalCoreData<T> : GolemThrowData<T> where T : GolemCrystalCore
{
    public string coreFireEffectId;
    public string fireHitEffectId;
    public string coreEffectId;
    public float projectileVelocity = 0.4f;
    public float coreFireLifetime = 6f;
    public float coreFireDelay = 2f;
    public float dragDelay;
    public float drag;

    public override GolemAbility GetGolemAbility()
    {
        GolemAbility golemAbility = base.GetGolemAbility();
        if (golemAbility is GolemCrystalCore golemCrystalCore)
        {
            golemCrystalCore.coreFireEffectId = coreFireEffectId;
            golemCrystalCore.fireHitEffectId = fireHitEffectId;
            golemCrystalCore.coreEffectId = coreEffectId;
            golemCrystalCore.projectileVelocity = projectileVelocity;
            golemCrystalCore.coreFireLifetime = coreFireLifetime;
            golemCrystalCore.coreFireDelay = coreFireDelay;
            golemCrystalCore.dragDelay = dragDelay;
            golemCrystalCore.drag = drag;
        }
        return golemAbility;
    }
}