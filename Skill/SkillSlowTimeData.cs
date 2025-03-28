using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.SpellPower;

namespace Crystallic.Skill;

public class SkillSlowTimeData : SpellSkillData
{
    public static bool timeSlowed;

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        SpellPowerSlowTime.OnTimeScaleChangeEvent += OnTimeScaleChangeEvent;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        SpellPowerSlowTime.OnTimeScaleChangeEvent -= OnTimeScaleChangeEvent;
    }

    private void OnTimeScaleChangeEvent(SpellPowerSlowTime spell, float scale)
    {
        if (TimeManager.slowMotionState == TimeManager.SlowMotionState.Starting) OnSlowMotionEnter(spell, scale);
        else if (TimeManager.slowMotionState == TimeManager.SlowMotionState.Stopping) OnSlowMotionExit(spell);
    }

    public virtual void OnSlowMotionEnter(SpellPowerSlowTime spellPowerSlowTime, float scale)
    {
        timeSlowed = true;
    }

    public virtual void OnSlowMotionExit(SpellPowerSlowTime spellPowerSlowTime)
    {
        timeSlowed = false;
    }
}