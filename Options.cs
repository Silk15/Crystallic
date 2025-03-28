using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic;

public class Options : ThunderScript
{
    [ModOption("Enemies Use Spell", "Enables/Disables enemies using the spell."), ModOptionCategory("Creatures", -1)]
    public static bool useSpell = true;

    [ModOption("Enemies Use Crystal Imbues", "Enables/Disables enemies using the Crystal Imbues."), ModOptionCategory("Creatures", -1)]
    public static bool useCrystalImbues = true;

    [ModOption("Enemies Use Crystallic Imbue", "Enables/Disables enemies using the Crystallic imbue."), ModOptionCategory("Creatures", -1)]
    public static bool useImbues = true;

    [ModOption("Enemies Use Misc Skills", "Enables/Disables enemies using misc Crystallic skills."), ModOptionCategory("Creatures", -1)]
    public static bool useMisc = true;

    public static Dictionary<Creature, List<string>> unloaded = new();

    public static Settings settings;

    [ModOption("Refresh Enemies", "Refreshes the creature skills, if you've re-enabled a previously disabled option and don't want to restart the level, use this."), ModOptionCategory("Creatures", -1), ModOptionButton]
    public static void Refresh(bool _)
    {
        for (var index = 0; index < Creature.allActive.Count; index++)
        {
            var creature = Creature.allActive[index];
            if (!creature.isKilled && !creature.isPlayer && unloaded.ContainsKey(creature))
            {
                Debug.Log($"Refreshing skills for {creature}:\n - " + string.Join("\n - ", unloaded[creature]));
                foreach (var value in unloaded[creature])
                    creature.ForceLoadSkill(value);
            }
        }
    }

    public override void ScriptEnable()
    {
        base.ScriptEnable();
        settings = Catalog.GetData<Settings>("Settings");
        EventManager.onCreatureSpawn += OnCreatureSpawn;
    }
    
    private void OnCreatureSpawn(Creature creature)
    {
        if (creature.isPlayer) return;
        if (!useSpell && !string.IsNullOrEmpty(settings.spellId) && creature.HasSkill(settings.spellId))
        {
            creature.ForceUnloadSkill(settings.spellId);
            if (!unloaded.ContainsKey(creature)) unloaded.Add(creature, new List<string>());
            unloaded[creature].Add(settings.spellId);
        }

        if (!useCrystalImbues && settings.skills["crystalImbues"] != null)
        {
            var ids = settings.skills["crystalImbues"];
            for (var i = 0; i < ids.Count; i++)
                if (creature.HasSkill(ids[i]))
                {
                    creature.ForceUnloadSkill(ids[i]);
                    if (!unloaded.ContainsKey(creature)) unloaded.Add(creature, new List<string>());
                    unloaded[creature].Add(ids[i]);
                }
        }

        if (!useImbues && settings.skills["imbues"] != null)
        {
            var ids = settings.skills["imbues"];
            for (var i = 0; i < ids.Count; i++)
                if (creature.HasSkill(ids[i]))
                {
                    creature.ForceUnloadSkill(ids[i]);
                    if (!unloaded.ContainsKey(creature)) unloaded.Add(creature, new List<string>());
                    unloaded[creature].Add(ids[i]);
                }
        }

        if (!useMisc && settings.skills["misc"] != null)
        {
            var ids = settings.skills["misc"];
            for (var i = 0; i < ids.Count; i++)
                if (creature.HasSkill(ids[i]))
                {
                    creature.ForceUnloadSkill(ids[i]);
                    if (!unloaded.ContainsKey(creature)) unloaded.Add(creature, new List<string>());
                    unloaded[creature].Add(ids[i]);
                }
        }
    }
}