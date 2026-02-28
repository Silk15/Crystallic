using Crystallic.Golem.Ability;
using ThunderRoad;
using TriInspector;
using UnityEngine;

namespace Crystallic.Golem;

public abstract class GolemAbilityData : CustomData
{
    public abstract GolemAbility GetGolemAbility();
        
    #if UNITY_EDITOR
        public override string GetCatalogPath()
        {
            string result = $"GolemAbilities";
            if (!groupPath.IsNullOrEmptyOrWhitespace()) result += $"/{groupPath}";
            if (result[result.Length - 1] == '/') result = result.Substring(0, result.Length - 1);
            return result;
        }
    #endif
        
    public TriDropdownList<string> GetAllEffectID() => Catalog.GetDropdownAllID(Category.Effect);
        
    public TriDropdownList<string> GetAllSkillID() => Catalog.GetDropdownAllID(Category.Skill);
        
    public TriDropdownList<string> GetAllItemID() => Catalog.GetDropdownAllID(Category.Item);
}

public class GolemAbilityData<T> : GolemAbilityData where T : GolemAbility
{
    public GolemAbilityType type;
    public bool stunOnExit = false;
    public float stunDuration = 1.0f;
    public float weight = 1.0f;

    public override GolemAbility GetGolemAbility() => null;
}