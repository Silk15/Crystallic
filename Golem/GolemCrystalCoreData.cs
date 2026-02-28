using Crystallic.Golem.Ability.Throw;
using ThunderRoad;
using TriInspector;
using UnityEngine;

namespace Crystallic.Golem;

public class GolemCrystalCoreData<T> : GolemThrowData<T> where T : GolemCrystalCore
{
    [Dropdown(nameof(GetAllEffectID))]
    public string coreFireEffectId;
        
    [Dropdown(nameof(GetAllEffectID))]
    public string fireHitEffectId;
        
    [Dropdown(nameof(GetAllEffectID))]
    
    public string coreEffectId;
    public float projectileVelocity = 0.4f;
    public float coreFireLifetime = 6f;
    public float coreFireDelay = 2f;
    public float dragDelay;
    public float drag;

    public override GolemAbility GetGolemAbility()
    {
        #if !SDK
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
        #else
        return null;
        #endif
    }
}