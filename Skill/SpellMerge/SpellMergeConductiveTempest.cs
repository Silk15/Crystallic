using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill.SpellMerge;

public class SpellMergeConductiveTempest : SpellMergeData
{
    public string conductiveTempestEffectId;
    public EffectData conductiveTempestEffectData;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        conductiveTempestEffectData = Catalog.GetData<EffectData>(conductiveTempestEffectId);
    }

    public override void Merge(bool active)
    {
        base.Merge(active);
        if (!active)
        {
            conductiveTempestEffectData.Spawn(Player.currentCreature.transform.position + Vector3.up * 3f, Quaternion.identity).Play();
        }
    }
}