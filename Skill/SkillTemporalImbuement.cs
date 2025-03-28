using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill.SpellPower;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillTemporalImbuement : SkillSlowTimeData
{
    public Dictionary<string, Gradient> defaults = new();
    public List<SkillSpellPair> skillSpellPairs = new();
    public Color startColor;
    public List<Imbue> imbuesActive = new();

    public override void OnImbueLoad(SpellData spell, Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        var spellCastCharge = imbue.spellCastBase;
        if (!defaults.ContainsKey(spellCastCharge.id))
        {
            var effectShader = spellCastCharge.imbueEffect.effects.First(e => e is EffectShader);
            var defaultMainGradient = effectShader?.GetType()?.GetField("currentMainGradient", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(effectShader) as Gradient;
            defaults.Add(spellCastCharge.id, defaultMainGradient);
            if (Settings.debug) Debug.Log($"Saving default gradient for spell {spellCastCharge.id}. gradient is: \n Color keys: \n {string.Join(" \n - ", defaultMainGradient.colorKeys.Select(key => key.color.ToString()).ToList())} \n Alpha keys: \n {string.Join(" \n - ", defaultMainGradient.alphaKeys.ToList())}");
        }
        if (!imbuesActive.Contains(imbue))
        {
            bool flag = false;
            for (int i = 0; i < skillSpellPairs.Count; i++)
                if (skillSpellPairs[i].IsValid(imbue.imbueCreature, spell)) flag = true;
            if (flag) imbuesActive.Add(imbue);
        }
    }

    public override void OnImbueUnload(SpellData spell, Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        if (imbuesActive.Contains(imbue)) imbuesActive.Remove(imbue);
    }

    public override void OnImbueHit(SpellCastCharge spellData, float amount, bool fired, CollisionInstance hit, EventTime eventTime)
    {
        base.OnImbueHit(spellData, amount, fired, hit, eventTime);
        if (eventTime == EventTime.OnStart && timeSlowed)
        {
            var item = hit?.sourceColliderGroup?.collisionHandler?.item;
            for (int i = 0; i < item.imbues.Count; i++)
            {
                var imbue = item.imbues[i];
                if (imbue.spellCastBase == spellData && imbuesActive.Contains(imbue))
                {
                    var hitCreature = hit?.targetColliderGroup?.collisionHandler?.Entity as Creature;
                    if (!hitCreature) continue;
                    hitCreature.Inflict("Slowed", this, 5);
                    hitCreature.brain.instance.GetModule<BrainModuleCrystal>().SetColor(Dye.GetEvaluatedColor("Mind", imbue.spellCastBase.id), imbue.spellCastBase.id);
                }
            }
        }
    }

    public override void OnSlowMotionEnter(SpellPowerSlowTime spellPowerSlowTime, float scale)
    {
        base.OnSlowMotionEnter(spellPowerSlowTime, scale);
        Imbue active = null;
        try
        {
            for (int i = 0; i < imbuesActive.Count; i++)
            {
                active = imbuesActive[i];
                Color color = Dye.GetEvaluatedColor("Mind", imbuesActive[i].spellCastBase.id);
                var behaviour = imbuesActive[i].gameObject.GetComponent<ImbueBehavior>();
                if (behaviour != null)
                {
                    behaviour.imbueEffectInstance.SetColorImmediate(color);
                    behaviour.SetColorModifier(color);
                }
                if (Settings.debug) Debug.Log($"Dying imbue: {imbuesActive[i].spellCastBase.id} to endColor: {color}");
                SetShaderColor(imbuesActive[i].spellCastBase, ThunderRoad.Utils.CreateGradient(startColor, color), 0.5f);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception caught while attempting to modify shader gradient of: {active?.spellCastBase?.id}! Exception: {e}");
        }
    }

    public override void OnSlowMotionExit(SpellPowerSlowTime spellPowerSlowTime)
    {
        base.OnSlowMotionExit(spellPowerSlowTime);
        Imbue active = null;
        try
        {
            for (int i = 0; i < imbuesActive.Count; i++)
            {
                active = imbuesActive[i];
                var behaviour = imbuesActive[i].gameObject.GetComponent<ImbueBehavior>();
                if (behaviour != null)
                {
                    behaviour.imbueEffectInstance.SetColorImmediate(behaviour.handler.colorModifier);
                    behaviour.ClearColorModifier();
                }
                var defaultMainGradient = defaults[imbuesActive[i].spellCastBase.id];
                if (Settings.debug) Debug.Log($"Clearing effectShader gradient for imbue: {imbuesActive[i].spellCastBase.id}. Default gradient is: \n Color keys: \n {string.Join(" \n - ", defaultMainGradient.colorKeys.Select(key => key.color.ToString()).ToList())} \n Alpha keys: \n {string.Join(" \n - ", defaultMainGradient.alphaKeys.ToList())}");
                SetShaderColor(imbuesActive[i].spellCastBase, defaultMainGradient, 0.5f);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception caught while attempting to modify shader gradient of: {active?.spellCastBase?.id}! Exception: {e}");
        }
    }

    public void SetShaderColor(SpellCastCharge spellCastCharge, Gradient gradient, float time) => spellCastCharge.spellCaster.StartCoroutine(LerpShaderColor(spellCastCharge, gradient, time));

    public IEnumerator LerpShaderColor(SpellCastCharge spellCastCharge, Gradient gradient, float time)
    {
        var effectShader = spellCastCharge.imbueEffect.effects.First(e => e is EffectShader);
        var mainGradient = effectShader?.GetType()?.GetField("currentMainGradient", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(effectShader) as Gradient;
        int keyCount = Mathf.Min(mainGradient.colorKeys.Length, gradient.colorKeys.Length);
        float timeElapsed = 0f;
        while (timeElapsed < time)
        {
            timeElapsed += Time.deltaTime;
            GradientColorKey[] blendedColorKeys = new GradientColorKey[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                Color startColor = mainGradient.colorKeys[i].color;
                Color endColor = gradient.colorKeys[i].color;
                blendedColorKeys[i] = new GradientColorKey(Color.Lerp(startColor, endColor, spellCastCharge.imbue.energy / 100), Mathf.Lerp(mainGradient.colorKeys[i].time, gradient.colorKeys[i].time, spellCastCharge.imbue.energy / 100));
            }
            Gradient blendedGradient = new Gradient();
            blendedGradient.colorKeys = blendedColorKeys;
            blendedGradient.alphaKeys = mainGradient.alphaKeys;
            effectShader.SetMainGradient(blendedGradient);
            spellCastCharge.imbueEffect.SetIntensity(spellCastCharge.imbue.energy);
            spellCastCharge.imbueEffect.SetColorImmediate(Color.Lerp(mainGradient.Evaluate(spellCastCharge.imbue.energy / 100), gradient.Evaluate(spellCastCharge.imbue.energy / 100), spellCastCharge.imbue.energy / 100));
            spellCastCharge.imbue.Transfer(spellCastCharge, spellCastCharge.imbue.energy);
            yield return null;
        }
        effectShader.SetMainGradient(gradient);
    }
}