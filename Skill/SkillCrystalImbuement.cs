using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalImbuement : SpellSkillData
{
    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.imbueEnabled = true;
        spellCastCrystallic.spellCaster.imbueTrigger.SetRadius(0.2f);
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.imbueEnabled = false;
        spellCastCrystallic.spellCaster.imbueTrigger.SetRadius(0.2f);
    }
}