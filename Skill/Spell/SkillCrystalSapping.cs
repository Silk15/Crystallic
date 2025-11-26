using ThunderRoad.Skill.Spell;

namespace Crystallic.Skill.Spell;

public class SkillCrystalSapping : SkillSapStatusApplier
{
    public override void ApplyStatus(SpellCastLightning spell) => spell.AddStatus(this, statusData, statusDuration, param: new CrystallisedParams(Dye.GetEvaluatedColor("Crystallic", "Lightning"), "Lightning"));
}