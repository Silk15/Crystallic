using System.Collections;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace Crystallic.Modules;

public class InvasionModule : GameModeModule
{
    [ModOption("Music Volume"), ModOptionCategory("Invasion", 12), ModOptionFloatValues(0, 1, 0.05f)]
    public static float volume = 1f;

    [ModOption("Can Invade"), ModOptionCategory("Invasion", 12)]
    public static bool canInvade = true;

    public static bool invasionActive;
    public static string overrideTableId = "SoldierAlertMelee";
    public static string overrideWaveId = "SoldierInvasion";
    public static string loopMusicEffectId = "InvasionMusic";
    public static EffectData loopMusicEffectData;
    public static EffectInstance loopMusicEffect;
    private static InvasionContent invasionContent;
    public static float musicVolume;

    public static bool CanInvade => canInvade && GameModeManager.instance.currentGameMode.TryGetGameModeSaveData<CrystalHuntSaveData>(out var saveData) && saveData.endGameState != CrystalHuntProgressionModule.EndGameState.LockedAndRaidDone;

    [ModOption("Force Start Invasion", "Forces the invasion to start on the home map, even in sandbox mode."), ModOptionCategory("Debug", 99), ModOptionButton]
    public static void ForceStartInvasion(bool _)
    {
        if (Level.current == null || Player.currentCreature == null) return;
        if (Level.current.data.id != "Home")
        {
            Debug.LogWarning("Cannot start invasion, Player is not on the home map!");
        }
        else
        {
            loopMusicEffectData = Catalog.GetData<EffectData>(loopMusicEffectId);
            InvasionContent.GetCurrent().invasionComplete = false;
            var homeTower = GameObject.FindObjectOfType<HomeTower>();
            TryStartInvasion(homeTower);
            ChangeMusic();
        }
    }

    public override IEnumerator OnLoadCoroutine()
    {
        loopMusicEffectData = Catalog.GetData<EffectData>(loopMusicEffectId);
        EventManager.onPossess += Begin;
        return base.OnLoadCoroutine();
    }

    public override void Update()
    {
        base.Update();
        if (invasionActive && loopMusicEffect != null) loopMusicEffect.SetIntensity(volume);
    }

    public override void OnUnload()
    {
        base.OnUnload();
        EventManager.onPossess -= Begin;
    }

    public static void Begin(Creature creature, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart) return;
        var homeTower = GameObject.FindObjectOfType<HomeTower>();
        invasionContent = InvasionContent.GetCurrent();
        if (Player.currentCreature.HasSkill("Crystallic") && CrystalHuntProgressionModule.GetProgressLevel() > 2 && CrystalHuntProgressionModule.GetProgressLevel() < CrystalHuntProgressionModule.endRaidLevel && !invasionContent.invasionComplete)
        {
            TryStartInvasion(homeTower);
            ChangeMusic();
        }
    }

    public static void TryStartInvasion(HomeTower homeTower)
    {
        if (!CanInvade) return;
        if (homeTower)
        {
            invasionActive = true;
            var spawners = GameObject.FindObjectsOfType<CreatureSpawner>(true);
            foreach (var spawner in spawners) spawner.creatureTableID = overrideTableId;
            var waveSpawners = GameObject.FindObjectsOfType<WaveSpawner>(true);
            var creatureSpawner = new GameObject("InitialSpawner").AddComponent<CreatureSpawner>();
            creatureSpawner.creatureTableID = overrideTableId;
            creatureSpawner.transform.position = new Vector3(45.219f, 2.005f, -56.397f);
            creatureSpawner.transform.rotation = new Quaternion(0, -60.844f, 0, 0);
            creatureSpawner.Spawn();
            foreach (var waveSpawner in waveSpawners) waveSpawner.startWaveId = overrideWaveId;
            homeTower.StartRaid();
            homeTower.RunAfter(() =>
            {
                foreach (var creatureSpawner in spawners)
                {
                    creatureSpawner.enemyConfigType = CreatureSpawner.EnemyConfigType.AlertMelee;
                    creatureSpawner.SetCreaturesToWaveNPCS();
                    creatureSpawner.Spawn();
                }

                foreach (var waveSpawner in waveSpawners)
                {
                    waveSpawner.StopWave(false);
                    waveSpawner.StartWave(overrideWaveId);
                }

                foreach (var creature in Creature.allActive) Player.currentCreature.brain.instance.GetModule<BrainModulePlayer>().SetExposure(BrainModulePlayer.Exposure.RangedCombat);
            }, 0.5f);
        }
    }

    public static void ChangeMusic()
    {
        GameManager.local.StartCoroutine(FadeMusic(1, 0, 5));
        loopMusicEffect = loopMusicEffectData.Spawn(Player.currentCreature.transform);
        Player.currentCreature.RunAfter(() =>
        {
            loopMusicEffect.Play();
            loopMusicEffect.SetIntensity(musicVolume);
        }, 5);
    }

    public static IEnumerator FadeMusic(float start, float end, float time)
    {
        var startTime = Time.realtimeSinceStartup;
        ThunderRoadSettings.current.audioMixer.SetFloat("MusicVolume", ThunderRoad.Utils.PercentageToDecibels(start));
        while (Time.realtimeSinceStartup - (double)startTime <= time)
        {
            ThunderRoadSettings.current.audioMixer.SetFloat("MusicVolume", ThunderRoad.Utils.PercentageToDecibels(Mathf.Lerp(start, end, (Time.realtimeSinceStartup - startTime) / time)));
            yield return 0;
        }

        ThunderRoadSettings.current.audioMixer.SetFloat("MusicVolume", ThunderRoad.Utils.PercentageToDecibels(end));
    }
}