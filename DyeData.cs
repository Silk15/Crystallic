using System;
using System.Collections.Generic;
using ThunderRoad;
using TriInspector;
using UnityEngine;

namespace Crystallic
{
    [Serializable]
    public class DyeData : CustomData
    {
        [Dropdown(nameof(GetAllSpellID))]
        public string spellId;
        
        public Color color;
        public List<DyeMixture> dyeMixtures;

        #if UNITY_EDITOR
        public override string GetCatalogPath()
        {
            string result = $"Dyes";
            if (!groupPath.IsNullOrEmptyOrWhitespace()) result += $"/{groupPath}";
            if (result[result.Length - 1] == '/') result = result.Substring(0, result.Length - 1);
            return result;
        }
        #endif

        public TriDropdownList<string> GetAllSpellID()
        {
            TriDropdownList<string> result = new TriDropdownList<string>();
            foreach (SpellData spellData in Catalog.GetDataList<SpellData>()) result.Add(spellData.id, spellData.id);
            return result;
        }
    }
}