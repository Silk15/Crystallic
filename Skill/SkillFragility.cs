using ThunderRoad;

namespace Crystallic.Skill;

public class SkillFragility : SkillData
{
    #if !SDK
    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        EventManager.onCreatureSpawn -= OnCreatureSpawn;
        EventManager.onCreatureSpawn += OnCreatureSpawn;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        EventManager.onCreatureSpawn -= OnCreatureSpawn;
    }

    private void OnCreatureSpawn(Creature creature) => creature.brain.instance.GetModule<BrainModuleCrystal>().allowBreakForce = true;
    #endif
}