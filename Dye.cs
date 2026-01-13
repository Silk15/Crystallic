using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crystallic;

public class Dye : ThunderScript
{
    
    
    public static bool rainbowModeWasActivatedThisSession = false;
    public static bool rainbowMode = false;

    [ModOption("Rainbow Mode", "Modifies a few effects turning them into rainbows. If you have epilepsy I recommend you avoid this."), ModOptionCategory("Crystallisation", -1), ModOptionOrder(2)]
    public static void SetRainbowMode(bool active)
    {
        rainbowMode = active;
        if (active) rainbowModeWasActivatedThisSession = true;
    }
    
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
        var result = TryGetColor(originSpellId, hitSpellId);
        if (result.found)
            return result.color;

        result = TryGetColor(hitSpellId, originSpellId);
        if (result.found)
            return result.color;

        return new Color(1, 1, 1, 1);
    }

    private static (bool found, Color color) TryGetColor(string a, string b)
    {
        foreach (var data in dyeData)
        {
            if (data.spellId != a) continue;

            if (data.spellId == b)
                return (true, data.color);
            
            foreach (var mixture in data.dyeMixtures)
                if (mixture.mixSpellId == b)
                    return (true, mixture.mixColor);
        }

        return (false, default);
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
}

