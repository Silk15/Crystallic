using System.Collections.Generic;
using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystallicQuasar : SpellSkillData
{
    public delegate void OnQuasarHit(QuasarImpact impact, SpellCastCrystallic spellCastCrystallic);

    [ModOption("Drain Charge", "Decides whether or not this skill drains spell charge. (Warning, very OP, not recommended for normal CH playthroughs!)"), ModOptionCategory("Crystallic Quasar", 7)]
    public static bool drainCharge = true;

    [ModOption("Charge Drain", "Controls how much charge is drained over the span of a second."), ModOptionCategory("Crystallic Quasar", 7), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.05f)]
    public static float chargeDrain = 0.2f;

    [ModOption("Haptic Intensity", "Controls how strong the haptic feedback is for the beam."), ModOptionCategory("Crystallic Quasar", 7), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float hapticIntensity = 1f;

    [ModOption("Dismemberment Allowance", "Controls how close the beam hit point has to be to a limb to dismember it."), ModOptionCategory("Crystallic Quasar", 7), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.075f)]
    public static float dismembermentDistance = 0.3f;

    [ModOption("Beam Max Distance", "Controls the max distance of the Raycast, this decides how far the beam can shootShardshot."), ModOptionCategory("Crystallic Quasar", 7), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float beamMaxDistance = 7.5f;

    public static EffectInstance beamLeftEffectInstance;
    public static EffectInstance beamLeftImpactEffectInstance;
    public static EffectInstance beamRightEffectInstance;
    public static EffectInstance beamRightImpactEffectInstance;
    public static bool leftActive;
    public static bool rightActive;
    private static string status = "";
    public EffectData beamEffectData;
    public string beamEffectId;
    public float beamHandLocomotionVelocityCorrectionMultiplier = 1f;
    public float beamHandPositionDamperMultiplier = 1f;
    public float beamHandPositionSpringMultiplier = 1f;
    public float beamHandRotationDamperMultiplier = 0.6f;
    public float beamHandRotationSpringMultiplier = 0.2f;
    public EffectData beamImpactEffectData;
    public string beamImpactEffectId;
    public LayerMask beamMask;
    public List<string> blacklistMaterialIds;
    public GameObject impactLeftGameObject;
    public GameObject impactRightGameObject;

    public static void SetStatus(string status)
    {
        SkillCrystallicQuasar.status = status;
    }

    public event OnQuasarHit onQuasarHit;

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        impactLeftGameObject = new GameObject();
        impactRightGameObject = new GameObject();
        SkillHyperintensity.onSpellOvercharge += OnSpellOvercharge;
        SkillHyperintensity.onSpellReleased += OnSpellReleased;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        SkillHyperintensity.onSpellOvercharge -= OnSpellOvercharge;
        SkillHyperintensity.onSpellReleased -= OnSpellReleased;
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnSpellStopEvent += OnSpellStopEvent;
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnSpellStopEvent -= OnSpellStopEvent;
    }

    private void OnSpellOvercharge(SpellCastCrystallic spellCastCrystallic)
    {
        if (spellCastCrystallic.spellCaster.side == Side.Left)
        {
            spellCastCrystallic.allowSpray = true;
            spellCastCrystallic.onSprayStart -= OnSprayStart;
            spellCastCrystallic.onSprayLoop -= OnSprayLoop;
            spellCastCrystallic.onSprayEnd -= OnSprayEnd;
            spellCastCrystallic.onSprayStart += OnSprayStart;
            spellCastCrystallic.onSprayLoop += OnSprayLoop;
            spellCastCrystallic.onSprayEnd += OnSprayEnd;
        }
        else
        {
            spellCastCrystallic.allowSpray = true;
            spellCastCrystallic.onSprayStart -= OnSprayStart;
            spellCastCrystallic.onSprayLoop -= OnSprayLoop;
            spellCastCrystallic.onSprayEnd -= OnSprayEnd;
            spellCastCrystallic.onSprayStart += OnSprayStart;
            spellCastCrystallic.onSprayLoop += OnSprayLoop;
            spellCastCrystallic.onSprayEnd += OnSprayEnd;
        }
    }

    private void OnSpellReleased(SpellCastCrystallic spellCastCrystallic)
    {
        if (spellCastCrystallic.spellCaster.side == Side.Left)
        {
            Player.local.handLeft.controlHand.StopHapticLoop(this);
            spellCastCrystallic.spellCaster.ragdollHand.playerHand.link.RemoveJointModifier(this);
            spellCastCrystallic.allowSpray = spellCastCrystallic.currentCharge > 0.1f;
            beamLeftEffectInstance?.End();
            beamLeftEffectInstance?.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
            beamLeftEffectInstance = null;
        }
        else
        {
            Player.local.handRight.controlHand.StopHapticLoop(this);
            spellCastCrystallic.spellCaster.ragdollHand.playerHand.link.RemoveJointModifier(this);
            spellCastCrystallic.allowSpray = spellCastCrystallic.currentCharge > 0.1f;
            beamRightEffectInstance?.End();
            beamRightEffectInstance?.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
            beamRightEffectInstance = null;
        }
    }

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        beamEffectData = Catalog.GetData<EffectData>(beamEffectId);
        beamImpactEffectData = Catalog.GetData<EffectData>(beamImpactEffectId);
    }

    private void OnSpellStopEvent(SpellCastCharge spell)
    {
        if (spell.spellCaster.side == Side.Left)
        {
            Player.local.handLeft.controlHand.StopHapticLoop(this);
            beamLeftEffectInstance?.End();
            beamLeftEffectInstance?.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
            beamLeftEffectInstance = null;
            leftActive = false;
            var spellCastCrystallic = spell as SpellCastCrystallic;
            spellCastCrystallic.onSprayStart -= OnSprayStart;
            spellCastCrystallic.onSprayLoop -= OnSprayLoop;
            spellCastCrystallic.onSprayEnd -= OnSprayEnd;
        }
        else
        {
            Player.local.handRight.controlHand.StopHapticLoop(this);
            beamRightEffectInstance?.End();
            beamRightEffectInstance?.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
            beamRightEffectInstance = null;
            rightActive = false;
            var spellCastCrystallic = spell as SpellCastCrystallic;
            spellCastCrystallic.onSprayStart -= OnSprayStart;
            spellCastCrystallic.onSprayLoop -= OnSprayLoop;
            spellCastCrystallic.onSprayEnd -= OnSprayEnd;
        }
    }

    private void OnSprayStart(SpellCastCrystallic spellCastCrystallic)
    {
        if (spellCastCrystallic.spellCaster.side == Side.Left)
        {
            beamLeftEffectInstance = beamEffectData?.Spawn(spellCastCrystallic.spellCaster.magicSource.transform);
            beamLeftEffectInstance?.Play();
            beamLeftEffectInstance.SetColorImmediate(spellCastCrystallic.currentColor);
            leftActive = true;
            Player.local.handLeft.link.SetJointModifier(this, beamHandPositionSpringMultiplier, beamHandPositionDamperMultiplier, beamHandRotationSpringMultiplier, beamHandRotationDamperMultiplier, beamHandLocomotionVelocityCorrectionMultiplier);
        }
        else
        {
            beamRightEffectInstance = beamEffectData?.Spawn(spellCastCrystallic.spellCaster.magicSource.transform);
            beamRightEffectInstance?.Play();
            beamRightEffectInstance.SetColorImmediate(spellCastCrystallic.currentColor);
            rightActive = true;
            Player.local.handRight.link.SetJointModifier(this, beamHandPositionSpringMultiplier, beamHandPositionDamperMultiplier, beamHandRotationSpringMultiplier, beamHandRotationDamperMultiplier, beamHandLocomotionVelocityCorrectionMultiplier);
        }
    }

    private void OnSprayLoop(SpellCastCrystallic spellCastCrystallic)
    {
        if (spellCastCrystallic.spellCaster.side == Side.Left)
        {
            if (drainCharge) spellCastCrystallic.TryDrainCharge(chargeDrain);
            if (spellCastCrystallic.currentCharge <= 0.1f) return;
            Player.local.handLeft.controlHand.HapticLoop(this, hapticIntensity, 0.01f);
            if (Physics.Raycast(spellCastCrystallic.spellCaster.magicSource.transform.position, spellCastCrystallic.spellCaster.magicSource.transform.up, out var hitInfo, beamMaxDistance))
            {
                var creature = hitInfo.collider?.GetComponentInParent<Creature>();
                impactLeftGameObject.transform.position = hitInfo.point;
                if (!creature)
                {
                    if (beamLeftImpactEffectInstance == null) beamLeftImpactEffectInstance = beamImpactEffectData.Spawn(impactLeftGameObject.transform);
                    if (beamLeftImpactEffectInstance != null && !beamLeftImpactEffectInstance.isPlaying)
                    {
                        beamLeftImpactEffectInstance?.Play();
                        beamLeftImpactEffectInstance.SetColorImmediate(spellCastCrystallic.currentColor);
                    }
                }

                if (hitInfo.collider != null && creature && !creature.isPlayer)
                {
                    RagdollPart part = null;
                    var brainModuleCrystal = creature?.brain?.instance?.GetModule<BrainModuleCrystal>();
                    brainModuleCrystal.Crystallise(5);
                    brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, spellCastCrystallic.spellId), spellCastCrystallic.spellId);
                    brainModuleCrystal.isCrystallised = true;
                    if (!string.IsNullOrEmpty(status)) creature.Inflict(status, this, 5, 30 * Time.deltaTime);
                    if (creature && creature.ragdoll.GetClosestPart(hitInfo.point, dismembermentDistance, out part) && part && part.sliceAllowed && part != creature.ragdoll.rootPart && !part.hasMetalArmor)
                    {
                        part?.TrySlice();
                        creature?.Kill();
                    }

                    onQuasarHit?.Invoke(new QuasarImpact(hitInfo.collider, hitInfo.point, hitInfo.normal, part, creature), spellCastCrystallic);
                }
            }
            else
            {
                beamLeftImpactEffectInstance?.End();
                beamLeftImpactEffectInstance = null;
            }
        }
        else
        {
            if (drainCharge) spellCastCrystallic.TryDrainCharge(chargeDrain);
            if (spellCastCrystallic.currentCharge <= 0.1f) return;
            Player.local.handRight.controlHand.HapticLoop(this, hapticIntensity, 0.01f);
            if (Physics.Raycast(spellCastCrystallic.spellCaster.magicSource.transform.position, spellCastCrystallic.spellCaster.magicSource.transform.up, out var hitInfo, beamMaxDistance))
            {
                var creature = hitInfo.collider?.GetComponentInParent<Creature>();
                impactRightGameObject.transform.position = hitInfo.point;
                if (!creature)
                {
                    if (beamRightImpactEffectInstance == null) beamRightImpactEffectInstance = beamImpactEffectData.Spawn(impactRightGameObject.transform);
                    if (beamRightImpactEffectInstance != null && !beamRightImpactEffectInstance.isPlaying)
                    {
                        beamRightImpactEffectInstance?.Play();
                        beamRightImpactEffectInstance.SetColorImmediate(spellCastCrystallic.currentColor);
                    }
                }

                if (hitInfo.collider != null && creature && !creature.isPlayer)
                {
                    RagdollPart part = null;
                    var brainModuleCrystal = creature?.brain?.instance?.GetModule<BrainModuleCrystal>();
                    brainModuleCrystal.Crystallise(5);
                    brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, spellCastCrystallic.spellId), spellCastCrystallic.spellId);
                    brainModuleCrystal.isCrystallised = true;
                    if (creature && creature.ragdoll.GetClosestPart(hitInfo.point, dismembermentDistance, out part) && part && part.sliceAllowed && part != creature.ragdoll.rootPart && !part.hasMetalArmor)
                    {
                        part?.TrySlice();
                        creature?.Kill();
                    }

                    onQuasarHit?.Invoke(new QuasarImpact(hitInfo.collider, hitInfo.point, hitInfo.normal, part, creature), spellCastCrystallic);
                }
            }
            else
            {
                beamRightImpactEffectInstance?.End();
                beamRightImpactEffectInstance = null;
            }
        }
    }

    private void OnSprayEnd(SpellCastCrystallic spellCastCrystallic)
    {
        if (spellCastCrystallic.spellCaster.side == Side.Left)
        {
            Player.local.handLeft.controlHand.StopHapticLoop(this);
            beamLeftEffectInstance?.End();
            beamLeftEffectInstance.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
            leftActive = false;
            if (beamLeftImpactEffectInstance != null && beamLeftImpactEffectInstance.isPlaying) beamLeftImpactEffectInstance.Stop();
        }
        else
        {
            Player.local.handRight.controlHand.StopHapticLoop(this);
            beamRightEffectInstance?.End();
            beamRightEffectInstance.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
            rightActive = false;
            if (beamRightImpactEffectInstance != null && beamRightImpactEffectInstance.isPlaying) beamRightImpactEffectInstance.Stop();
        }
    }

    public class QuasarImpact
    {
        public Collider hitCollider;
        public ThunderEntity hitEntity;
        public Vector3 hitNormal;
        public Vector3 hitPoint;
        public RagdollPart hitRagdollPart;

        public QuasarImpact(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal, RagdollPart hitRagdollPart, ThunderEntity hitEntity)
        {
            this.hitCollider = hitCollider;
            this.hitPoint = hitPoint;
            this.hitNormal = hitNormal;
            this.hitRagdollPart = hitRagdollPart;
            this.hitEntity = hitEntity;
        }
    }
}