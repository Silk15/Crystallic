using System.Collections.Generic;
using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill.SpellPower;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalDilation : SkillSlowTimeData
{
    public override void OnSlowMotionEnter(SpellPowerSlowTime spellPowerSlowTime, float scale)
    {
        base.OnSlowMotionEnter(spellPowerSlowTime, scale);
        foreach (var creature in Creature.allActive)
        {
            var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            if (brainModuleCrystal.isCrystallised)
            {
                brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, "Mind"), "Mind");
                creature.Inflict("Slowed", this, 2.5f);
            }
        }
    }
}