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
    [ModOption("Allow Player Crystallisation", "Controls whether the player can be crystallised by any source.", order = 1, defaultValueIndex = 1), ModOptionCategory("Crystallisation", -1)]
    public static void TogglePlayerCrystallisation(bool active)
    {
        allowPlayerCrystallisation = active;
        if (!active && Player.currentCreature && Player.currentCreature.HasStatus("Crystallised")) 
            Player.currentCreature.Clear("Crystallised");
    }

    [ModOption("Speed Multiplier", "Controls how slowed you and other NPCs are while crystallised.\n\n- 0 = Locked in place\n- 1 = No slowing", defaultValueIndex = 0), ModOptionFloatValues(0, 1, 0.1f), ModOptionOrder(3), ModOptionSlider, ModOptionCategory("Crystallisation", -1)]
    public static void SetCrystallisationSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
        if (Player.currentCreature)
            foreach (Creature creature in CrystallisedCreatures)
            {
                BrainModuleCrystal brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                creature.currentLocomotion.globalMoveSpeedMultiplier.Remove(brainModuleCrystal);
                creature.currentLocomotion.globalMoveSpeedMultiplier.Add(brainModuleCrystal, speedMultiplier);
            }
    }

    [ModOption("Damage Multiplier", "Controls how much damage you and other NPCs take while crystallised.\n\n- 0 = No damage\n- 1 = Regular amount\n- 5 = 5x damage taken", defaultValueIndex = 15), ModOptionFloatValues(0, 5, 0.1f), ModOptionOrder(4), ModOptionSlider, ModOptionCategory("Crystallisation", -1)]
    public static void SetCrystallisationDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
        if (Player.currentCreature)
            foreach (Creature creature in CrystallisedCreatures)
            {
                BrainModuleCrystal brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                creature.RemoveDamageMultiplier(brainModuleCrystal);
                creature.SetDamageMultiplier(brainModuleCrystal, damageMultiplier);
            }
    }

    [ModOption(" Charge Speed Multiplier", "Controls how much you and other NPCs spell casting speed is slowed while crystallised.\n\n- 0 = Not able to cast\n- 1 = Regular speed\n- 5 = 5x as quick", defaultValueIndex = 1), ModOptionFloatValues(0, 5, 0.1f), ModOptionOrder(5), ModOptionSlider, ModOptionCategory("Crystallisation", -1)]
    public static void SetCrystallisationChargeSpeedMultiplier(float multiplier)
    {
        chargeMultiplier = multiplier;
        if (Player.currentCreature)
            foreach (Creature creature in CrystallisedCreatures)
            {
                BrainModuleCrystal brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                creature.mana?.chargeSpeedMult.Remove(brainModuleCrystal);
                creature.mana?.chargeSpeedMult.Add(brainModuleCrystal, chargeMultiplier);
            }
    }

    public static HashSet<Creature> CrystallisedCreatures { get; } = new();
    
    public static bool allowPlayerCrystallisation = true;
    public static int totalCrystallisedCreatures = 0;

    public static float damageMultiplier = 1.5f;
    public static float chargeMultiplier = 0.1f;
    public static float speedMultiplier = 0f;
    
    public List<BoneEffectPair> boneEffectPairs = new();
    public List<EffectInstance> instances = new();
    
    public Coroutine crystalliseCoroutine;
    public BoneEffectPair currentBoneMap;
    public EffectData endEffectData;
    public Lerper lerper;
    
    public bool allowBreakForce;
    public bool isCrystallised;
    
    public static event OnCrystallise onCreatureCrystallised;
    public event OnCrystallise onCrystallise;

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
            SetMaxParticles(CrystallisationPlatformController.maxParticles);
            if (Dye.rainbowMode)
                lerper.StartRainbow(instances.GetParticleSystems().ToArray());
        }
        else
        {
            lerper.StopRainbow();
            for (var i = instances.Count - 1; i >= 0; i--)
            {
                instances[i]?.End();
                instances.RemoveAt(i);
            }
            
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
        if (crystalliseCoroutine != null || isCrystallised || (creature.isPlayer && !allowPlayerCrystallisation) || totalCrystallisedCreatures >= CrystallisationPlatformController.maxCrystallisedEntities) return;
        crystalliseCoroutine = creature.StartCoroutine(CrystalliseRoutine(creature));
        IEnumerator CrystalliseRoutine(Creature creature)
        {
            if (!CrystallisedCreatures.Contains(creature)) 
                CrystallisedCreatures.Add(creature);
            
            totalCrystallisedCreatures++;
            isCrystallised = true;
            SetEffects(true);
            yield return Yielders.ForSeconds(0.25f);
            InvokeCrystallise(true);
            creature.locomotion.allowMove = false;
            creature.locomotion.allowTurn = false;
            creature.currentLocomotion.globalMoveSpeedMultiplier.Add(this, speedMultiplier);
            creature.mana?.chargeSpeedMult.Add(this, chargeMultiplier);
            
            creature.SetDamageMultiplier(this, damageMultiplier);
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
            if (CrystallisedCreatures.Contains(creature))
                CrystallisedCreatures.Remove(creature);
            
            creature.locomotion.allowMove = true;
            creature.locomotion.allowTurn = true;
            creature.currentLocomotion.globalMoveSpeedMultiplier.Remove(this);
            creature.mana?.chargeSpeedMult.Remove(this);
            creature.RemoveDamageMultiplier(this);
            if (!creature.isPlayer)
            {
                creature.brain.Load(creature.brain.instance.id);
                creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                creature.ragdoll.DisableCharJointBreakForce();
            }

            SetEffects(false);
            isCrystallised = false;
            totalCrystallisedCreatures--;
            var instance = endEffectData.Spawn(creature.ragdoll.targetPart.transform);
            instance.Play();
            instance.SetColor(lerper.currentColor);
            InvokeCrystallise(false);
        }
    }
    
    public void InvokeCrystallise(bool active)
    {
        onCrystallise?.Invoke(this, creature, active);
        onCreatureCrystallised?.Invoke(this, creature, active);
    }
    
    public delegate void OnCrystallise(BrainModuleCrystal brainModuleCrystal, Creature creature, bool active);

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