using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill;

public class ImbueBehaviour : ThunderBehaviour
{
    public SkillCrystalImbueHandler skillCrystalImbueHandler;
    public EffectInstance imbueEffect;
    public Imbue imbue;

    private float lastTime;
    private float cooldown;

    public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

    public virtual void Load(SkillCrystalImbueHandler skillCrystalImbueHandler, Imbue imbue)
    {
        this.skillCrystalImbueHandler = skillCrystalImbueHandler;
        this.imbue = imbue;
        imbueEffect = skillCrystalImbueHandler.imbueEffectData.Spawn(imbue.colliderGroup.transform);
        imbueEffect.SetRenderer(imbue.colliderGroup.imbueEffectRenderer, false);
        imbueEffect.SetColor(skillCrystalImbueHandler.colorModifier);
        imbue.OnImbueHit += OnImbueHit;
    }

    private void OnImbueHit(SpellCastCharge spellData, float amount, bool fired, CollisionInstance collisionInstance, EventTime eventTime)
    {
        if (Time.time - lastTime > cooldown && collisionInstance.impactVelocity.magnitude > 7.5f)
        {
            lastTime = Time.time;
            Hit(collisionInstance, spellData);
            skillCrystalImbueHandler.imbueCollisionEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.targetCollider.transform).Play();
            if (skillCrystalImbueHandler.crystalliseOnHit && collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && creature != null && creature != spellData.spellCaster.mana.creature && collisionInstance.targetMaterial != null && !collisionInstance.targetMaterial.IsMetal())
                creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), skillCrystalImbueHandler.spellId), skillCrystalImbueHandler.spellId));
        }
    }

    public virtual void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge)
    {
        
    }

    protected override void ManagedUpdate()
    {
        base.ManagedUpdate();
        float speedRatio = Mathf.InverseLerp(imbue.spellCastBase.imbueWhooshMinSpeed, imbue.spellCastBase.imbueWhooshMaxSpeed, (imbue.colliderGroup.collisionHandler != null ? imbue.colliderGroup.collisionHandler.physicBody.GetPointVelocity(imbue.colliderGroup.whooshPoint.position) : Vector3.zero).magnitude);
        imbueEffect.SetIntensity(speedRatio);
    }

    public virtual void Unload(Imbue imbue)
    {
        imbueEffect.End();
        imbue.OnImbueHit -= OnImbueHit;
    }
}