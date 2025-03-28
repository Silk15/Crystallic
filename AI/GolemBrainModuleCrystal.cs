using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.AI;

public class GolemBrainModuleCrystal : GolemBrainModule
{
    public bool isCrystallised;
    protected List<EffectInstance> instances = new();
    public Lerper lerper;
    public EffectData lowerArmLeftGolemData;
    public EffectData lowerArmRightGolemData;
    public EffectData lowerLegLeftGolemData;
    public EffectData lowerLegRightGolemData;
    public EffectData torsoGolemData;
    public EffectData upperArmLeftGolemData;
    public EffectData upperArmRightGolemData;
    public EffectData upperLegLeftGolemData;
    public EffectData upperLegRightGolemData;


    public override void Load(Golem golem)
    {
        base.Load(golem);
        lerper = new Lerper();
        lowerArmLeftGolemData = Catalog.GetData<EffectData>("LowerArmLeftGolem");
        lowerArmRightGolemData = Catalog.GetData<EffectData>("LowerArmRightGolem");
        upperArmLeftGolemData = Catalog.GetData<EffectData>("UpperArmLeftGolem");
        upperArmRightGolemData = Catalog.GetData<EffectData>("UpperArmRightGolem");
        torsoGolemData = Catalog.GetData<EffectData>("TorsoGolem");
        lowerLegLeftGolemData = Catalog.GetData<EffectData>("LowerLegLeftGolem");
        lowerLegRightGolemData = Catalog.GetData<EffectData>("LowerLegRightGolem");
        upperLegLeftGolemData = Catalog.GetData<EffectData>("UpperLegLeftGolem");
        upperLegRightGolemData = Catalog.GetData<EffectData>("UpperLegRightGolem");
    }

    public static IEnumerator AdjustAnimatorSpeed(bool active, Animator animator, int steps)
    {
        var stepValue = 1f / steps;
        var currentSpeed = animator.speed;

        for (var i = 0; i < steps; i++)
        {
            if (active) currentSpeed += stepValue;
            else currentSpeed -= stepValue;
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, 1f);
            animator.speed = currentSpeed;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public override void Unload(Golem golem)
    {
        base.Unload(golem);
        Golem.local.speed = 1;
        Golem.local.allowMelee = true;
        SetColor(Dye.GetEvaluatedColor(lerper.currentSpellId, "Crystallic"), "Crystallic");
        StartCoroutine(AdjustAnimatorSpeed(true, Golem.local.animator, 20));
        isCrystallised = false;
    }

    public void SetColor(Color target, string spellId, float time = 1)
    {
        var particleSystems = instances.GetParticleSystems();
        lerper.SetColor(target, particleSystems, spellId, time);
    }


    public void Crystallise(float duration, bool crystallise = true)
    {
        if (!isCrystallised) StartCoroutine(CrystalliseRoutine(duration, crystallise));
    }

    private IEnumerator CrystalliseRoutine(float duration, bool crystallise = true)
    {
        isCrystallised = true;
        instances.AddRange(Golem.local.Brain().PlayFullBodyEffect(5, upperArmLeftGolemData, upperArmRightGolemData, lowerArmLeftGolemData, lowerArmRightGolemData, torsoGolemData, upperLegLeftGolemData, upperLegRightGolemData, lowerLegLeftGolemData, lowerLegRightGolemData));
        if (crystallise) StartCoroutine(AdjustAnimatorSpeed(false, Golem.local.animator, 20));
        yield return new WaitForSeconds(0.25f);
        if (crystallise)
        {
            Golem.local.speed = 0;
            Golem.local.allowMelee = false;
        }

        yield return new WaitForSeconds(duration);
        Golem.local.speed = 1;
        Golem.local.allowMelee = true;
        SetColor(Dye.GetEvaluatedColor(lerper.currentSpellId, "Crystallic"), "Crystallic");
        if (crystallise) StartCoroutine(AdjustAnimatorSpeed(true, Golem.local.animator, 20));
        isCrystallised = false;
    }
}