using System;
using System.Collections.Generic;
using System.Linq;
using Crystallic.Skill.Spell;
using ThunderRoad;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crystallic.Skill
{
    public class SkillCrystallineDarts : SkillData
    {
        #if !SDK
        [ModOption("Targeting Angle", "Controls the angle of aim assist when you throw a shard, if an enemy is in this radius the shard's velocity move towards it slightly."), ModOptionFloatValues(0f, 360f, 1f), ModOptionSlider, ModOptionCategory("Crystalline Darts", 11)]
        public static float targetingAngle = 20f;

        [ModOption("Targeting Max Distance", "Controls the max distance of aim assist."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Crystalline Darts", 11)]
        public static float targetingDistance = 5f;

        [NonSerialized]
        public Dictionary<ArcPointsManager.PointData, Handle> handles = new();
        #endif

        [NonSerialized]
        public SkillCrystalReservoir skillCrystalReservoir;

        [NonSerialized]
        public SpellCastCrystallic spellCastCrystallic;

        #if !SDK
        public override void OnLateSkillsLoaded(SkillData skillData, Creature creature)
        {
            base.OnLateSkillsLoaded(skillData, creature);

            if (creature.TryGetSkill("CrystalReservoir", out skillCrystalReservoir))
            {
                skillCrystalReservoir.onShardAdd += OnShardAdd;
                skillCrystalReservoir.onShardRemove += OnShardRemove;
            }

            spellCastCrystallic = Catalog.GetData<SpellCastCrystallic>("Crystallic");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);
            if (skillCrystalReservoir == null) return;
            skillCrystalReservoir.onShardAdd -= OnShardAdd;
            skillCrystalReservoir.onShardRemove -= OnShardRemove;
        }

        private void OnShardAdd(ArcPointsManager pointsManager, ArcPointsManager.PointData point)
        {
            if (!handles.ContainsKey(point))
            {
                CapsuleCollider collider = point.transform.GetOrAddComponent<CapsuleCollider>();
                Rigidbody rigidbody = point.transform.GetOrAddComponent<Rigidbody>();
                Handle handle = point.transform.GetOrAddComponent<Handle>();
                collider.isTrigger = true;
                collider.height = 0.1f;
                collider.radius = 0.05f;
                rigidbody.centerOfMass = Vector3.up * -0.05f;
                rigidbody.drag = 0;
                rigidbody.mass = 0.1f;
                rigidbody.angularDrag = 0;
                rigidbody.ResetInertiaTensor();
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                handle.handOverlapColliders = new();
                handle.Load(Catalog.GetData<InteractableData>("ObjectHandleLight"));
                handle.data.localizationId = "";
                handle.data.highlightDefaultTitle = "Shard";
                handle.jointModifiers.Add(new Handle.JointModifier(this, 3, 1, 3, 1));
                Side side = skillCrystalReservoir.wristHolders.FirstOrDefault(k => k.Value == pointsManager).Key;
                handle.allowedHandSide = side == Side.Left ? Interactable.HandSide.Right : Interactable.HandSide.Left;
                handle.SetTelekinesis(false);
                handle.touchRadius = 0.075f;
                handle.reach = 0.075f;
                handle.Grabbed += OnGrabbed;
                handle.UnGrabbed += OnUngrabbed;
                handles.Add(point, handle);
            }
        }

        private void OnGrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            if (handles.TryGetKey(handle, out var point))
                point.allowMove = false;
            handle.physicBody.rigidBody.isKinematic = false;

            GameObject effectParent = new GameObject();
            effectParent.transform.SetParent(handle.transform);
            effectParent.transform.localPosition = Vector3.up * 0.075f;
            effectParent.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            ragdollHand.HapticTick();
            skillCrystalReservoir.pointEffects[point].SetParent(effectParent.transform, true);
        }

        private void OnUngrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            if (handles.TryGetKey(handle, out var point))
                point.allowMove = true;
            handle.physicBody.rigidBody.isKinematic = true;
            EffectInstance effect = skillCrystalReservoir.pointEffects[point];
            GameObject parent = effect.effects[0].transform.parent.gameObject;
            effect.SetParent(handle.transform, true);
            Object.Destroy(parent);

            ragdollHand.HapticTick();
            Vector3 velocity = Player.local.transform.rotation * PlayerControl.GetHand(ragdollHand.side).GetHandVelocity();
            Vector3 position = ragdollHand.caster.magicSource.position + velocity.normalized * 0.25f;

            ThunderEntity thunderEntity = Creature.AimAssist(position, velocity, targetingDistance, targetingAngle, out Transform targetPoint, Filter.EnemyOf(ragdollHand.ragdoll.creature), CreatureType.Golem | CreatureType.Human);
            if (thunderEntity != null) velocity = (targetPoint.position + (thunderEntity is Creature creature ? creature.locomotion.moveDirection : Vector3.zero) * 0.5f - position).normalized * velocity.magnitude;

            if (velocity.magnitude > SpellCaster.throwMinHandVelocity)
                spellCastCrystallic.FireShard(spellCastCrystallic.shardEffectData, position, velocity * 2.5f, spellCastCrystallic.ShardLifetime, 1.0f, shard =>
                {
                    ragdollHand.HapticTick();
                    skillCrystalReservoir.wristHolders[ragdollHand.otherHand.side].RemovePoint(point);
                    ragdollHand.PlayHapticClipOver(spellCastCrystallic.pulseCurve, 0.15f);
                    EffectInstance pulse = spellCastCrystallic.pulseEffectData.Spawn(position, Quaternion.LookRotation(velocity));
                    pulse.Play();
                    pulse.SetSize(0.5f);
                }, ignoredRagdoll: ragdollHand.ragdoll);
        }

        private void OnShardRemove(ArcPointsManager pointsManager, ArcPointsManager.PointData point)
        {
            if (handles.TryGetValue(point, out Handle handle))
            {
                handles.Remove(point);
                handle.Release();
                handle.ReleaseAllTkHandlers();
                Object.Destroy(handle);
            }
        }
        #endif
    }
}