using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.AI;

[Serializable]
public class BoneEffectPair
{
    public List<string> creatureIds;
    public Dictionary<string, EffectData> boneDataPairs = new();

    public Dictionary<string, string> boneEffectPairs = new()
    {
        { "LeftArm", "UpperArmCrystallic" },
        { "RightArm", "UpperArmCrystallic" },
        { "LeftForeArm", "LowerArmCrystallic" },
        { "RightForeArm", "LowerArmCrystallic" },
        { "LeftUpLeg", "UpperLegCrystallic" },
        { "RightUpLeg", "UpperLegCrystallic" },
        { "LeftLeg", "LowerLegCrystallic" },
        { "RightLeg", "LowerLegCrystallic" },
        { "Spine1", "TorsoCrystallic" }
    };

    public Dictionary<string, EffectData> Load(Creature creature)
    {
        foreach (var stringValues in boneEffectPairs)
            if (!string.IsNullOrEmpty(stringValues.Key))
                boneDataPairs.Add(stringValues.Key, Catalog.GetData<EffectData>(stringValues.Value));
        return boneDataPairs;
    }
}

public class BrainModuleCrystal : BrainData.Module
{
    public delegate void OnCrystalliseStart(Creature callback);

    public delegate void OnCrystalliseStop(Creature callback);

    [ModOption("Crystallisation Quality", "Controls the max particles active for crystallisation Vfx Per limb, higher values will cause severe performance issues on lower end machines."), ModOptionCategory("Crystallisation", 0), ModOptionSlider, ModOptionFloatValues(1, 100, 1)]
    public static float crystallisationParticleQuality = 40f;

    [ModOption("Crystallisation Break Force Multiplier", "Controls how easy it is to dismember enemies when crystallised, higher is more difficult."), ModOptionCategory("Crystallisation", 0), ModOptionSlider, ModOptionFloatValues(1, 50, 0.1f)]
    public static float breakForceMultiplier = 2.5f;

    [ModOption("Crystallisation Damage Multiplier", "Controls the damage multiplier applied to crystallised creatures. The higher this value is, the more damage they'll take."), ModOptionCategory("Crystallisation", 0), ModOptionSlider, ModOptionFloatValues(1, 50, 0.1f)]
    public static float creatureDamageMultiplier = 1.5f;

    protected static bool allowBreakForce;
    public List<BoneEffectPair> boneEffectPairs = new();
    public List<EffectInstance> instances = new();
    public bool isCrystallised;
    public Lerper lerper;
    public BoneEffectPair selected;
    public event OnCrystalliseStart onCrystalliseStart;
    public event OnCrystalliseStop onCrystalliseStop;
    public EffectData endEffectData;

    public override void Load(Creature creature)
    {
        base.Load(creature);
        lerper = new Lerper();
        foreach (var creatureBones in boneEffectPairs)
            if (creatureBones.creatureIds.Contains(creature.creatureId))
            {
                selected = creatureBones;
                selected.Load(creature);
            }

        endEffectData = Catalog.GetData<EffectData>("EndRagdollCrystallic");
        creature.OnDespawnEvent += OnDespawnEvent;
    }

    private void OnDespawnEvent(EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd) return;
        SetColor(Color.white, "Crystallic", 0.01f);
        creature.currentLocomotion.globalMoveSpeedMultiplier.Remove(this);
        creature.locomotion.allowMove = true;
        creature.locomotion.allowTurn = true;
        creature.RemoveDamageMultiplier(this);
        creature.ragdoll.DisableCharJointBreakForce();
    }

    public static void SetBreakForce(bool active)
    {
        allowBreakForce = active;
    }

    public bool Crystallise(float duration, string spellId = null)
    {
        if (!isCrystallised)
        {
            isCrystallised = true;
            creature.StartCoroutine(CrystalliseRoutine(duration));
            if (!string.IsNullOrEmpty(spellId)) SetColor(Dye.GetEvaluatedColor(lerper.currentSpellId, spellId), spellId);
        }

        return !isCrystallised;
    }

    public void SetColor(Color target, string spellId, float time = 1)
    {
        var particleSystems = instances.GetParticleSystems();
        lerper.SetColor(target, particleSystems, spellId, time);
    }


    public void SetEffects(bool active)
    {
        if (active)
        {
            foreach (var pair in selected.boneDataPairs)
                if (pair.Value != null && !string.IsNullOrEmpty(pair.Key))
                {
                    var part = creature?.ragdoll?.GetPartByName(pair.Key);
                    if (part != null)
                    {
                        var instance = pair.Value.Spawn(part.transform.position, Quaternion.LookRotation(part.upDirection, part.forwardDirection), part.transform);
                        instance?.Play();
                        instances.Add(instance);
                    }
                }

            instances.SetMaxParticles(Mathf.RoundToInt(crystallisationParticleQuality));
        }
        else
        {
            for (var i = instances.Count - 1; i >= 0; i--)
            {
                instances[i]?.End();
                instances.RemoveAt(i);
            }
        }
    }


    private IEnumerator CrystalliseRoutine(float duration)
    {
        SetEffects(true);
        yield return new WaitForSeconds(0.25f);
        creature.Inflict("LockMovement", this, duration);
        onCrystalliseStart?.Invoke(creature);
        creature.locomotion.allowMove = false;
        creature.locomotion.allowTurn = false;
        if (creature.isPlayer) creature.currentLocomotion.globalMoveSpeedMultiplier.Add(this, 0);
        creature.SetDamageMultiplier(this, creatureDamageMultiplier);
        if (!creature.isPlayer)
        {
            creature.brain.Stop();
            if (SkillDiscombobulate.CreatureStunned(creature)) SkillDiscombobulate.BrainToggle(creature, true);
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
            creature.ragdoll.SetState(Ragdoll.State.Frozen);
            if (allowBreakForce) creature.ragdoll.EnableCharJointBreakForce(breakForceMultiplier);
            var module1 = creature.brain.instance.GetModule<BrainModuleFear>();
            if (module1 != null && module1.isCowering) module1.StopPanic();
            if (GameManager.CheckContentActive(BuildSettings.ContentFlag.Fright))
                if (!creature.isKilled)
                {
                    var module2 = creature.brain.instance.GetModule<BrainModuleSpeak>(false);
                    if (module2 != null)
                    {
                        module2.StopSpeak();
                        module2.Play(BrainModuleSpeak.hashFalling, true);
                    }
                }
        }

        yield return new WaitForSeconds(duration);
        if (creature.isPlayer) creature.currentLocomotion.globalMoveSpeedMultiplier.Remove(this);
        creature.locomotion.allowMove = true;
        creature.locomotion.allowTurn = true;
        creature.RemoveDamageMultiplier(this);
        if (!creature.isPlayer)
        {
            creature.brain.Load(creature.brain.instance.id);
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
            creature.ragdoll.DisableCharJointBreakForce();
        }

        SetEffects(false);
        isCrystallised = false;
        var instance = endEffectData.Spawn(creature.ragdoll.targetPart.transform);
        instance.Play();
        instance.SetColorImmediate(lerper.currentColor);
        onCrystalliseStop?.Invoke(creature);
    }
}