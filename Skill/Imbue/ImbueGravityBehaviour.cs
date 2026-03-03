using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using TriInspector;
using UnityEngine;

namespace Crystallic.Skill.Imbue
{
    public class ImbueGravityBehaviour : ImbueBehaviour
    {
        #if !SDK
        [ModOption("Lithohammer Spring", "The spring applied to the tether connecting two physicBodies, this is the value that decides how tightly two limbs are bound.", order = 0), ModOptionCategory("Lithohammer", 6), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
        public static float spring = 550f;

        [ModOption("Lithohammer Damper", "The damping applied to the tether connecting two physicBodies, this acts as a smoother, damping out movement to act floaty.", order = 1), ModOptionCategory("Lithohammer", 6), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
        public static float damper = 30f;

        [ModOption("Min Lithohammer Distance", "The min distance two physicBodies can be from one another.", order = 2), ModOptionCategory("Lithohammer", 6), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
        public static float minDistance = 1f;

        [ModOption("Max Lithohammer Distance", "The max distance two physicBodies can be from one another.", order = 3), ModOptionCategory("Lithohammer", 6), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
        public static float maxDistance = 15f;

        [ModOption("Lithohammer Lifetime", "The lifetime of each tether."), ModOptionCategory("Lithohammer", 5), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
        public static float lifetime = 3f;
        #endif

        [NonSerialized]
        public EffectData tetherEffectData;

        [Dropdown(nameof(GetAllEffectID))]
        public string tetherEffectId = "GravityTether";

        [NonSerialized]
        public EffectData snapEffectData;

        [Dropdown(nameof(GetAllEffectID))]
        public string snapEffectId = "GravitySnap";

        [NonSerialized]
        public Dictionary<Creature, JointEffect> jointedBodies = new();

        [NonSerialized]
        public SpellCastGravity spellCastGravity;

        [NonSerialized]
        public StatusData statusData;

        #if !SDK
        public override void Load(CrystalImbueSkillData handler, ThunderRoad.Imbue imbue)
        {
            base.Load(handler, imbue);
            EventManager.onCreatureDespawn += OnCreatureDespawn;
            tetherEffectData = Catalog.GetData<EffectData>(tetherEffectId);
            snapEffectData = Catalog.GetData<EffectData>(snapEffectId);
            statusData = Catalog.GetData<StatusData>("Floating");
            spellCastGravity = Catalog.GetData<SpellCastGravity>("Gravity");
        }

        private void OnCreatureDespawn(Creature creature, EventTime eventTime)
        {
            if (jointedBodies.ContainsKey(creature))
            {
                Destroy(jointedBodies[creature].configurableJoint);
                foreach (EffectInstance effectInstance in jointedBodies[creature].effectInstances) effectInstance.End();
                jointedBodies.Remove(creature);
            }
        }

        public override void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge)
        {
            var item = collisionInstance?.sourceColliderGroup?.collisionHandler?.item;
            var part = collisionInstance?.targetColliderGroup?.collisionHandler?.ragdollPart;

            if (part && item && !part.ragdoll.creature.isPlayer && collisionInstance.impactVelocity.sqrMagnitude > 18f * 18f / (Common.IsAndroid ? 1.5f : 1f))
                TryCreateJoint(collisionInstance, item, part);
        }

        public IEnumerator JointExpirationRoutine(Item source, RagdollPart ragdollPart)
        {
            var jointEffect = jointedBodies[ragdollPart.ragdoll.creature];
            yield return Yielders.ForSeconds(1);
            float startTime = Time.time;
            bool velocityMet = false;
            while (Time.time - startTime < 2f)
            {
                if (source.physicBody.velocity.sqrMagnitude < 12.5f * 12.5f / (Common.IsAndroid ? 1.5f : 1f))
                {
                    velocityMet = true;
                    break;
                }

                yield return null;
            }

            if (velocityMet)
            {
                var currentPart = ragdollPart;

                while (currentPart != null)
                {
                    if (currentPart.sliceAllowed)
                    {
                        currentPart?.TrySlice();
                        currentPart.ragdoll.creature.Kill();
                        yield return Yielders.ForSeconds(lifetime);
                        break;
                    }

                    currentPart = currentPart.parentPart;
                    yield return null;
                }
            }

            if (jointEffect.configurableJoint != null)
            {
                snapEffectData.Spawn(jointEffect.configurableJoint.transform).Play();
                ragdollPart.ragdoll.creature.Remove(statusData, this);
                spellCastGravity.readyEffectData.Spawn(jointEffect.configurableJoint.transform).Play();
                jointEffect.configurableJoint.breakForce = 0f;
                Destroy(jointEffect.configurableJoint);
            }

            foreach (EffectInstance effectInstance in jointEffect.effectInstances) effectInstance.End();
            jointedBodies.Remove(ragdollPart.ragdoll.creature);
        }

        public void TryCreateJoint(CollisionInstance collisionInstance, Item source, RagdollPart target)
        {
            if (jointedBodies.ContainsKey(target.ragdoll.creature)) return;
            var effectInstance = tetherEffectData.Spawn(collisionInstance.sourceCollider.transform);
            effectInstance.SetSource(collisionInstance.sourceCollider.transform);
            effectInstance.SetTarget(target.transform);
            effectInstance.Play();
            var joint = Utils.CreateConfigurableJoint(source.physicBody.rigidBody, target?.physicBody.rigidBody, spring, damper, minDistance, maxDistance, 0.35f);
            jointedBodies.Add(target.ragdoll.creature, new JointEffect(joint));
            jointedBodies[target.ragdoll.creature].effectInstances.Add(effectInstance);
            target.ragdoll.creature.Inflict(statusData, this, parameter: new FloatingParams(0f, 0.1f, 1f, true));
            StartCoroutine(JointExpirationRoutine(source, target));
        }

        public override void Unload(ThunderRoad.Imbue imbue)
        {
            base.Unload(imbue);

            foreach (var kvp in jointedBodies.ToList())
            {
                var creature = kvp.Key;
                var jointEffect = kvp.Value;
                if (jointEffect != null && !jointEffect.effectInstances.IsNullOrEmpty())
                    foreach (EffectInstance effectInstance in jointEffect.effectInstances)
                        effectInstance.End();
                if (jointEffect?.configurableJoint != null)
                {
                    creature.Remove(statusData, this);
                    jointEffect.configurableJoint.breakForce = 0f;
                    Destroy(jointEffect.configurableJoint);
                }

                jointedBodies.Remove(creature);
            }
        }
        #endif
    }
}