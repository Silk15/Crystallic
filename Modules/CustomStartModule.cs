using System.Collections;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace Crystallic.Modules;

public class CustomStartModule : GameModeModule
{
    public override IEnumerator OnLoadCoroutine()
    {
        EventManager.onPossess += OnPossess;
        return base.OnLoadCoroutine();
    }

    public override void OnUnload()
    {
        base.OnUnload();
        EventManager.onPossess -= OnPossess;
    }

    private void OnPossess(Creature creature, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart || StartContent.GetCurrent().loreFound) return;
        creature.RunAfter(() =>
        {
            var loreSpawner = GameObject.Find("Raein Journal 3").GetComponent<LoreSpawner>();
            foreach (Item item in Item.allActive.ToList())
                if (item.data.id == "CrystalCrystallicT1")
                    foreach (Item item1 in Item.InRadius(item.transform.position, 1))
                        if (item1.data.id != "CrystalCrystallicT1" && item1.data.id != "DaggerCommon") item1.Despawn();
            if (GameModeManager.instance.currentGameMode.TryGetModule(out LoaderModule loader) && loader.SpawnLore("CrystallicStart", loreSpawner.transform.position, loreSpawner.transform.rotation)) GameObject.Destroy(loreSpawner);
            else Debug.LogError("No lore loader module found in current game mode or spawning of lore failed!");
        },1);
    }
}