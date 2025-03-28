using System;
using System.Reflection.Emit;
using ThunderRoad;

namespace Crystallic.Skill;

[Serializable]
public class SkillSpellPair
{
    public string spellId;
    public string skillId;

    public virtual bool HasSkill(Creature creature, bool skipNullConditions = true)
    {
        bool condition = creature.HasSkill(skillId);
        return skipNullConditions ? condition || string.IsNullOrEmpty(skillId) : condition;
    }

    public virtual bool IsSpell(SpellData spellData, bool skipNullConditions = true)
    {
        bool condition = spellData.id == spellId;
        return skipNullConditions ? condition || string.IsNullOrEmpty(spellId) : condition;
    }

    public virtual bool IsValid(Creature creature, SpellData spellData, bool skipNullConditions = true)
    {
        bool spellCondition = IsSpell(spellData, skipNullConditions);
        bool creatureCondition = HasSkill(creature, skipNullConditions);
        return spellCondition && creatureCondition;
    }
}
