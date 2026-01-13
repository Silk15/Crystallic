using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill.SpellPower;
using UnityEngine;

namespace Crystallic.Skill.Spell.SlowTime;

public class SkillTemporalImbuement : SlowTimeSkillData
{
    public Dictionary<string, Gradient[]> cachedTemporalGradients = new();
    public List<SkillSpellPair> skillSpellPairs = new();
    

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        GameManager.local.StartCoroutine(CreateGradientsCoroutine());
    }

    public IEnumerator CreateGradientsCoroutine()
    { 
        Dictionary<string, Gradient> temporalShaderGradients = new(); 
        Dictionary<string, Gradient> originalShaderGradients = new();
        
        originalShaderGradients.Clear();
        foreach (SpellCastCharge spellCastCharge in Catalog.GetDataList<SpellCastCharge>())
            if (spellCastCharge.imbueBladeEffectData != null)
                foreach (EffectModule effectModule in spellCastCharge.imbueBladeEffectData.modules)
                    if (effectModule is EffectModuleShader effectModuleShader)
                    {
                        Color colorA = Common.IsWindows ? effectModuleShader.mainColorStart : effectModuleShader.mainNoHdrColorStart;
                        Color colorB = Common.IsWindows ? effectModuleShader.mainColorEnd : effectModuleShader.mainNoHdrColorEnd;
                        originalShaderGradients.Add(spellCastCharge.id, ThunderRoad.Utils.CreateGradient(colorA, colorB));
                    }

        foreach (string spellId in originalShaderGradients.Keys)
        {
            Color temporalColor = Dye.GetEvaluatedColor("Mind", spellId);
            Gradient temporalGradient = new Gradient();
            temporalGradient.SetKeys(new []
            {
                new GradientColorKey(temporalColor, 0.0f),
                new GradientColorKey(temporalColor, 1.0f)
            }, new []
            {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            });
            
            temporalShaderGradients.Add(spellId, temporalGradient);
        }

        foreach (string spellId in originalShaderGradients.Keys)
        {
            List<Gradient> frameGradients = new();
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                Gradient frameGradient = new Gradient();

                Color frameColor = Color.Lerp(originalShaderGradients[spellId].colorKeys[0].color, temporalShaderGradients[spellId].colorKeys[0].color, elapsed);
                
                frameGradient.SetKeys(new []
                {
                    new GradientColorKey(frameColor, 0.0f),
                    new GradientColorKey(frameColor, 1.0f)
                }, new []
                {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                });
                
                frameGradients.Add(frameGradient);
                
                elapsed += Time.deltaTime;
                yield return Yielders.EndOfFrame;
            }
            
            cachedTemporalGradients.Add(spellId, frameGradients.ToArray());
            yield return Yielders.EndOfFrame;
        }
    }

    public override void OnSlowMotionEnter(SpellPowerSlowTime spellPowerSlowTime, float scale)
    {
        base.OnSlowMotionEnter(spellPowerSlowTime, scale);

        foreach (ThunderRoad.Imbue imbue in ThunderRoad.Imbue.all)
            TryToggleImbue(imbue, true);
    }

    public override void OnSlowMotionExit(SpellPowerSlowTime spellPowerSlowTime)
    {
        base.OnSlowMotionExit(spellPowerSlowTime);
        foreach (ThunderRoad.Imbue imbue in ThunderRoad.Imbue.all)
            TryToggleImbue(imbue, false);
    }

    public void TryToggleImbue(ThunderRoad.Imbue imbue, bool active)
    {
        if (imbue.imbueCreature == Player.currentCreature)
            foreach (SkillSpellPair skillSpellPair in skillSpellPairs)
                if (skillSpellPair.spellId == imbue.spellCastBase.id && Player.currentCreature.HasSkill(skillSpellPair.skillId))
                {
                    if (imbue.spellCastBase.imbueEffect != null) foreach (Effect effect in imbue.spellCastBase.imbueEffect.effects)
                        if (effect is EffectShader effectShader) imbue.StartCoroutine(ShiftImbueColourCoroutine(skillSpellPair.spellId, active, effectShader));
                }
    }

    public IEnumerator ShiftImbueColourCoroutine(string spellId, bool active, EffectShader effectShader)
    {
        List<Gradient> frameGradients = cachedTemporalGradients[spellId].ToList();
        if (!active) 
            frameGradients.Reverse();

        float elapsed = 0f;
        int currentFrame = 0;

        while (elapsed < 1f)
        {
            Gradient frameGradient = frameGradients[currentFrame];
            currentFrame++;
            effectShader.SetMainGradient(frameGradient);
            
            elapsed += Time.deltaTime;
            yield return Yielders.EndOfFrame;
        }
    }
}