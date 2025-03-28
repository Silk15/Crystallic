using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalStinger : SpellSkillData
{
    [ModOption("Creature Throw Velocity Mult", "This is used for NPCs since they move their hands very slowly when throwing spells. The throw velocity is multiplied by this value before spawning."), ModOptionCategory("Crystal Stinger", 8), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.5f)]
    public static float creatureVelocityMultiplier = 4f;

    private EffectData projectileCollisionEffectData;
    public string projectileCollisionEffectId;
    private EffectData projectileEffectData;
    public string projectileEffectId;
    private EffectData projectileTrailEffectData;
    public string projectileTrailEffectId;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        projectileCollisionEffectData = Catalog.GetData<EffectData>(projectileCollisionEffectId);
        projectileEffectData = Catalog.GetData<EffectData>(projectileEffectId);
        projectileTrailEffectData = Catalog.GetData<EffectData>(projectileTrailEffectId);
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnSpellThrowEvent += OnSpellThrowEvent;
    }

    private void OnSpellThrowEvent(SpellCastCharge spell, Vector3 velocity)
    {
        if (!SpellCastCrystallic.shootStinger) return;
        var velocityMult = spell.spellCaster.mana.creature.isPlayer ? 1f : creatureVelocityMultiplier;
        Stinger.SpawnStinger(projectileEffectData, projectileTrailEffectData, projectileCollisionEffectData, spell.spellCaster.magicSource.transform.position + spell.spellCaster.magicSource.transform.forward * 0.15f, Quaternion.LookRotation(velocity), velocity * velocityMult, 10, spell as SpellCastCrystallic, spell.spellCaster.mana.creature);
    }


    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnSpellThrowEvent -= OnSpellThrowEvent;
    }
}