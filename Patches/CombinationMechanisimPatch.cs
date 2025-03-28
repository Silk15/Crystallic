using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Patches;

[HarmonyPatch]
public class CombinationMechanismPatch
{
    [ModOption("Force Imbue Mechanisms", "Force imbues all the mechanisms in the level with Crystallc energy, for testing."), ModOptionCategory("Debug", 99), ModOptionButton]
    public static void ForceImbue(bool _)
    {
        if (Level.current == null || Player.currentCreature == null) return;
        if (Level.current.data.id != "Tower") Debug.LogWarning("Cannot imbue mechanisms, you are not on the tower map!");
        else
        {
            var combinations = GameObject.FindObjectsOfType<CombinationImbuedMechanism>(true);
            foreach (var combination in combinations)
            {
                var combinationField = combination.GetType().GetField("combination", BindingFlags.NonPublic | BindingFlags.Instance);
                var combination1 = combinationField.GetValue(combination) as Dictionary<ColliderGroup, string>;
                foreach (var colliderGroup in combination1.Keys) colliderGroup.imbue.Transfer(Catalog.GetData<SpellCastCharge>("Crystallic"), 100);
            }
        }
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CombinationImbuedMechanism), "Awake");
    }

    private static void Postfix(CombinationImbuedMechanism __instance)
    {
        __instance.RunAfter(() =>
        {
            if (!EndingContent.GetCurrent().endingComplete && EndingContent.GetCurrent().hasT4Skill)
            {
                __instance.isOrderedConbination = false;
                var combinationField = __instance.GetType().GetField("combination", BindingFlags.NonPublic | BindingFlags.Instance);
                if (combinationField != null)
                {
                    var combination = combinationField.GetValue(__instance) as Dictionary<ColliderGroup, string>;
                    if (combination != null)
                    {
                        var list = new List<ColliderGroup>();
                        foreach (var item in combination) list.Add(item.Key);
                        combination.Clear();
                        foreach (var colliderGroup in list) combination.Add(colliderGroup, "Crystallic");
                    }
                }
            }
        }, 5);
    }
}