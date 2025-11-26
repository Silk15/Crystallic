using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crystallic;

public class Dye : ThunderScript
{
    public static List<DyeData> dyeData = new();

    public override void ScriptEnable()
    {
        base.ScriptEnable();
        EventManager.onLevelLoad += OnLevelLoad;
    }

    public override void ScriptDisable()
    {
        base.ScriptDisable();
        EventManager.onLevelLoad -= OnLevelLoad;
    }

    private void OnLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime) => Utils.Validate(() => eventTime == EventTime.OnEnd, () => Load());

    public static void Load()
    {
        dyeData.Clear();
        dyeData = Catalog.GetDataList<DyeData>();
        Debug.Log("[Crystallic] Loaded Dye Data:\n - " + string.Join("\n - ", dyeData.Select(d => d.id)));
    }
    
    public static Color GetEvaluatedColor(string originSpellId, string hitSpellId)
    {
        var color = new Color(1, 1, 1, 1);
        foreach (var data in dyeData)
        {
            if (data.spellId != originSpellId) continue;
            if (data.spellId == hitSpellId) return data.color;
            foreach (var mixture in data.dyeMixtures)
                if (mixture.mixSpellId == hitSpellId)
                    color = mixture.mixColor;
        }

        return color;
    }


    public static ColorType GetColorType(string originSpellId, string hitSpellId)
    {
        foreach (var data in dyeData)
            if (data.spellId == originSpellId)
            {
                if (data.spellId == hitSpellId) return ColorType.Solid;
                foreach (var mixture in data.dyeMixtures)
                    if (mixture.mixSpellId == hitSpellId)
                        return ColorType.Mix;
            }

        return ColorType.Solid;
    }

    private static DyeData FindDyeDataById(string id) => dyeData.FirstOrDefault(d => d.id == id);
}

