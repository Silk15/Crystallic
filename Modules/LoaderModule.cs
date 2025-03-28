using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace Crystallic.Modules;

public class LoaderModule : GameModeModule
{
    public bool loaded;
    public List<LorePack> lorePacks;

    public void LogAllLore()
    {
        var finalIndex = 0;
        Debug.Log("Custom lore loader module located in instance: " + GameModeManager.instance.currentGameMode.id + " Implementing lore packs: ");
        for (var i = 0; i < lorePacks.Count; i++)
        {
            finalIndex = i;
            Debug.Log("Lore pack: " + lorePacks[i].packId + " found for lore group: " + lorePacks[i].groupId);
        }

        Debug.Log("Total lore pack load count: " + (finalIndex + 1) + ". Expected count: " + lorePacks.Count);
    }

    public bool SpawnLore(string lorePackId, Vector3 position, Quaternion rotation)
    {
        for (var index = 0; index < lorePacks.Count; ++index)
        {
            var pack = lorePacks[index];
            if (pack.packId == lorePackId)
            {
                ItemData outputData;
                if (!Catalog.TryGetData(pack.lore[0].itemId, out outputData)) return false;
                outputData.SpawnAsync(item => ItemSpawn(item, gameMode.GetModule<LoreModule>(), pack), position, rotation);
                return true;
            }
        }

        return false;
    }

    private void ItemSpawn(Item item, LoreModule module, LorePack pack)
    {
        item.DisallowDespawn = true;
        item.transform.GetComponentInChildren<ILoreDisplay>().SetMultipleLore(module, null, pack.lore);
        item.OnGrabEvent += MarkAsRead;

        void MarkAsRead(Handle handle, RagdollHand hand)
        {
            item.OnGrabEvent -= MarkAsRead;
            if (pack.packId == "CrystallicStart") StartContent.GetCurrent().loreFound = true;
            module.ReleaseLore(LoreScriptableObject.GetLoreHashId(pack.packId), true);
            Catalog.GetData<EffectData>("LoreFound").Spawn(item.transform).Play();
        }
    }

    public override IEnumerator OnLoadCoroutine()
    {
        LogAllLore();
        if (!loaded && lorePacks != null)
        {
            loaded = true;
            LoreModule module;
            if (TryGetLore(out module))
            {
                var loreDict = GetCurrentLoreDict();
                foreach (var loreEntry in lorePacks)
                {
                    var customLorePack = loreEntry;
                    LoreScriptableObject loreGroup;
                    if (loreDict.TryGetValue(customLorePack.groupId, out loreGroup))
                    {
                        var pack = customLorePack.ToLorePack();
                        if (pack != null)
                        {
                            var list = new List<LoreScriptableObject.LorePack>(loreGroup.allLorePacks) { pack };
                            loreGroup.rootLoreHashIds.Add(pack.hashId);
                            typeof(LoreScriptableObject).GetField("_hashIdToLorePack", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(loreGroup, null);
                            loreGroup.allLorePacks = list.ToArray();
                            loreGroup = null;
                            pack = null;
                            list = null;
                            customLorePack = null;
                            list = null;
                        }

                        pack = null;
                    }

                    customLorePack = null;
                    loreGroup = null;
                }

                module.InitLoreState();
                loreDict = null;
            }

            module = null;
        }

        return base.OnLoadCoroutine();
    }

    public bool TryGetLore(out LoreModule module)
    {
        return GameModeManager.instance.currentGameMode.TryGetModule(out module);
    }

    public List<int> GetAvailableLore()
    {
        LoreModule module;
        return TryGetLore(out module) ? module.availableLore : null;
    }

    public List<LoreScriptableObject> GetAllLoreScriptableObjects()
    {
        LoreModule module;
        return TryGetLore(out module) ? module.allLoreSO : null;
    }

    public Dictionary<string, LoreScriptableObject> GetCurrentLoreDict()
    {
        var scriptableObjects = GetAllLoreScriptableObjects();
        if (scriptableObjects == null) return null;
        var currentLoreDict = new Dictionary<string, LoreScriptableObject>();
        for (var index = 0; index < scriptableObjects.Count; ++index)
        {
            var allLorePacks = scriptableObjects[index].allLorePacks;
            string key = null;
            int num;
            if (allLorePacks != null && allLorePacks.Length != 0)
            {
                key = allLorePacks[0].groupId;
                num = key == null ? 1 : 0;
            }
            else
            {
                num = 1;
            }

            if (num == 0) currentLoreDict[key] = scriptableObjects[index];
        }

        return currentLoreDict;
    }
}