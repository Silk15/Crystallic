using Crystallic.AI;
using ThunderRoad;
using ThunderRoad.Skill;

namespace Crystallic.Skill;

public class SkillCrystalFracture : SpellSkillData
{
    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        BrainModuleCrystal.SetBreakForce(true);
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        BrainModuleCrystal.SetBreakForce(false);
    }
}