using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Patches;

public class SkillTreeOrbPatch
{
    [HarmonyPatch(typeof(SkillTreeOrb), "Init"), HarmonyPostfix]
    public static void SkillOrbInitPostfix(SkillTreeOrb __instance)
    {
        __instance.distanceForceRelease = Mathf.Max((__instance.skillTree.maxTierInTree - 3f) * 0.6f + 3f, 3f);
    }
}