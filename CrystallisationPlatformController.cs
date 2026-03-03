#if !SDK
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;
using QualityLevel = ThunderRoad.QualityLevel;

namespace Crystallic
{
    public static class CrystallisationPlatformController
    {
        [ModOption("Max Crystallised Entities", "Controls the maximum amount of crystallised NPCs excluding the Golem.", order = 1), ModOptionSlider, ModOptionCategory("Performance", -2), ModOptionIntValues(1, 50, 1)]
        public static int maxCrystallisedEntities = 10;

        [ModOption("Blend Mode", "Controls whether the crystallisation Vfx are visually transparent. This does not outright disable transparency meaning it is still rendered as a transparent object. This provides a small performance gain on its own. \n\n- Zero = Not transparent, incompatible with soft particles\n\n- Src Alpha Saturate = Not transparent, incompatible with soft particles\n\n- One Minus Src Alpha = Default. Both transparent and compatible with soft particles but slightly more expensive to render", nameof(blendModeValues), order = 3, defaultValueIndex = 2), ModOptionButton, ModOptionCategory("Performance", -2)]
        public static void SetBlendMode(int value) => LoadMaterial(material =>
        {
            material.SetFloat(_SourceBlendRBG, value);
            currentBlendMode = value;
        });

        [ModOption("Max Crystallisation Particles", "Controls the maximum amount of particles the crystallisation Vfx can display at one time.", order = 0, defaultValueIndex = 24), ModOptionSlider, ModOptionCategory("Performance", -2), ModOptionIntValues(1, 50, 1)]
        public static void SetMaxParticles(int value)
        {
            maxParticles = value;
            if (Player.currentCreature == null)
                return;

            foreach (Creature creature in Creature.allActive)
                creature.brain.instance.GetModule<BrainModuleCrystal>().SetMaxParticles(value);
        }

        [ModOptionPlatformIndex(new[]
        {
            QualityLevel.Android,
            QualityLevel.Windows
        }, new[]
        {
            0,
            1
        }), ModOption("Soft Particles", "Controls whether the crystallisation Vfx smooth out sharp intersections and edges. This reduces the jagged cutting that most effects create but is expensive to render.", order = 4, defaultValueIndex = 1), ModOptionButton, ModOptionCategory("Performance", -2)]
        public static void SetSoftParticles(bool active)
        {
            LoadMaterial(mat =>
            {
                if (active)
                {
                    mat.EnableKeyword(_USESOFTALPHA_ON);
                    mat.SetFloat(_UseSoftAlpha, 0.1f);
                }
                else
                {
                    mat.DisableKeyword(_USESOFTALPHA_ON);
                    mat.SetFloat(_UseSoftAlpha, 0.1f);
                }
            });
        }

        public static ModOptionParameter[] blendModeValues = new ModOptionParameter[3]
        {
            new ModOptionInt("Zero \n(Opaque)", 0),
            new ModOptionInt("Src Alpha Saturate \n(Opaque)", 9),
            new ModOptionInt("One Minus Src Alpha \n(Transparent)", 10)
        };

        public const string MaterialAddress = "Silk.Material.Spell.Crystallic.Crystallisation";

        public static readonly int _UseSoftAlpha = Shader.PropertyToID("_UseSoftAlpha");
        public static readonly string _USESOFTALPHA_ON = "_USESOFTALPHA_ON";

        public static readonly int _SourceBlendRBG = Shader.PropertyToID("_SourceBlendRBG");

        public static Material loadedMaterial;
        public static int maxParticles = 25;
        public static int currentBlendMode = 10;

        static CrystallisationPlatformController()
        {
            LoadMaterial(material =>
            {
                if (Common.IsAndroid)
                    material.DisableKeyword(_USESOFTALPHA_ON);
            });
        }

        public static void LoadMaterial(Action<Material> onLoaded)
        {
            if (loadedMaterial != null)
            {
                onLoaded?.Invoke(loadedMaterial);
                return;
            }

            Catalog.LoadAssetAsync(MaterialAddress, onLoaded, "Crystallisation Material");
        }
    }
}
#endif