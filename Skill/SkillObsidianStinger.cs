using ThunderRoad;
using ThunderRoad.Skill;

namespace Crystallic.Skill;

public class SkillObsidianStinger : SpellSkillData
{
    [ModOption("Dismemberment Allowance", "This value is used to decide how close the stinger has to be to a limb to dismember it."), ModOptionCategory("Obsidian Stinger", 10), ModOptionSlider, ModOptionFloatValues(0.05f, 100, 0.01f)]
    public float dismembermentAllowance = 0.4f;

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        Stinger.onStingerSpawn -= OnStingerSpawn;
        Stinger.onStingerSpawn += OnStingerSpawn;
    }

    private void OnStingerSpawn(Stinger stinger)
    {
        stinger.onStingerStab += OnStingerStab;
    }

    private void OnStingerStab(Stinger stinger, Damager damager, CollisionInstance collisionInstance, Creature hitCreature)
    {
        stinger.onStingerStab -= OnStingerStab;
        if (hitCreature != null)
            if (hitCreature.ragdoll.GetClosestPart(collisionInstance.contactPoint, dismembermentAllowance, out var partToSlice) && partToSlice.sliceAllowed && !partToSlice.hasMetalArmor)
            { 
                partToSlice.RunAfter(() =>
                {
                    partToSlice.TrySlice();
                    hitCreature.Kill();
                }, 0.05f);
            }
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        Stinger.onStingerSpawn -= OnStingerSpawn;
    }
}