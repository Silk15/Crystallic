using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine.XR;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic;

public class BrainModuleCrystal : BrainData.Module
{
    [ModOption("Allow Crystallisation", "Controls whether any entity can be crystallised, by any source.", order = 0), ModOptionCategory("Crystallisation", -1)]
    public static bool allowCrystallisation = true;
    
    [ModOption("Allow Player Crystallisation", "Controls whether the player can be crystallised, by any source.", order = 1), ModOptionCategory("Crystallisation", -1)]
    public static bool allowPlayerCrystallisation = true;
    
    [ModOption("Max Crystallisation Particles", "Controls the maximum amount of particles the crystallisation Vfx can display at one time.", order = 3), ModOptionSlider, ModOptionCategory("Crystallisation", -1), ModOptionIntValues(10, 50, 1)]
    public static int maxParticles = 35;
    
    public List<BoneEffectPair> boneEffectPairs = new();
    public List<EffectInstance> instances = new();
    public Coroutine crystalliseCoroutine;
    public BoneEffectPair currentBoneMap;
    public EffectData endEffectData;
    public Lerper lerper;
    
    public bool allowBreakForce;
    public bool isCrystallised;
    
    public event OnCrystalliseStart onCrystalliseStart;
    public event OnCrystalliseStop onCrystalliseStop;
    public event Action onParticleCountUpdated;

    public override void Load(Creature creature)
    {
        base.Load(creature);
        lerper = new Lerper();
        foreach (var creatureBones in boneEffectPairs)
            if (creatureBones.creatureIds.Contains(creature.creatureId))
            {
                currentBoneMap = creatureBones;
                currentBoneMap.Load(creature);
            }
        endEffectData = Catalog.GetData<EffectData>("CrystallisationEnd");
        creature.OnDespawnEvent += OnDespawnEvent;
    }
    
    private void OnDespawnEvent(EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd) return;
        SetColor(Color.white, "Crystallic", 0.05f);
        creature.currentLocomotion.globalMoveSpeedMultiplier.Remove(this);
        creature.locomotion.allowMove = true;
        creature.locomotion.allowTurn = true;
        creature.RemoveDamageMultiplier(this);
        creature.ragdoll.DisableCharJointBreakForce();
        creature.OnDespawnEvent -= OnDespawnEvent;
    }

    public void SetColor(Color target, string spellId, float time = 1f)
    {
        var particleSystems = instances.GetParticleSystems();
        lerper.SetColor(target, particleSystems, spellId, time);
    }

    public void SetEffects(bool active)
    {
        if (active && currentBoneMap != null && currentBoneMap.boneDataPairs != null && currentBoneMap.boneDataPairs.Count > 0)
        {
            foreach (var pair in currentBoneMap.boneDataPairs)
                if (pair.Value != null && !string.IsNullOrEmpty(pair.Key))
                {
                    var part = creature?.ragdoll?.GetPartByName(pair.Key);
                    if (part == null) continue;
                    var instance = pair.Value.Spawn(part.transform.position, Quaternion.LookRotation(part.upDirection, part.forwardDirection), part.transform);
                    instance?.Play();
                    instances.Add(instance);
                }
            SetMaxParticles(maxParticles);
        }
        else
            for (var i = instances.Count - 1; i >= 0; i--)
            {
                instances[i]?.End();
                instances.RemoveAt(i);
            }
    }

    public void SetMaxParticles(int max)
    {
        foreach (ParticleSystem particleSystem in instances.GetParticleSystems())
        {
            var main = particleSystem.main;
            main.maxParticles = max;
        }
    }

    public void StartCrystallise()
    {
        if (crystalliseCoroutine != null || isCrystallised || !allowCrystallisation || (creature.isPlayer && !allowPlayerCrystallisation)) return;
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
            
            if (creature.isPlayer)
                creature.currentLocomotion.globalMoveSpeedMultiplier.Add(this, 0);
            
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
            if (creature.isPlayer)
                creature.currentLocomotion.globalMoveSpeedMultiplier.Remove(this);
            
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
    
    public void InvokeCrystallise(bool active)
    {
        if (active) onCrystalliseStart?.Invoke(creature);
        else onCrystalliseStop?.Invoke(creature);
    }
    
    public delegate void OnCrystalliseStart(Creature creature);
    public delegate void OnCrystalliseStop(Creature creature);

    [Serializable]
    public class BoneEffectPair
    {
        public List<string> creatureIds = new();
        public Dictionary<string, EffectData> boneDataPairs = new();
        public Dictionary<string, string> boneEffectPairs = new();

        public Dictionary<string, EffectData> Load(Creature creature)
        {
            foreach (var stringValues in boneEffectPairs)
                if (!string.IsNullOrEmpty(stringValues.Key))
                    boneDataPairs.Add(stringValues.Key, Catalog.GetData<EffectData>(stringValues.Value));
            return boneDataPairs;
        }
    }
}