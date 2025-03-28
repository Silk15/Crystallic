using System;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalImbueHandler : SpellSkillData
{
    public string classAddress;
    public Color colorModifier;
    public bool crystallise;
    public float crystalliseDuration;
    public string imbueEffectId;
    public string imbueHitEffectId;
    public Vector2 minMaxImpactVelocity;
    public string spellId;
    public Type type;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        type = Type.GetType(classAddress);
    }

    public override void OnImbueLoad(SpellData spell, Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        if (spell?.id != spellId || type == null) return;
        var behavior = (ImbueBehavior)imbue.gameObject.AddComponent(type);
        behavior?.Activate(imbue, this);
    }

    public override void OnImbueUnload(SpellData spell, Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        if (spell?.id != spellId || type == null) return;
        var components = imbue?.gameObject?.GetComponents<ImbueBehavior>();
        if (components == null || components.Length == 0) return;
        foreach (var imbueBehavior in components) imbueBehavior?.Deactivate();
    }
}