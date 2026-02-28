#if !SDK
using System.Collections.Generic;
using ThunderRoad;

namespace Crystallic.EnemyToggling;

public class ToggledEnemy
{
    public List<ToggledSkill> toggledSkills = new();
    public Creature creature;

    public ToggledEnemy(Creature creature)
    {
        this.creature = creature;
        foreach (SkillData skill in EnemyToggleManager.allSkills)
            if (creature.HasSkill(skill.id))
            {
                ToggledSkill toggledSkill = null;
                switch (skill)
                {
                    case SpellData spellData:
                        toggledSkill = new ToggledSpell(creature, spellData.id);
                        break;
                    default:
                        toggledSkill = new ToggledSkill(creature, skill.id);
                        break;
                }
                toggledSkills.Add(toggledSkill);
            }
    }
    
    public bool Has(string skillId) => creature.HasSkill(skillId);
    
    public void Load(string[] skillIds)
    {
        for (int i = 0; i < skillIds.Length; i++)
            Load(skillIds[i]);
    }

    public void Load(string skillId)
    {
        for (int i = 0; i < toggledSkills.Count; i++)
            if (toggledSkills[i].id == skillId) 
                toggledSkills[i].Load();
    }

    public void Unload(string[] skillIds)
    {
        for (int i = 0; i < skillIds.Length; i++)
            Unload(skillIds[i]);
    }

    public void Unload(string skillId)
    {
        for (int i = 0; i < toggledSkills.Count; i++)
            if (toggledSkills[i].id == skillId) 
                toggledSkills[i].Unload();
    }
}
#endif