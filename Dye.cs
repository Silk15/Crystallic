using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class Dye : ThunderScript
{
    public static List<DyeData> dyeData = new();

    public override void ScriptEnable()
    {
        base.ScriptEnable();
        EventManager.onLevelLoad += OnLevelLoad;
    }

    private void OnLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd) Load();
    }

    public static void Load()
    {
        dyeData.Clear();
        List<string> dyeDataIds = new();
        dyeData = Catalog.GetDataList<DyeData>();
        for (int i = 0; i < dyeData.Count; i++)
        {
            DyeData data = dyeData[i];
            if (Settings.debug)
            {
                List<string> mixes = new();
                foreach (DyeMixture mixture in data.dyeMixtures) mixes.Add($"{string.Join(", ", mixture.mixSpellId)} + {string.Join(", ", mixture.mixColor)}");
                dyeDataIds.Add($"{data.id}  \n    - SpellId: {data.spellId}  \n    - Color: {data.color}  \n    - Mixes:\n       - {string.Join("   \n       - ", mixes)}");
            }
            else dyeDataIds.Add(data.id);
        }
        string toUse = !Settings.debug ? "" : $"Stack Trace: \n{System.Environment.StackTrace}\n";
        Debug.Log(toUse + "Loaded Dye Data:\n - " + string.Join("\n - ", dyeDataIds));
    }

    public static bool TryGetDye(string id, out DyeData dyeData)
    {
        dyeData = FindDyeDataById(id);
        return dyeData != null;
    }

    public static DyeData GetDye(string id)
    {
        var dyeData = FindDyeDataById(id);
        if (dyeData == null) Debug.LogError($"Dye with ID {id} not found.");
        return dyeData;
    }

    public static bool TryGetDyeDataFromColorAndSpell(Color color, string spellId, out DyeData dyeData)
    {
        foreach (var data in Dye.dyeData)
        {
            var dyeMixture = data.dyeMixtures.FirstOrDefault(m => m.mixColor == color && m.mixSpellId == spellId);
            if (dyeMixture != null)
            {
                dyeData = data;
                return true;
            }
        }

        dyeData = null;
        return false;
    }

    
    public static Color GetEvaluatedColor(string originSpellId, string hitSpellId)
    {
        var color = new Color(1, 1, 1, 1);
        foreach (var data in dyeData)
        {
            if (data.spellId == originSpellId)
            {
                if (data.spellId == hitSpellId) return data.color;

                foreach (var mixture in data.dyeMixtures)
                {
                    if (mixture.mixSpellId == hitSpellId)
                    {
                        color = mixture.mixColor;
                        if (Settings.debug) Debug.Log($"Stack Trace: \n{System.Environment.StackTrace}\n" + $"Match found:\n" + $"- Color: {color}\n" + $"- Json: {data.id}\n" + $"- Spell: {data.spellId}\n" + $"- Mix: {mixture.mixSpellId}");
                    }
                    
                }
            }
        }

        return color;
    }


    public static ColorType GetColorType(string originSpellId, string hitSpellId)
    {
        foreach (var data in dyeData)
        {
            if (data.spellId == originSpellId)
            {
                if (data.spellId == hitSpellId) return ColorType.Solid;

                foreach (var mixture in data.dyeMixtures)
                {
                    if (mixture.mixSpellId == hitSpellId)
                        return ColorType.Mix;
                }
            }
        }

        return ColorType.Solid;
    }
    
    private static DyeData FindDyeDataById(string id)
    {
        return dyeData.FirstOrDefault(d => d.id == id);
    }
}

