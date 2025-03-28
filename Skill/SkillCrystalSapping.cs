using System.Collections.Generic;
using Crystallic.AI;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalSapping : SkillSapStatusApplier
{
    [ModOption("Crystallise With Other Sapping"), ModOptionCategory("Crystal Sapping", 13)]
    public static bool applyCrystalSapping = true;

    public string mixSpellId;

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (spell is SpellCastLightning spellCastLightning)
        {
            spellCastLightning.OnChargeSappingEvent += OnChargeSappingEvent;
            spellCastLightning.OnBoltHitColliderGroupEvent += OnBoltHitColliderGroupEvent;
        }
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (spell is SpellCastLightning spellCastLightning)
        {
            spellCastLightning.OnChargeSappingEvent -= OnChargeSappingEvent;
            spellCastLightning.OnBoltHitColliderGroupEvent -= OnBoltHitColliderGroupEvent;
        }
    }

    private void OnBoltHitColliderGroupEvent(SpellCastLightning spell, ColliderGroup colliderGroup, Vector3 position, Vector3 normal, Vector3 velocity, float intensity, ColliderGroup source, HashSet<ThunderEntity> seenEntities)
    {
        if (applyCrystalSapping && colliderGroup.collisionHandler.Entity is Creature creature && creature != spell.spellCaster.mana.creature && creature.factionId != spell.spellCaster.mana.creature.factionId && mixSpellId != "Lightning" && mixSpellId !=null)
        {
            var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            brainModuleCrystal.Crystallise(5);
            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor("Lightning", mixSpellId), mixSpellId);
        }
    }

    private void OnChargeSappingEvent(SpellCastLightning spell, SkillChargeSapping skill, EventTime time, SpellCastCharge other)
    {
        if (time == EventTime.OnEnd) mixSpellId = other.id;
    }
}