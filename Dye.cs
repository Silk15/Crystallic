using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crystallic.Skill.Spell.Attunement;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Crystallic;

public class Dye : ThunderScript
{
    public static Color defaultColor = Color.white;
    public static bool rainbowModeWasActivatedThisSession = false;
    public static bool rainbowMode = false;

    [ModOption("Rainbow Mode", "Modifies a few effects turning them into rainbows. If you have epilepsy I recommend you avoid this."), ModOptionCategory("Crystallisation", -1), ModOptionOrder(2)]
    public static void SetRainbowMode(bool active)
    {
        rainbowMode = active;
        if (active) 
            rainbowModeWasActivatedThisSession = true;
    }
    
    public static List<DyeData> dyeData = new();
    public static Action onDyeDataLoaded;

    public override void ScriptEnable()
    {
        base.ScriptEnable();
        EventManager.onLevelLoad += OnLevelLoad;
        EventManager.onPossess += OnPossess;
    }

    public override void ScriptDisable()
    {
        base.ScriptDisable();
        EventManager.onLevelLoad -= OnLevelLoad;
        EventManager.onPossess -= OnPossess;
    }
    
    private void OnPossess(Creature creature, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart)
            return;
        SaveData.LoadAsync(() =>
        {
            foreach (SaveData.ModRequirement modRequirement in SaveData.instance.savedModRequirements)
            {
                if (!modRequirement.messageSeen)
                {
                    bool modFound = false;
                    bool requirementFound = false;
                    foreach (ModManager.ModData modData in ModManager.loadedMods)
                    {
                        if (modData.Name == modRequirement.modName) 
                            modFound = true;
                        
                        else if (modData.Name == modRequirement.requirementName) 
                            requirementFound = true;
                    }

                    if (modFound && !requirementFound)
                    {
                        Debug.Log($"[Crystallic] Showing mod requirement message for mod: {modRequirement.modName}");
                        DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData(text: modRequirement.message, 1, isSkippable: false, dismissTime: 10f, dismissAutomatically: true, anchorType: MessageAnchorType.Head));
                        modRequirement.messageSeen = true;
                        SaveData.SaveAsync();
                    }
                    break;
                }
            }
        });
    }

    private void OnLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime) => Utils.Validate(() => eventTime == EventTime.OnEnd, () => Load());

    public static void Load()
    {
        SaveData.SaveAsync();
        dyeData.Clear();
        dyeData = Catalog.GetDataList<DyeData>();
        onDyeDataLoaded?.Invoke();
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

        Debug.LogWarning($"[Crystallic] Unable to find interpolated mix between [{originSpellId}] and [{hitSpellId}], default will be used!");
        return defaultColor;
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

