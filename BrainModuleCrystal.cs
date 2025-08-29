using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic;

public class BrainModuleCrystal : BrainData.Module
{
    public List<BoneEffectPair> boneEffectPairs = new();
    public List<EffectInstance> instances = new();
    public Coroutine crystalliseCoroutine;
    public EffectData endEffectData;
    public BoneEffectPair selected;
    public Lerper lerper;
    
    public bool allowBreakForce;
    public bool isCrystallised;
    
    public event OnCrystalliseStart onCrystalliseStart;
    public event OnCrystalliseStop onCrystalliseStop;
    
    public delegate void OnCrystalliseStart(Creature callback);

    public delegate void OnCrystalliseStop(Creature callback);

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

        endEffectData = Catalog.GetData<EffectData>("CrystallisationEnd");
        creature.OnDespawnEvent += OnDespawnEvent;
    }
    
    public void EnableBreakForce() => allowBreakForce = true;

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

    public void SetColor(Color target, string spellId, float time = 1f)
    {
        var particleSystems = instances.GetParticleSystems();
        lerper.SetColor(target, particleSystems, spellId, time);
    }

    public void SetEffects(bool active)
    {
        if (active && selected != null && selected.boneDataPairs != null && selected.boneDataPairs.Count > 0)
        {
            foreach (var pair in selected.boneDataPairs)
                if (pair.Value != null && !string.IsNullOrEmpty(pair.Key))
                {
                    var part = creature?.ragdoll?.GetPartByName(pair.Key);
                    if (part == null) continue;
                    var instance = pair.Value.Spawn(part.transform.position, Quaternion.LookRotation(part.upDirection, part.forwardDirection), part.transform);
                    instance?.Play();
                    instances.Add(instance);
                }
        }
        else
            for (var i = instances.Count - 1; i >= 0; i--)
            {
                instances[i]?.End();
                instances.RemoveAt(i);
            }
    }

    public void InvokeCrystallise(bool active)
    {
        if (active) onCrystalliseStart?.Invoke(creature);
        else onCrystalliseStop?.Invoke(creature);
    }

    public void Crystallise()
    {
        if (crystalliseCoroutine != null || isCrystallised) return;
        crystalliseCoroutine = creature.StartCoroutine(CrystalliseRoutine(creature));
        IEnumerator CrystalliseRoutine(Creature creature)
        {
            isCrystallised = true;
            SetEffects(true);
            yield return Yielders.ForSeconds(0.25f);
            InvokeCrystallise(true);
            creature.locomotion.allowMove = false;
            creature.locomotion.allowTurn = false;
            creature.currentLocomotion.globalMoveSpeedMultiplier.Add(this, 0);
            creature.mana.chargeSpeedMult.Add(this, 0.1f);
            if (creature.isPlayer) creature.currentLocomotion.globalMoveSpeedMultiplier.Add(this, 0);
            creature.SetDamageMultiplier(this, 1.5f);
            if (!creature.isPlayer)
            {
                creature.brain.Stop();
                if (SkillDiscombobulate.CreatureStunned(creature)) SkillDiscombobulate.BrainToggle(creature, true);
                creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                creature.ragdoll.SetState(Ragdoll.State.Frozen);
                if (allowBreakForce) creature.ragdoll.EnableCharJointBreakForce(2.5f);
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
            crystalliseCoroutine = null;
        }
    }

    public void StopCrystallise()
    {
        if (creature)
        { 
            if (creature.isPlayer) creature.currentLocomotion.globalMoveSpeedMultiplier.Remove(this);
            creature.locomotion.allowMove = true;
            creature.locomotion.allowTurn = true;
            creature.currentLocomotion.globalMoveSpeedMultiplier.Remove(this);
            creature.mana.chargeSpeedMult.Remove(this);
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
            InvokeCrystallise(false);
        }
    }
    
    [Serializable]
    public class BoneEffectPair
    {
        public List<string> creatureIds = new(new []{"HumanMale, HumanFemale"});
        public Dictionary<string, EffectData> boneDataPairs = new();

        public Dictionary<string, string> boneEffectPairs = new()
        {
            { "LeftArm", "CrystallisationUpperArm" },
            { "RightArm", "CrystallisationUpperArm" },
            { "LeftForeArm", "CrystallisationLowerArm" },
            { "RightForeArm", "CrystallisationLowerArm" },
            { "LeftUpLeg", "CrystallisationUpperLeg" },
            { "RightUpLeg", "CrystallisationUpperLeg" },
            { "LeftLeg", "CrystallisationLowerLeg" },
            { "RightLeg", "CrystallisationLowerLeg" },
            { "Spine1", "CrystallisationTorso" }
        };

        public Dictionary<string, EffectData> Load(Creature creature)
        {
            foreach (var stringValues in boneEffectPairs)
                if (!string.IsNullOrEmpty(stringValues.Key))
                    boneDataPairs.Add(stringValues.Key, Catalog.GetData<EffectData>(stringValues.Value));
            return boneDataPairs;
        }
    }
}