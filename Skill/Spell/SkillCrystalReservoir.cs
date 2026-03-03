using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crystallic.Skill.Spell
{
    public class SkillCrystalReservoir : SpellSkillData
    {
        #if !SDK
        [ModOption("Max Reservoir Shards", "Controls the default maximum shards per wrist."), ModOptionIntValues(1, 15, 1), ModOptionSlider, ModOptionCategory("Crystal Reservoir", 9)]
        public static int maxShards = 5;

        [NonSerialized]
        public Dictionary<ArcPointsManager.PointData, EffectInstance> pointEffects = new();

        [NonSerialized]
        public Dictionary<Side, ArcPointsManager> wristHolders = new();
        #endif

        [NonSerialized]
        public SpellCastCrystallic spellCastCrystallic;

        #if !SDK
        public event ShardDelegate onShardAdd;
        public event ShardDelegate onShardRemove;

        public delegate void ShardDelegate(ArcPointsManager pointsManager, ArcPointsManager.PointData point);

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            spellCastCrystallic = Catalog.GetData<SpellCastCrystallic>("Crystallic");
            wristHolders.FillWithDefault();
        }

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);
            ToggleReservoir(creature, Side.Left, true);
            ToggleReservoir(creature, Side.Right, true);
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);
            ToggleReservoir(creature, Side.Left, false);
            ToggleReservoir(creature, Side.Right, false);
        }

        public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
        {
            base.OnSpellLoad(spell, caster);
            if (spell is not SpellCastCrystallic crystallic)
                return;

            crystallic.onButtonPressed += OnButtonPressedWhileCasting;
            crystallic.OnSpellUpdateEvent += OnSpellUpdateEvent;
            crystallic.onShardshotStart += OnShardshotStart;
            crystallic.OnSpellCastEvent += OnSpellCastEvent;
            crystallic.OnSpellStopEvent += OnSpellCastEvent;
        }

        public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
        {
            base.OnSpellUnload(spell, caster);
            if (spell is not SpellCastCrystallic crystallic)
                return;

            crystallic.onButtonPressed -= OnButtonPressedWhileCasting;
            crystallic.OnSpellUpdateEvent -= OnSpellUpdateEvent;
            crystallic.onShardshotStart -= OnShardshotStart;

            crystallic.OnSpellCastEvent -= OnSpellCastEvent;
            crystallic.OnSpellStopEvent -= OnSpellCastEvent;
        }

        private void OnSpellCastEvent(SpellCastCharge spell) => spell.endOnGrip = true;

        private void OnSpellUpdateEvent(SpellCastCharge spell)
        {
            if (spell.currentCharge >= spell.ReadyThreshold && spell.endOnGrip)
                spell.endOnGrip = false;
        }

        private void OnShardshotStart(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards)
        {
            if (eventTime == EventTime.OnStart)
                return;

            if (spellCastCrystallic.AdditionalShards.ContainsKey(this))
            {
                spellCastCrystallic.RemoveShardCountModifier(this);
                return;
            }

            if (wristHolders[spellCastCrystallic.spellCaster.side].numberOfPoints < maxShards)
                wristHolders[spellCastCrystallic.spellCaster.side].AddPoint();
        }

        private void OnButtonPressedWhileCasting(SpellCastCrystallic spellCastCrystallic, PlayerControl.Hand.Button button, bool pressed, bool casting)
        {
            if (!casting)
                return;

            if (button != PlayerControl.Hand.Button.Grip || !pressed || !spellCastCrystallic.Ready || wristHolders[spellCastCrystallic.spellCaster.side].numberOfPoints == 0)
                return;

            spellCastCrystallic.AddShardCountModifier(this, wristHolders[spellCastCrystallic.spellCaster.side].numberOfPoints);
            spellCastCrystallic.readyEffectData.Spawn(spellCastCrystallic.spellCaster.Orb).Play();
            spellCastCrystallic.spellCaster.ragdollHand.HapticTick();
            wristHolders[spellCastCrystallic.spellCaster.side].ClearPoints();
        }

        public void ToggleReservoir(Creature creature, Side side, bool active)
        {
            if (active)
            {
                ArcPointsManager arcPointsManager = new GameObject($"Crystallic Reservoir {side}").AddComponent<ArcPointsManager>();
                Transform parent = creature.transform.GetChildByNameRecursive(side + "ForeArmTwist");
                arcPointsManager.transform.SetParent(parent, false);
                arcPointsManager.transform.localPosition = new Vector3(-0.12f, side == Side.Left ? 0.014f : -0.014f, 0);
                arcPointsManager.transform.localRotation = Quaternion.Euler(0, -90, 0);
                arcPointsManager.originalLocalRotation = arcPointsManager.transform.localRotation;

                arcPointsManager.radius = 0.07f;
                arcPointsManager.defaultRadius = arcPointsManager.radius;
                arcPointsManager.totalAngle = 360;
                arcPointsManager.startAngle = 0;
                arcPointsManager.delayEvents = false;
                arcPointsManager.spinSpeed = 90;
                arcPointsManager.driftAmount = 0.05f;
                arcPointsManager.driftSpeed = 0.15f;
                arcPointsManager.onPointCreatedEvent += OnPointCreated;
                arcPointsManager.onPointRemovedEvent += OnPointRemoved;
                wristHolders[side] = arcPointsManager;
            }
            else
            {
                ArcPointsManager arcPointsManager = wristHolders[side];

                arcPointsManager.onPointCreatedEvent -= OnPointCreated;
                arcPointsManager.onPointRemovedEvent -= OnPointRemoved;
                if (arcPointsManager.numberOfPoints > 0)
                    for (int i = 0; i < arcPointsManager.numberOfPoints; i++)
                        arcPointsManager.RemovePoint();

                Object.Destroy(arcPointsManager.gameObject);

                if (wristHolders.ContainsKey(side))
                    wristHolders.Remove(side);
            }
        }

        private void OnPointCreated(ArcPointsManager pointsManager, ArcPointsManager.PointData point)
        {
            if (pointEffects.ContainsKey(point))
                pointEffects.Remove(point);

            EffectInstance effectInstance = spellCastCrystallic.shardEffectData.Spawn(point.transform);
            effectInstance.Play();
            effectInstance.SetVolume(0f);
            effectInstance.SetSize(0.75f);
            effectInstance.SetDisallowedSimulationSpace(ParticleSystemSimulationSpace.World);

            if (wristHolders.TryGetKey(pointsManager, out Side key))
                Player.currentCreature.GetHand(key).HapticTick();

            pointEffects.Add(point, effectInstance);
            onShardAdd?.Invoke(pointsManager, point);
        }

        private void OnPointRemoved(ArcPointsManager pointsManager, ArcPointsManager.PointData point)
        {
            if (wristHolders.TryGetKey(pointsManager, out Side key))
                Player.currentCreature.GetHand(key).HapticTick();

            if (pointEffects.ContainsKey(point))
            {
                EffectInstance effectInstance = pointEffects[point];
                effectInstance.SetParent(null);
                effectInstance.End();
                pointEffects.Remove(point);
                onShardRemove?.Invoke(pointsManager, point);
            }
        }
        #endif
    }
}