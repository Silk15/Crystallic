using Crystallic.Modules;
using HarmonyLib;
using ThunderRoad;
using ThunderRoad.Modules;

namespace Crystallic.Patches;

[HarmonyPatch(typeof(HomeTower), nameof(HomeTower.RaidCompleted))]
public class RaidStopPatch
{
    public static void Postfix(HomeTower __instance)
    {
        if (!InvasionContent.GetCurrent().invasionComplete)
        {
            InvasionModule.invasionActive = false;
            InvasionContent.GetCurrent().invasionComplete = true;
            if (InvasionModule.loopMusicEffect != null) InvasionModule.loopMusicEffect.End();
            GameManager.local.StartCoroutine(InvasionModule.FadeMusic(0, 1, 5));
            if (GameModeManager.instance.currentGameMode.TryGetModule<CrystalHuntProgressionModule>(out var module)) module.SetEndGameState(CrystalHuntProgressionModule.EndGameState.Locked); 
        }
    }
}