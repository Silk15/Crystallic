using System.Collections.Generic;
using ThunderRoad;

namespace Crystallic.Golem;

public class GolemSprayData<T> : GolemAbilityData<T> where T : GolemSpray
{
    public GolemController.AttackMotion sprayMotion = GolemController.AttackMotion.Spray;
    public float sprayAngle = 90.0f;
    public List<string> spraySources;
    public string spraySkillID;
    
    public override GolemAbility GetGolemAbility()
    {
        GolemAbility golemAbility = base.GetGolemAbility();
        if (golemAbility is GolemSpray golemSpray)
        {
            golemSpray.sprayMotion = sprayMotion;
            golemSpray.sprayAngle = sprayAngle;
            golemSpray.spraySources = spraySources;
            golemSpray.spraySkillID = spraySkillID;
        }
        return golemAbility;
    }
}