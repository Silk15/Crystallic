using System;
using ThunderRoad;
using TriInspector;

namespace Crystallic.Skill.SpellMerge
{
    public class SpellMergeConductiveTempest : SpellMergeData
    {
        [NonSerialized]
        public EffectData conductiveTempestEffectData;

        [Dropdown(nameof(GetAllEffectID))]
        public string conductiveTempestEffectId;

        #if !SDK
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            conductiveTempestEffectData = Catalog.GetData<EffectData>(conductiveTempestEffectId);
        }
        #endif
    }
}