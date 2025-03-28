using HarmonyLib;
using ThunderRoad;

namespace Crystallic.Patches;

[HarmonyPatch(typeof(Tower), nameof(Tower.StartAnnihilationEnding))]
public class AnnihilationEndingPatch
{
    public static bool Prefix(Tower __instance)
    {
        if (EndingContent.GetCurrent().endingComplete || !EndingContent.GetCurrent().hasT4Skill) return true;
        Ending.StartCrystallicEnding(__instance);
        return false;
    }
}