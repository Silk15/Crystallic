using System;
using System.Collections.Generic;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillSeekingShards : SpellSkillData
{
    [ModOption("Seeking Radius", "Controls how large the attraction radius is for each creature."), ModOptionCategory("Seeking Shards", 16), ModOptionSlider, ModOptionFloatValues(0.5f, 100, 0.5f)]
    public static float seekingRadius = 5f;
    
    [ModOption("Seeking Max Distance", "Controls how far the ray shoots, this is used to detect creatures to seek."), ModOptionCategory("Seeking Shards", 16), ModOptionSlider, ModOptionFloatValues(0.5f, 100, 0.5f)]
    public static float seekingMaxDistance = 7f;
    
    [ModOption("Seeking Max Angle", "Controls how far the ray shoots, this is used to detect creatures to seek."), ModOptionCategory("Seeking Shards", 16), ModOptionSlider, ModOptionFloatValues(0.5f, 100, 0.5f)]
    public static float seekingMaxAngle= 25f;
    
    public List<ParticleSystemForceField> activeFields = new();
    
    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnShardshotStart += OnShardshotStart;
        spellCastCrystallic.OnShardshotEnd += OnShardshotEnd;
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnShardshotStart -= OnShardshotStart;
        spellCastCrystallic.OnShardshotEnd -= OnShardshotEnd;
    }

    private void OnShardshotEnd(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance)
    {
        foreach (ParticleSystemForceField forceField in activeFields) GameObject.Destroy(forceField);
    }

    private void OnShardshotStart(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance)
    {
        if (Creature.AimAssist(spellCastCrystallic.spellCaster.Orb.position, spellCastCrystallic.lastVelocity.normalized, seekingMaxDistance, seekingMaxAngle, out Transform point, Filter.LiveCreaturesExcept(Player.currentCreature)) && point.GetComponentInParent<Creature>() is Creature creature)
        {
            if (creature != null)
            {
                var field = creature.ragdoll.targetPart.gameObject.GetOrAddComponent<ParticleSystemForceField>();
                activeFields.Add(field);
                field.gravity = 0.45f;
                field.endRange = seekingRadius;
            }
        }
    }
}