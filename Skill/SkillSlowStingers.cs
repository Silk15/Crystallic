using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using ThunderRoad.Skill.SpellMerge;
using ThunderRoad.Skill.SpellPower;

namespace Crystallic.Skill;

public class SkillSlowStingers : SkillSlowTimeData
{
    public List<Stinger> slowed = new();
    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        Stinger.onStingerSpawn -= OnStingerSpawn;
        Stinger.onStingerSpawn += OnStingerSpawn;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        Stinger.onStingerSpawn -= OnStingerSpawn;
    }

    public override void OnSlowMotionEnter(SpellPowerSlowTime spellPowerSlowTime, float scale)
    {
        base.OnSlowMotionEnter(spellPowerSlowTime, scale);
        foreach (Stinger stinger in Stinger.all)
        {
            stinger.item.GetOrAddComponent<VelocityStorer>()?.Activate(2);
            stinger.item.Inflict("Slowed", this);
        }
    }

    private void OnStingerSpawn(Stinger stinger)
    {
        if (timeSlowed)
        {
            stinger.item.Inflict("Slowed", this);
            stinger.item.GetOrAddComponent<VelocityStorer>()?.Activate(2);
        }
    }

    public override void OnSlowMotionExit(SpellPowerSlowTime spellPowerSlowTime)
    {
        base.OnSlowMotionExit(spellPowerSlowTime);
        foreach (Stinger stinger in slowed)
        {
            stinger.item.Remove("Slowed", this);
            stinger.item.GetComponent<VelocityStorer>()?.Deactivate();
        }
    }
}