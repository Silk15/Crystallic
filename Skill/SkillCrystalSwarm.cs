using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalSwarm : SpellSkillData
{
    public EffectData projectileCollisionEffectData;
    public string projectileCollisionEffectId;
    public EffectData projectileEffectData;
    public string projectileEffectId;
    public EffectData projectileTrailEffectData;
    public string projectileTrailEffectId;
    public EffectData pulseEffectData;
    public string pulseEffectId;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        projectileCollisionEffectData = Catalog.GetData<EffectData>(projectileCollisionEffectId);
        projectileEffectData = Catalog.GetData<EffectData>(projectileEffectId);
        projectileTrailEffectData = Catalog.GetData<EffectData>(projectileTrailEffectId);
        pulseEffectData = Catalog.GetData<EffectData>(pulseEffectId);
    }

    public override void OnImbueLoad(SpellData spell, Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        if (!(spell is SpellCastCrystallic spellCastCrystallic) || imbue.colliderGroup.modifier.imbueType != ColliderGroupData.ImbueType.Crystal) return;
        spellCastCrystallic.OnCrystalUseEvent -= OnCrystalUse;
        spellCastCrystallic.OnCrystalUseEvent += OnCrystalUse;
    }

    public override void OnImbueUnload(SpellData spell, Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        if (!(spell is SpellCastCrystallic spellCastCrystallic) || imbue.colliderGroup.modifier.imbueType != ColliderGroupData.ImbueType.Crystal) return;
        spellCastCrystallic.OnCrystalUseEvent -= OnCrystalUse;
    }

    public void OnCrystalUse(SpellCastCharge spell, Imbue imbue, RagdollHand hand, bool active)
    {
        if (!(spell is SpellCastCrystallic) || !active) return;
        var overcharge = Player.currentCreature.HasSkill("OverchargedCore");
        var spellCastCrystallic = (SpellCastCrystallic)spell;
        if (overcharge) SkillHyperintensity.ForceInvokeOvercharged(spellCastCrystallic);
        hand.PlayHapticClipOver(spellCastCrystallic.pulseCurve, 0.25f);
        pulseEffectData.Spawn(imbue.colliderGroup.imbueShoot.transform.position + imbue.colliderGroup.imbueShoot.transform.forward * 0.15f, Quaternion.LookRotation(imbue.colliderGroup.imbueShoot.transform.forward)).Play();
        Stinger.SpawnStinger(projectileEffectData, projectileTrailEffectData, projectileCollisionEffectData, imbue.colliderGroup.imbueShoot.transform.position + imbue.colliderGroup.imbueShoot.transform.forward * 0.15f, Quaternion.LookRotation(imbue.colliderGroup.imbueShoot.transform.forward), imbue.colliderGroup.imbueShoot.transform.forward * 4.25f, 10, spell as SpellCastCrystallic, forceReleaseOnSpawn: overcharge);
    }
}