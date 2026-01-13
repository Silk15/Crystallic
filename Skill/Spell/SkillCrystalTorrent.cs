using System;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill.Spell;

public class SkillCrystalTorrent : SpellSkillData, IGolemSprayable
{
    public Dictionary<Creature, float> lastPushTimes = new();
    public Vector2 crystallisationChance = new(0, 6);
    public EffectData hitShardEffectData;
    public string hitShardEffectId = "HitShard";
    public EffectData torrentEffectData;
    public string torrentEffectId;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        hitShardEffectData = Catalog.GetData<EffectData>(hitShardEffectId);
        torrentEffectData = Catalog.GetData<EffectData>(torrentEffectId);
        lastPushTimes.Clear();
    }

    public override void OnImbueLoad(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        if (spell is not SpellCastCrystallic spellCastCrystallic || imbue.colliderGroup.modifier.imbueType != ColliderGroupData.ImbueType.Crystal) 
            return;
        
        spellCastCrystallic.OnCrystalUseEvent -= OnCrystalUse;
        spellCastCrystallic.OnSpellStopEvent -= OnSpellStop;
        imbue.OnItemDrop -= OnDrop;

        spellCastCrystallic.OnCrystalUseEvent += OnCrystalUse;
        spellCastCrystallic.OnSpellStopEvent += OnSpellStop;
        imbue.OnItemDrop += OnDrop;
    }

    public override void OnImbueUnload(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        if (spell is not SpellCastCrystallic spellCastCrystallic || imbue.colliderGroup.modifier.imbueType != ColliderGroupData.ImbueType.Crystal)
            return;
        
        imbue.colliderGroup.imbueShoot.GetComponent<CrystalTorrent>()?.Fire(this, imbue.imbueCreature, imbue, false);
        
        spellCastCrystallic.OnCrystalUseEvent -= OnCrystalUse;
        spellCastCrystallic.OnSpellStopEvent -= OnSpellStop;
        imbue.OnItemDrop -= OnDrop;
    }

    public void OnCrystalUse(SpellCastCharge spell, ThunderRoad.Imbue imbue, RagdollHand hand, bool active)
    {
        if (spell is not SpellCastCrystallic crystallic) 
            return;
        
        hand.PlayHapticClipOver(crystallic.pulseCurve, 0.15f);
        imbue.colliderGroup.imbueShoot.GetOrAddComponent<CrystalTorrent>().Fire(this, imbue.imbueCreature, imbue, active);
    }

    public void OnSpellStop(SpellCastCharge spell)
    {
        if (spell is not SpellCastCrystallic)
            return;
        
        spell.imbue?.colliderGroup?.imbueShoot?.GetComponent<CrystalTorrent>()?.Fire(this, spell.imbue.imbueCreature, spell.imbue, false);
    }

    public void OnDrop(ThunderRoad.Imbue imbue)
    {
        if (imbue.spellCastBase is not SpellCastCrystallic)
            return;
        
        imbue.colliderGroup?.imbueShoot?.GetComponent<CrystalTorrent>()?.Fire(this, imbue.imbueCreature, imbue, false);
    }

    public void GolemSprayStart(GolemSpray ability, out Action end)
    {
        CrystalTorrent[] activeTorrents = new CrystalTorrent[ability.sprayPoints.Count];
        for (int index = 0; index < ability.sprayPoints.Count; ++index)
        {
            Transform sprayPoint = ability.sprayPoints[index];
            activeTorrents[index] = sprayPoint.GetOrAddComponent<CrystalTorrent>();
            activeTorrents[index].buildupTime = 0.5f;
            activeTorrents[index].Fire(ability.spraySkillData as SkillCrystalTorrent, ThunderRoad.Golem.local, null, true);
            activeTorrents[index].overrideTarget = ability.sprayPoints[index];
        }
        end = () =>
        {
            foreach (CrystalTorrent torrent in activeTorrents)
                torrent.Fire(ability.spraySkillData as SkillCrystalTorrent, ThunderRoad.Golem.local, null, false);
        };
    }
}