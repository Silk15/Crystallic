using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using TriInspector;
using UnityEngine;
using System.Linq;
using ThunderRoad.Skill;

namespace Crystallic
{
    public class BrainModuleCrystal : BrainData.Module
    {
        #if !SDK
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
        #endif

        public static HashSet<Creature> CrystallisedCreatures { get; } = new();

        public static bool allowPlayerCrystallisation = true;
        public static int totalCrystallisedCreatures = 0;

        public static float damageMultiplier = 1.5f;
        public static float chargeMultiplier = 0.1f;
        public static float speedMultiplier = 0f;

        public List<BoneEffectPair> boneEffectPairs = new();

        #if !SDK
        [NonSerialized]
        public List<EffectInstance> effectInstances = new();
        #endif

        [NonSerialized]
        public Coroutine crystalliseCoroutine;

        [NonSerialized]
        public BoneEffectPair currentBoneMap;

        [NonSerialized]
        public EffectData endEffectData;

        #if !SDK
        [NonSerialized]
        public Lerper lerper;
        #endif

        [NonSerialized]
        public bool allowBreakForce;

        [NonSerialized]
        public bool isCrystallised;

        #if !SDK
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
            var particleSystems = effectInstances.GetParticleSystems();
            lerper.SetColor(target, particleSystems, spellId, time);
        }

        public void SetEffects(bool active)
        {
            if (active && currentBoneMap != null && currentBoneMap.boneDataPairs != null && currentBoneMap.boneDataPairs.Count > 0)
            {
                foreach (KeyValuePair<string, EffectData> pair in currentBoneMap.boneDataPairs)
                    if (pair.Value != null && !string.IsNullOrEmpty(pair.Key))
                    {
                        if (TrySpawnOnPart(pair.Value, creature?.ragdoll?.GetPartByName(pair.Key), out EffectInstance effectInstance))
                            effectInstances.Add(effectInstance);
                    }

                SetMaxParticles(CrystallisationPlatformController.maxParticles);
            }
            else
            {
                for (int i = effectInstances.Count - 1; i >= 0; i--)
                {
                    effectInstances[i]?.End();
                    effectInstances.RemoveAt(i);
                }

            }
        }
        
        public void SetMaxParticles(int max)
        {
            foreach (ParticleSystem particleSystem in effectInstances.GetParticleSystems())
            {
                var main = particleSystem.main;
                main.maxParticles = max;
            }
        }

        public bool TrySpawnOnPart(EffectData effectData, RagdollPart ragdollPart, out EffectInstance effectInstance, bool play = true)
        {
            if (effectData == null || ragdollPart == null)
            {
                effectInstance = null;
                return false;
            }
            effectInstance = effectData.Spawn(ragdollPart.transform.position, Quaternion.LookRotation(ragdollPart.upDirection, ragdollPart.forwardDirection), ragdollPart.transform);
            if (play) effectInstance.Play();
            return true;
        }

        public bool TryCrystallise()
        {
            if (crystalliseCoroutine != null || isCrystallised || (creature.isPlayer && !allowPlayerCrystallisation) || totalCrystallisedCreatures >= CrystallisationPlatformController.maxCrystallisedEntities)
                return false;
            
            StartCrystallise();
            return true;
        }

        public bool TryStopCrystallise()
        {
            if (!isCrystallised) 
                return false;
            
            StopCrystallise();
            return true;
        }

        protected void StartCrystallise()
        {
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

        protected void StopCrystallise()
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
        #endif

        [Serializable]
        public class BoneEffectPair
        {
            [DropdownList(nameof(GetAllCreatureID))]
            public List<string> creatureIds = new();

            public Dictionary<string, string> boneEffectPairs = new();

            [NonSerialized]
            public Dictionary<string, EffectData> boneDataPairs = new();

            public Dictionary<string, EffectData> Load(Creature creature)
            {
                foreach (var stringValues in boneEffectPairs)
                    if (!string.IsNullOrEmpty(stringValues.Key))
                        boneDataPairs.Add(stringValues.Key, Catalog.GetData<EffectData>(stringValues.Value));
                return boneDataPairs;
            }

            public TriDropdownList<string> GetAllCreatureID() => Catalog.GetDropdownAllID(Category.Creature);
        }
    }
}