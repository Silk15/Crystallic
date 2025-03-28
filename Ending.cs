using System;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace Crystallic;

public class Ending
{
    public static Settings settings;
    public static bool isRunningCrystallicEnding;
    public static List<TowerLaser> lasers = new();

    public static void StartCrystallicEnding(Tower tower)
    {
        EventManager.onLevelUnload += OnLevelUnload;
        if (isRunningCrystallicEnding) return;
        isRunningCrystallicEnding = true;
        tower.RunAfter(() =>
        {
            TrapPlayer(tower);
            if (GameModeManager.instance.currentGameMode.TryGetModule<CrystalHuntProgressionModule>(out var module)) module.SetEndGameState(CrystalHuntProgressionModule.EndGameState.AnnihilationEnding);
            settings = Catalog.GetData<Settings>("Settings");
            tower.RunAfter(() =>
            {
                Catalog.LoadAssetAsync<RuntimeAnimatorController>(settings.laserAnimatorControllerAddress, controller =>
                {
                    var music = Catalog.GetData<EffectData>(settings.endingMusicEffectId);
                    foreach (var audioSource in GameObject.FindObjectsOfType<AudioSource>())
                        if (audioSource.gameObject.name == "AudioMusicEntry")
                            audioSource.Stop();
                    var music1 = music.Spawn(Player.currentCreature.ragdoll.targetPart.transform);
                    music1.Play();
                    foreach (TowerLaserType type in Enum.GetValues(typeof(TowerLaserType)))
                    {
                        var part = type == TowerLaserType.Front ? "Part  " : "Part ";
                        var huntingFor = $"Laser Mobile {part}{type.ToString().ToUpper()}";
                        var obj = GameObject.Find(huntingFor);
                        if (obj != null)
                        {
                            var laser = obj.gameObject.AddComponent<TowerLaser>();
                            laser.RunAfter(() =>
                            {
                                laser.Init(type, controller, settings);
                                lasers.Add(laser);
                                if (type == TowerLaserType.Right)
                                    laser.RunAfter(() =>
                                    {
                                        Catalog.LoadAssetAsync<GameObject>("Silk.Prefab.Waves", obj =>
                                        {
                                            var o = GameObject.Instantiate(obj);
                                            laser.RunAfter(() =>
                                            {
                                                music1.Stop();
                                                ReleasePlayer(tower);
                                                Player.currentCreature.AddForce(-Player.currentCreature.transform.forward * 2.5f, ForceMode.Impulse);
                                                var shakers = o.GetComponentsInChildren<Shaker>();
                                                shakers[0].Begin();
                                                shakers[1].Begin();
                                                shakers[1].RunAfter(() => { shakers[1].End(); }, 5);
                                                var door = GameObject.FindObjectOfType<DalgarianDoor>();
                                                door.CloseDoor();
                                                laser.RunAfter(() =>
                                                {
                                                    foreach (var creatureSpawner in o.GetComponentsInChildren<CreatureSpawner>())
                                                    {
                                                        creatureSpawner.spawnAtRandomWaypoint = true;
                                                        creatureSpawner.SetCreaturesToWaveNPCS();
                                                        creatureSpawner.Spawn();
                                                    }

                                                    var spawners = o.GetComponentsInChildren<WaveSpawner>();
                                                    spawners[1].StartWave("SoldierInvasion");
                                                }, 10);
                                            }, 5);
                                        }, "GameObject.Waves");
                                    }, 25);
                            }, settings.endingTimings[type]);
                        }
                    }
                }, "Controller");
            }, 5f);
        }, 5f);
    }

    public static void TrapPlayer(Tower tower)
    {
        Player.TogglePlayerMovement(false);
        Player.TogglePlayerJump(false);
        Player.local.creature.mana.casterRight.DisallowCasting(tower);
        Player.local.creature.mana.casterRight.DisableSpellWheel(tower);
        Player.local.creature.mana.casterLeft.DisallowCasting(tower);
        Player.local.creature.mana.casterLeft.DisableSpellWheel(tower);
        Player.local.locomotion.physicBody.isKinematic = true;
        Player.local.locomotion.enabled = false;
        Player.currentCreature.SetPhysicModifier(tower, 0.0f);
        var startPosition = Player.local.transform.position;
        Player.currentCreature.ProgressiveAction(tower.trapToTargetDuration, t => Player.local.creature.currentLocomotion.physicBody.transform.position = Vector3.Lerp(startPosition, Player.local.GetPlayerPositionRelativeToHead(tower.shootingPosition.position), tower.levitationForceCurve.Evaluate(t)));
    }

    public static void ReleasePlayer(Tower tower)
    {
        Player.TogglePlayerMovement(true);
        Player.TogglePlayerJump(true);
        Player.local.creature.mana.casterRight.AllowCasting(tower);
        Player.local.creature.mana.casterRight.AllowSpellWheel(tower);
        Player.local.creature.mana.casterLeft.AllowCasting(tower);
        Player.local.creature.mana.casterLeft.AllowSpellWheel(tower);
        Player.local.locomotion.physicBody.isKinematic = false;
        Player.local.locomotion.enabled = true;
        Player.currentCreature.SetPhysicModifier(tower, 1);
    }

    private static void OnLevelUnload(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
    {
        EventManager.onLevelUnload -= OnLevelUnload;
        isRunningCrystallicEnding = false;
    }
}