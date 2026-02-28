using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crystallic.Skill.Imbue;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.Skill.SpellPower;
using UnityEngine;

namespace Crystallic.Skill.Spell.SlowTime;

public class SkillTemporalImbuement : SlowTimeSkillData
{
    #if !SDK
    public static List<ThunderRoad.Imbue> activeImbues = new();
    #endif
    
    public List<SkillSpellPair> skillSpellPairs = new();

    [NonSerialized, JsonIgnore]
    public Dictionary<string, Gradient[]> cachedTemporalGradients = new();
    
    #if !SDK
    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        Dye.onDyeDataLoaded += OnDyeDataLoaded;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        Dye.onDyeDataLoaded -= OnDyeDataLoaded;
    }

    public override void OnImbueLoad(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        if (imbue.imbueCreature == Player.currentCreature)
            foreach (SkillSpellPair skillSpellPair in skillSpellPairs)
            {
                if (skillSpellPair == null || imbue.spellCastBase == null)
                    return;

                if ((skillSpellPair.spellId == imbue.spellCastBase.id && Player.currentCreature.HasSkill(skillSpellPair.skillId)) || imbue.spellCastBase.id == "Crystallic")
                    activeImbues.Add(imbue);
            }
    }
    
    public override void OnImbueUnload(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        if (activeImbues.Contains(imbue)) activeImbues.Remove(imbue);
    }

    private void OnDyeDataLoaded() => GameManager.local.StartCoroutine(CreateGradientsCoroutine());

    public IEnumerator CreateGradientsCoroutine()
    {
        cachedTemporalGradients.Clear();
        Dictionary<string, Gradient> temporalShaderGradients = new(); 
        Dictionary<string, Gradient> originalShaderGradients = new();
        
        originalShaderGradients.Clear();
        foreach (SpellCastCharge spellCastCharge in Catalog.GetDataList<SpellCastCharge>().Where(s => s.allowSkill && s.showInTree && s.primarySkillTreeId != "Test"))
        {
            if (spellCastCharge.imbueBladeEffectData != null)
            {
                foreach (EffectModule effectModule in spellCastCharge.imbueBladeEffectData.modules)
                    if (effectModule is EffectModuleShader effectModuleShader)
                    {
                        Color colorA = Common.IsWindows ? effectModuleShader.mainColorStart : effectModuleShader.mainNoHdrColorStart;
                        Color colorB = Common.IsWindows ? effectModuleShader.mainColorEnd : effectModuleShader.mainNoHdrColorEnd;
                        
                        Gradient gradient = new Gradient();
                        gradient.SetKeys(new []
                        {
                            new GradientColorKey(colorA, 0.0f),
                            new GradientColorKey(colorB, 1.0f),
                        }, new []
                        {
                            new GradientAlphaKey(1.0f, 0.0f),
                            new GradientAlphaKey(1.0f, 1.0f),
                        });
                        originalShaderGradients.Add(spellCastCharge.id, gradient);
                        break;
                    }
            }
        }

        foreach (string spellId in originalShaderGradients.Keys)
        {
            Color temporalColor = Dye.GetEvaluatedColor(spellId, "Mind");
            Gradient temporalGradient = new Gradient();
            temporalGradient.SetKeys(new[]
            {
                new GradientColorKey(temporalColor, 0.0f),
                new GradientColorKey(temporalColor, 1.0f)
            }, new []
            {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            });
            temporalShaderGradients.Add(spellId, temporalGradient);
        }

        foreach (string spellId in originalShaderGradients.Keys)
        {
            List<Gradient> frameGradients = new();
            int totalFrames = 30;
            for (int i = 0; i < totalFrames; i++)
            {
                float elapsed = (float)i / (totalFrames - 1);
                Gradient frameGradient = new Gradient();

                Color startColor = Color.Lerp(originalShaderGradients[spellId].colorKeys[0].color, temporalShaderGradients[spellId].colorKeys[0].color, elapsed);
                Color endColor = Color.Lerp(originalShaderGradients[spellId].colorKeys[1].color, temporalShaderGradients[spellId].colorKeys[1].color, elapsed);

                frameGradient.SetKeys(new[]
                {
                    new GradientColorKey(startColor, 0.0f),
                    new GradientColorKey(endColor, 1.0f)
                }, new[]
                {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                });

                frameGradients.Add(frameGradient);
                yield return Yielders.EndOfFrame;
            }

            cachedTemporalGradients.Add(spellId, frameGradients.ToArray());
            yield return Yielders.EndOfFrame;
        }
    }

    public override void OnSlowMotionEnter(SpellPowerSlowTime spellPowerSlowTime, float scale)
    {
        base.OnSlowMotionEnter(spellPowerSlowTime, scale);

        foreach (ThunderRoad.Imbue imbue in activeImbues)
            TryToggleImbue(imbue, true);
    }

    private void OnImbueHitEvent(SpellCastCharge spellData, float amount, bool fired, CollisionInstance collisionInstance, EventTime eventTime)
    {
        if (collisionInstance.targetColliderGroup?.collisionHandler?.Entity is Creature thunderEntity && eventTime == EventTime.OnEnd && collisionInstance.impactVelocity.magnitude > 7.5f)
        {
            thunderEntity.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(thunderEntity.GetCurrentCrystallisationId(), "Mind"), "Mind"));
            thunderEntity.Inflict("Slowed", this, collisionInstance.impactVelocity.magnitude * 0.5f, parameter: Mathf.Lerp(0.75f, 0, collisionInstance.impactVelocity.magnitude / 30));
        }
    }

    public override void OnSlowMotionExit(SpellPowerSlowTime spellPowerSlowTime)
    {
        base.OnSlowMotionExit(spellPowerSlowTime);
        foreach (ThunderRoad.Imbue imbue in activeImbues)
            TryToggleImbue(imbue, false);
    }

    public void TryToggleImbue(ThunderRoad.Imbue imbue, bool active)
    {
        if (imbue == null || imbue.spellCastBase == null) return;

        bool hasActiveCrystalImbue = false;
        foreach (ImbueBehaviour imbueBehaviour in imbue.GetComponents<ImbueBehaviour>())
            if (imbueBehaviour.enabled) hasActiveCrystalImbue = true;
        if (!hasActiveCrystalImbue) return;
        
        if (imbue.spellCastBase.imbueEffect != null)
            foreach (Effect effect in imbue.spellCastBase.imbueEffect.effects)
                if (effect is EffectShader effectShader)
                {
                    imbue.StartCoroutine(ShiftImbueColourCoroutine(imbue.spellCastBase.id, active, imbue, imbue.spellCastBase.imbueEffect, effectShader));
                    switch (active)
                    {
                        case true:
                            imbue.OnImbueHit -= OnImbueHitEvent;
                            imbue.OnImbueHit += OnImbueHitEvent;
                            break;
                        case false:
                            imbue.OnImbueHit -= OnImbueHitEvent;
                            break;
                    }
                }
    }

    public IEnumerator ShiftImbueColourCoroutine(string spellId, bool active, ThunderRoad.Imbue imbue, EffectInstance effectInstance, EffectShader effectShader)
    {
        Gradient[] frameGradients = cachedTemporalGradients[spellId];
        int totalFrames = frameGradients.Length;
        
        foreach (ImbueBehaviour imbueBehaviour in imbue.gameObject.GetComponents<ImbueBehaviour>())
            if (imbueBehaviour.enabled && imbueBehaviour.crystalImbueSkillData.spellId == spellId)
            {
                Color color = active ? Dye.GetEvaluatedColor(spellId, "Mind") : imbueBehaviour.crystalImbueSkillData.colorModifier;
                imbueBehaviour.imbueEffect.SetColor(color);
            }
        
        effectInstance.SetColor(active ? Dye.GetEvaluatedColor(spellId, "Mind") : Color.white);

        for (int i = 0; i < totalFrames; i++)
        {
            int currentFrame = active ? i : totalFrames - 1 - i;
            currentFrame = Mathf.Clamp(currentFrame, 0, totalFrames - 1);
            Gradient frameGradient = frameGradients[currentFrame];
            effectShader.SetMainGradient(frameGradient);
            yield return Yielders.EndOfFrame;
        }

        int finalFrame = active ? totalFrames - 1 : 0;
        effectShader.SetMainGradient(frameGradients[finalFrame]);
        yield return Yielders.EndOfFrame;
    }
    #endif
}