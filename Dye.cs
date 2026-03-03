#if !SDK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crystallic.Skill.Spell.Attunement;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Crystallic
{
    public class Dye : ThunderScript
    {
        public static Dictionary<string, int> colorProperties = new()
        {
            ["ThunderRoad/VFX/vfx_fake_bloom_additive_HD"] = Shader.PropertyToID("_BloomColor"),
            ["ThunderRoad/VFX/vfx_hdr_additive_flowmap"] = Shader.PropertyToID("_Color"),
            ["Hovl/Particles/Add_CenterGlow"] = Shader.PropertyToID("_Color"),
            ["Kripto FX/RE2/Distortion"] = Shader.PropertyToID("_Color"),
            ["Shader Graphs/URP_Add_CG"] = Shader.PropertyToID("_Color"),
            ["AllIn1Vfx/AllIn1VfxURP"] = Shader.PropertyToID("_Color"),
            ["Piloto Studio/UberFX"] = Shader.PropertyToID("_MainTextureChannel")
        };

        // Not populated unless Rainbow Mode is enabled. Remember, I never said this was optimised.
        public static Dictionary<Material, Color> baseColors;
        public static List<Material> allMaterials;

        public static Color defaultColor = Color.white;
        public static bool rainbowModeWasActivatedThisSession = false;
        public static bool rainbowMode = false;

        [ModOption("Rainbow Mode", "Modifies a few effects turning them into rainbows. If you have epilepsy I recommend you avoid this."), ModOptionCategory("Crystallisation", -1), ModOptionOrder(2)]
        public static void SetRainbowMode(bool active)
        {
            if (!rainbowModeWasActivatedThisSession && active && allMaterials.IsNullOrEmpty())
            {
                rainbowModeWasActivatedThisSession = true;
                CacheMaterials();
            }
            else if (rainbowModeWasActivatedThisSession && !active && !allMaterials.IsNullOrEmpty())
                RestoreMaterials();

            rainbowMode = active;
        }

        public static List<DyeData> dyeData = new();
        public static Action onDyeDataLoaded;
        private static float lastRefreshTime;

        public static void CacheMaterials()
        {
            allMaterials = new List<Material>();
            baseColors = new Dictionary<Material, Color>();
            foreach (EffectData effectData in Catalog.GetDataList<EffectData>())
            {
                if (effectData.groupId == "Crystallic" || effectData.id == "SpellOrbCrystallic")
                    foreach (EffectModule effectModule in effectData.modules)
                        if (effectModule is EffectModuleParticle effectModuleParticle)
                        {
                            foreach (ParticleSystem particleSystem in effectModuleParticle.effectParticlePrefab.GetComponentsInChildren<ParticleSystem>())
                            {
                                ParticleSystemRenderer particleSystemRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                                foreach (Material material in particleSystemRenderer.materials)
                                {
                                    if (colorProperties.TryGetValue(material.shader.name, out int colorPropertyId) && !baseColors.ContainsKey(material) && !allMaterials.Contains(material))
                                    {
                                        baseColors.Add(material, material.GetColor(colorPropertyId));
                                        allMaterials.Add(material);
                                    }
                                }
                            }

                        }
            }

            lastRefreshTime = Time.time;
        }

        public static void RestoreMaterials()
        {
            foreach (Material material in allMaterials)
            {
                if (baseColors.TryGetValue(material, out Color baseColor) && colorProperties.TryGetValue(material.shader.name, out int colorPropertyId))
                    material.SetColor(colorPropertyId, baseColor);
            }

            baseColors.Clear();
            allMaterials.Clear();

            baseColors = null;
            allMaterials = null;
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();
            if (Time.time - lastRefreshTime < 0.05f) return;
            lastRefreshTime = Time.time;
            if (rainbowMode && !allMaterials.IsNullOrEmpty() && !baseColors.IsNullOrEmpty())
            {
                Color color = Utils.GetColorOverTime() * 7;
                foreach (Material material in allMaterials)
                    if (colorProperties.TryGetValue(material.shader.name, out int colorPropertyId))
                    {
                        if (material.shader.name.Contains("Piloto"))
                            material.SetVector(colorPropertyId, color);
                        else material.SetColor(colorPropertyId, color * 0.25f);
                    }
            }
        }

        public override void ScriptEnable()
        {
            base.ScriptEnable();
            EventManager.onLevelLoad += OnLevelLoad;
            EventManager.onPossess += OnPossess;
        }

        public override void ScriptDisable()
        {
            base.ScriptDisable();
            EventManager.onLevelLoad -= OnLevelLoad;
            EventManager.onPossess -= OnPossess;
        }

        private void OnPossess(Creature creature, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
                return;
            SaveData.LoadAsync(() =>
            {
                foreach (SaveData.ModRequirement modRequirement in SaveData.instance.savedModRequirements)
                {
                    if (!modRequirement.messageSeen)
                    {
                        bool modFound = false;
                        bool requirementFound = false;
                        foreach (ModManager.ModData modData in ModManager.loadedMods)
                        {
                            if (modData.Name == modRequirement.modName)
                                modFound = true;

                            else if (modData.Name == modRequirement.requirementName)
                                requirementFound = true;
                        }

                        if (modFound && !requirementFound)
                        {
                            Debug.Log($"[Crystallic] Showing mod requirement message for mod: {modRequirement.modName}");
                            DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData(text: modRequirement.message, 1, isSkippable: false, dismissTime: 10f, dismissAutomatically: true, anchorType: MessageAnchorType.Head));
                            modRequirement.messageSeen = true;
                            SaveData.SaveAsync();
                        }

                        break;
                    }
                }
            });
        }

        private void OnLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime) => Utils.Validate(() => eventTime == EventTime.OnEnd, () => Load());

        public static void Load()
        {
            SaveData.SaveAsync();
            dyeData.Clear();
            dyeData = Catalog.GetDataList<DyeData>();
            onDyeDataLoaded?.Invoke();
            Debug.Log("[Crystallic] Loaded Dye Data:\n - " + string.Join("\n - ", dyeData.Select(d => d.id)));
        }

        public static Color GetEvaluatedColor(DyeData sourceData, DyeData targetData)
        {
            string sourceSpellId = sourceData.id.Replace("Dye", "");
            string targetSpellId = targetData.id.Replace("Dye", "");
            return GetEvaluatedColor(sourceSpellId, targetSpellId);
        }

        public static Color GetEvaluatedColor(string sourceSpellId, string targetSpellId)
        {
            var result = TryGetColor(sourceSpellId, targetSpellId);
            if (result.found)
                return result.color;

            result = TryGetColor(targetSpellId, sourceSpellId);
            if (result.found)
                return result.color;

            Debug.LogWarning($"[Crystallic] Unable to find interpolated mix between [{sourceSpellId}] and [{targetSpellId}], default will be used!");
            return defaultColor;
        }

        private static (bool found, Color color) TryGetColor(string source, string target)
        {
            foreach (var data in dyeData)
            {
                if (data.spellId != source) continue;

                if (data.spellId == target)
                    return (true, data.color);

                foreach (var mixture in data.dyeMixtures)
                    if (mixture.mixSpellId == target)
                        return (true, mixture.mixColor);
            }

            return (false, default);
        }

        public static ColorType GetColorType(string sourceSpellId, string targetSpellId)
        {
            foreach (var data in dyeData)
                if (data.spellId == sourceSpellId)
                {
                    if (data.spellId == targetSpellId) return ColorType.Solid;
                    foreach (var mixture in data.dyeMixtures)
                        if (mixture.mixSpellId == targetSpellId)
                            return ColorType.Mix;
                }

            return ColorType.Solid;
        }
    }
}
#endif
