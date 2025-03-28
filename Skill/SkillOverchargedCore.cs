using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillOverchargedCore : SpellSkillData
{
    [ModOption("Detonation Radius", "Decides how far force is added to creatures in a spherical radius, this is used for all detonation skills as a generic detonate method."), ModOptionCategory("Overcharged Core", 11), ModOptionSlider, ModOptionFloatValues(1f, 100, 0.1f)]
    public static float detonationRadius = 5f;

    [ModOption("Detonation Force", "Decides how strong the applied force is for all detonate skills."), ModOptionCategory("Overcharged Core", 11), ModOptionSlider, ModOptionFloatValues(1f, 100, 0.1f)]
    public static float detonationForce = 30f;

    [ModOption("Detonation Force Upwards Modifier", "Decides the force multiplier when a rigidbody is pushed up."), ModOptionCategory("Overcharged Core", 11), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float detonationUpdardsModifier = 0.3f;

    public static EffectData detonateEffectData;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        detonateEffectData = Catalog.GetData<EffectData>("DetonateCrystallicLarge");
    }

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        SkillHyperintensity.onSpellOvercharge += OnSpellOvercharge;
        SkillHyperintensity.onSpellReleased += OnSpellReleased;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        SkillHyperintensity.onSpellOvercharge -= OnSpellOvercharge;
        SkillHyperintensity.onSpellReleased -= OnSpellReleased;
    }

    public void OnSpellOvercharge(SpellCastCrystallic spellCastCrystallic)
    {
        Stinger.onStingerSpawn += OnStingerSpawn;
    }

    private void OnSpellReleased(SpellCastCrystallic spellCastCrystallic)
    {
        Stinger.onStingerSpawn -= OnStingerSpawn;
    }

    private void OnStingerSpawn(Stinger stinger)
    {
        stinger.onStingerStab += OnStingerStab;
    }

    private void OnStingerStab(Stinger stinger, Damager damager, CollisionInstance collisionInstance, Creature hitCreature)
    {
        stinger.onStingerStab -= OnStingerStab;
        if (hitCreature && !hitCreature.isPlayer)
        {
            var brainModule = hitCreature.brain.instance.GetModule<BrainModuleCrystal>();
            Detonate(hitCreature, Dye.GetEvaluatedColor(brainModule.lerper.currentSpellId, brainModule.lerper.currentSpellId), stinger);
        }
    }

    public static void Detonate(Creature creature, Color color, Stinger stinger = null)
    {
        if (creature.isPlayer) return;
        if (detonateEffectData == null) detonateEffectData = Catalog.GetData<EffectData>("DetonateCrystallicLarge");
        var effectInstance = detonateEffectData?.Spawn(creature.ragdoll.targetPart.transform.position, Quaternion.identity, creature.ragdoll.targetPart.transform);
        effectInstance?.Play();
        effectInstance.SetColorImmediate(color);
        creature.Shred();
        creature.AddExplosionForce(detonationForce, creature.ragdoll.targetPart.transform.position, detonationRadius, detonationUpdardsModifier, ForceMode.Impulse);
    }
}