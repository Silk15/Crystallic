using System.Collections.Generic;
using Crystallic.AI;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillHyperdetonation : SpellSkillData
{
    [ModOption("Depth Allowance X", "This is used by the max depth damager detector to control how far in you have to stab before the event is invoked. This value is taken away from the damager's max length, the higher this is the less you have to stab."), ModOptionCategory("Hyperdetonation", 9), ModOptionSlider, ModOptionFloatValues(0.01f, 100, 0.005f)]
    public static float allowanceX = 0.01f;

    [ModOption("Depth Allowance Y", "This is used by the max depth damager detector to control how far out you have to remove the weapon before you can trigger the event again, the higher this value the more you will have to remove."), ModOptionCategory("Hyperdetonation", 9), ModOptionSlider, ModOptionFloatValues(0.01f, 100, 0.005f)]
    public static float allowanceY = 0.085f;

    [ModOption("Min Stab Velocity", "The minimum velocity your hand has to be for the skill to trigger."), ModOptionCategory("Hyperdetonation", 9), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.05f)]
    public static float minVelocity = 0.25f;

    public List<SkillSpellPair> skillSpellPairs;

    public override void OnImbueLoad(SpellData spell, Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        foreach (var pair in skillSpellPairs)
            if (imbue.spellCastBase.id == pair.spellId && imbue.imbueCreature.HasSkill(pair.skillId))
                foreach (var damager in imbue.colliderGroup.collisionHandler.item.GetComponentsInChildren<Damager>())
                {
                    if (damager.penetrationDepth == 0 || damager.penetrationLength > 0) continue;
                    var detector = damager.GetOrAddComponent<MaxDepthDetector>();
                    detector.Activate(damager, new Vector2(allowanceX, allowanceY));
                    detector.onPenetrateMaxDepth += OnPenetrateMaxDepth;
                }
    }

    private void OnPenetrateMaxDepth(Damager damager, CollisionInstance collisionInstance, Vector3 velocity, float depth)
    {
        if (velocity.magnitude > minVelocity && collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && !creature.isPlayer)
        {
            var bainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            if (bainModuleCrystal.isCrystallised)
            {
                var color = bainModuleCrystal.lerper.currentColorType == ColorType.Solid ? Dye.GetEvaluatedColor(bainModuleCrystal.lerper.currentSpellId, bainModuleCrystal.lerper.currentSpellId) : Dye.GetEvaluatedColor(bainModuleCrystal.lerper.currentSpellId, "Crystallic");
                SkillOverchargedCore.Detonate(creature, color);
            }
        }
    }

    public override void OnImbueUnload(SpellData spell, Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        foreach (var pair in skillSpellPairs)
            if (imbue.spellCastBase.id == pair.spellId && imbue.imbueCreature.HasSkill(pair.skillId))
                foreach (var damager in imbue.colliderGroup.collisionHandler.item.GetComponentsInChildren<Damager>())
                {
                    if (damager.penetrationDepth == 0) continue;
                    var detector = damager?.GetComponent<MaxDepthDetector>();
                    if (detector != null)
                    {
                        detector.Deactivate();
                        detector.onPenetrateMaxDepth -= OnPenetrateMaxDepth;
                    }
                }
    }
}