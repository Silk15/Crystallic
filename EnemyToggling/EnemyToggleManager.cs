#if !SDK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crystallic.Skill.Imbue;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.EnemyToggling
{
    public static class EnemyToggleManager
    {
        [ModOption("Toggle Category", "Use this toggle to dynamically disable enemy skills.", nameof(EnemyToggleCategories), order = 0), ModOptionArrows, ModOptionSave, ModOptionCategory("Enemy Skills", -99)]
        public static void ToggleCategory(string category)
        {
            currentCategory = category;

            if (!ModManager.TryGetModData(typeof(EnemyToggleManager).Assembly, out ModManager.ModData modData))
                return;

            if (!modData.TryGetModOption("Toggle", out ModOption modOption))
                return;

            modifyingValues = true;
            bool enabled = categoryValues.ContainsKey(currentCategory) ? categoryValues[currentCategory] : true;

            modOption.parameterValues = enabled ? enabledConditions : enabledConditions.Reverse().ToArray();
            modOption.Reload();
            modifyingValues = false;
        }

        [ModOption("Toggle", "Toggles the skill category defined above.", order = 1), ModOptionButton, ModOptionSave, ModOptionCategory("Enemy Skills", -99)]
        public static void Toggle(bool active)
        {
            if (categoryValues.IsNullOrEmpty())
            {
                SaveData.LoadAsync(() =>
                {
                    categoryValues = new Dictionary<string, bool>(SaveData.instance.savedCategoryValues);
                    TryUpdate();
                });
                return;
            }

            TryUpdate();

            void TryUpdate()
            {
                if (enemyToggleData == null || Player.currentCreature == null || modifyingValues)
                    return;

                Toggle(enemyToggleData.categories.FirstOrDefault(c => c.id == currentCategory)?.skillIds, active);
                categoryValues[currentCategory] = active;

                SaveData.instance.savedCategoryValues = categoryValues;
                SaveData.SaveAsync();
            }
        }

        public static Dictionary<string, bool> categoryValues = new();
        public static List<ToggledEnemy> toggledEnemies = new();
        public static EnemyToggleData enemyToggleData;
        public static Action onBeforeToggle;

        public static string currentCategory;
        public static bool modifyingValues = false;

        public static SkillData[] allSkills;

        public static ModOptionBool[] enabledConditions = new ModOptionBool[]
        {
            new("Disable", true),
            new("Enable", false),
        };

        public static ModOptionString[] EnemyToggleCategories
        {
            get
            {
                if (enemyToggleData == null) enemyToggleData = Catalog.GetData<EnemyToggleData>("EnemyToggles");
                return enemyToggleData.categories.Select(c => new ModOptionString(c.id, c.id)).ToArray();
            }
        }

        static EnemyToggleManager() => GameManager.local.StartCoroutine(DelayCoroutine());

        public static IEnumerator DelayCoroutine()
        {
            yield return new WaitUntil(() => Catalog.IsJsonLoaded() && ModManager.gameModsLoaded);

            enemyToggleData = Catalog.GetData<EnemyToggleData>("EnemyToggles");
            allSkills = Catalog.GetDataList<SkillData>().ToArray();

            EventManager.onCreatureSpawn -= OnCreatureSpawn;
            EventManager.onCreatureSpawn += OnCreatureSpawn;
        }

        public static void OnCreatureSpawn(Creature creature)
        {
            if (creature.isPlayer)
                return;

            for (int i = toggledEnemies.Count - 1; i >= 0; i--)
                if (toggledEnemies[i].creature == creature)
                    toggledEnemies.RemoveAt(i);

            ToggledEnemy toggledEnemy = new ToggledEnemy(creature);
            toggledEnemies.Add(toggledEnemy);

            foreach (var toggledCategory in categoryValues)
                Toggle(enemyToggleData.categories.FirstOrDefault(c => c.id == toggledCategory.Key)?.skillIds, toggledCategory.Value);
        }

        public static void Toggle(string[] skillIds, bool active)
        {
            onBeforeToggle?.Invoke();
            CrystalImbueSkillData.SaveImbues();
            for (int i = 0; i < skillIds.Length; i++)
                Toggle(skillIds[i], active);
        }

        public static void Toggle(string skillId, bool active)
        {
            for (int i = 0; i < toggledEnemies.Count; i++)
            {
                ToggledEnemy toggledEnemy = toggledEnemies[i];
                if (active == toggledEnemy.Has(skillId)) return;

                if (active)
                {
                    toggledEnemy.Load(skillId);
                    Debug.Log($"Loading skill: {skillId} on creature: {toggledEnemy.creature.data.id}");
                }
                else
                {
                    toggledEnemy.Unload(skillId);
                    Debug.Log($"Unloading skill: {skillId} on creature: {toggledEnemy.creature.data.id}");
                }
            }
        }
    }
}
#endif