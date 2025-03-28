using System.Linq;
using Crystallic.AI;
using Crystallic.Skill;
using Crystallic.Skill.Spell;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class ImbueBehavior : ThunderBehaviour
{
    public bool isActive;
    public Imbue imbue;
    public EffectInstance imbueEffectInstance;
    public float cooldown = 0.01f;
    public SkillCrystalImbueHandler handler;
    public EffectData imbueEffectData;
    public EffectData imbueHitEffectData;
    private float lastTime;
    public Color currentColor;

    public virtual void Activate(Imbue imbue, SkillCrystalImbueHandler handler)
    {
        isActive = true;
        this.handler = handler;
        this.imbue = imbue;
        imbue.OnImbueHit += Hit;
        imbue.OnImbueSpellChange += SpellChange;
        imbueEffectData = Catalog.GetData<EffectData>(handler.imbueEffectId);
        imbueHitEffectData = Catalog.GetData<EffectData>(handler.imbueHitEffectId);
        imbueEffectInstance = imbueEffectData?.Spawn(imbue.transform.position, imbue.transform.rotation, imbue.transform, null, true, imbue.colliderGroup, false, imbue.EnergyRatio, 1f, null);
        if (imbue.colliderGroup.imbueEffectRenderer) imbueEffectInstance?.SetRenderer(imbue.colliderGroup.imbueEffectRenderer, false);
        imbueEffectInstance?.Play();
        imbueEffectInstance.SetColorImmediate(handler.colorModifier);
    }

    private void SpellChange(Imbue imbue1, SpellCastCharge spellData, float amount, float change, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart) Destroy(this);
    }

    private void Hit(SpellCastCharge spellCastCharge, float something, bool otherthing, CollisionInstance hit, EventTime eventTime)
    {
        if (Time.time - lastTime > cooldown && hit.impactVelocity.magnitude >= SpellCastCrystallic.imbueHitVelocity && hit.impactVelocity.magnitude <= handler.minMaxImpactVelocity.y && !hit.targetMaterial.isMetal)
        {
            lastTime = Time.time;
            var creature = hit?.targetColliderGroup?.collisionHandler?.Entity as Creature;
            var item = hit?.targetColliderGroup?.collisionHandler?.Entity as Item;
            if (creature && creature != imbue.imbueCreature && handler.crystallise)
            {
                var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                brainModuleCrystal.Crystallise(handler.crystalliseDuration);
                brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, handler.spellId), handler.spellId);
            }

            Hit(hit, spellCastCharge, creature, item);
            var instance = imbueHitEffectData?.Spawn(hit.contactPoint, Quaternion.LookRotation(hit.contactNormal, hit.sourceColliderGroup.transform.up), hit.targetCollider.transform);
            instance?.Play();
            if (currentColor != Color.black)
            {
                instance.SetColorImmediate(currentColor);
                var decal = instance?.effects.First(m => m is EffectDecal) as EffectDecal;
                if (decal) decal.emissionColorGradient = ThunderRoad.Utils.CreateGradient(Color.black, currentColor);
            }
        }
    }

    public void SetColorModifier(Color color)
    {
        currentColor = color;
    }

    public void ClearColorModifier()
    {
        currentColor = new Color(0, 0, 0, 0);
    }

    public virtual void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge, Creature hitCreature = null, Item hitItem = null) { }

    public virtual void Deactivate()
    {
        isActive = false;
        imbueEffectInstance.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
        imbueEffectInstance?.End();
        if (imbueEffectInstance == null) return;
        imbue.OnImbueHit -= Hit;
        imbue.OnImbueSpellChange -= SpellChange;
        imbueEffectInstance = null;
    }
}