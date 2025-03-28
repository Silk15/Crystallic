using HarmonyLib;
using ThunderRoad;

namespace Crystallic.Patches;

[HarmonyPatch(typeof(Tower), nameof(Tower.TeleporterToHome))]
public class TeleporterToHomePatch
{
    public static bool Prefix(Tower __instance)
    {
        if (!Ending.isRunningCrystallicEnding) return true;
        __instance.teleporterLoadLevelAnnihilation.LoadLevel();
        EndingContent.GetCurrent().endingComplete = true;
        return false;
    }
}