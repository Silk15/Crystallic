using ThunderRoad.Skill;
using System;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalImbueHandler : SpellSkillData
{
    public EffectData imbueCollisionEffectData;
    public EffectData imbueEffectData;
    public Color colorModifier;
    public string spellId;
    public string imbueHitEffectId;
    public string imbueEffectId;
    public string typeAddress;
    public Type type;
    public bool crystalliseOnHit = true;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        imbueCollisionEffectData = Catalog.GetData<EffectData>(imbueHitEffectId);
        imbueEffectData = Catalog.GetData<EffectData>(imbueEffectId);
        type = Type.GetType(typeAddress);
    }

    public override void OnImbueLoad(SpellData spell, Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        if (spell.id != spellId) return;
        var behaviour = (ImbueBehaviour)imbue.gameObject.AddComponent(type);
        behaviour.Load(this, imbue);
    }

    public override void OnImbueUnload(SpellData spell, Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        var components = imbue?.gameObject?.GetComponents<ImbueBehaviour>();
        if (!components.IsNullOrEmpty()) foreach (var imbueBehavior in components) imbueBehavior?.Unload(imbue);
    }
}