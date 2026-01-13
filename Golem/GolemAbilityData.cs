using Crystallic.Golem.Ability;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Golem;

public abstract class GolemAbilityData : CustomData
{
    public abstract GolemAbility GetGolemAbility();
}

public class GolemAbilityData<T> : GolemAbilityData where T : GolemAbility
{
    public GolemAbilityType type;
    public bool stunOnExit = false;
    public float stunDuration = 1.0f;
    public float weight = 1.0f;
    
    public override GolemAbility GetGolemAbility() 
    {
        T ability = ScriptableObject.CreateInstance<T>();
        
        ability.stunDuration = stunDuration;
        ability.stunOnExit = stunOnExit;
        ability.weight = weight;
        ability.type = type;
 
        return ability;
    }
}
