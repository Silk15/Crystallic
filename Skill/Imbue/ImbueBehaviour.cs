using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill.Imbue;

public abstract class ImbueBehaviour : ThunderBehaviour
{
    public CrystalImbueSkillData crystalImbueSkillData;
    public EffectInstance imbueEffect;
    public ThunderRoad.Imbue imbue;

    private float lastTime;
    private float cooldown = 0.05f;

    public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

    public virtual void Load(CrystalImbueSkillData crystalImbueSkillData, ThunderRoad.Imbue imbue)
    {
        this.crystalImbueSkillData = crystalImbueSkillData;
        this.imbue = imbue;
        imbueEffect = crystalImbueSkillData.imbueEffectData.Spawn(imbue.colliderGroup.transform);
        imbueEffect.SetRenderer(imbue.colliderGroup.imbueEffectRenderer, false);
        imbueEffect.SetColor(crystalImbueSkillData.colorModifier);
        imbue.OnImbueHit += OnImbueHit;
    }

    private void OnImbueHit(SpellCastCharge spellData, float amount, bool fired, CollisionInstance collisionInstance, EventTime eventTime)
    {
        if (Time.time - lastTime > cooldown && collisionInstance.impactVelocity.magnitude > 7.5f)
        {
            lastTime = Time.time;
            Hit(collisionInstance, spellData);
            crystalImbueSkillData.imbueCollisionEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.targetCollider.transform).Play();
            if (crystalImbueSkillData.crystalliseOnHit && collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && creature != null && creature != spellData.spellCaster.mana.creature && collisionInstance.targetMaterial != null && !collisionInstance.targetMaterial.IsMetal())
                creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), crystalImbueSkillData.spellId), crystalImbueSkillData.spellId));
        }
    }

    public virtual void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge) { }

    protected override void ManagedUpdate()
    {
        base.ManagedUpdate();
        if (imbue != null && imbue.spellCastBase != null && imbue.colliderGroup != null && imbue.colliderGroup.collisionHandler != null && imbueEffect != null)
        {
            float speedRatio = Mathf.InverseLerp(imbue.spellCastBase.imbueWhooshMinSpeed, imbue.spellCastBase.imbueWhooshMaxSpeed, (imbue.colliderGroup.collisionHandler != null ? imbue.colliderGroup.collisionHandler.physicBody.GetPointVelocity(imbue.colliderGroup.whooshPoint.position) : Vector3.zero).magnitude);
            imbueEffect.SetIntensity(speedRatio);
        }
    }

    public virtual void Unload(ThunderRoad.Imbue imbue)
    {
        imbueEffect.End();
        imbue.OnImbueHit -= OnImbueHit;
    }
}