using System;
using ThunderRoad;
using TriInspector;
using UnityEngine;

namespace Crystallic.Skill.Imbue
{
    public abstract class ImbueBehaviour : ThunderBehaviour
    {
        [NonSerialized]
        public CrystalImbueSkillData crystalImbueSkillData;

        #if !SDK
        [NonSerialized]
        public EffectInstance imbueEffect;

        [NonSerialized]
        public ThunderRoad.Imbue imbue;
        #endif

        private float lastTime;
        private float cooldown = 0.05f;

        public new bool enabled;

        #if !SDK
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        public virtual void Load(CrystalImbueSkillData crystalImbueSkillData, ThunderRoad.Imbue imbue)
        {
            this.crystalImbueSkillData = crystalImbueSkillData;
            enabled = true;
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
                crystalImbueSkillData.imbueHitEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.targetCollider.transform).Play();

                if (crystalImbueSkillData.crystalliseOnHit && collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && creature != null && creature != spellData.spellCaster.mana.creature && collisionInstance.targetMaterial != null && !collisionInstance.targetMaterial.IsMetal())
                    creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), crystalImbueSkillData.spellId), crystalImbueSkillData.spellId));
            }
        }

        public virtual void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge)
        {
        }

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
            enabled = false;
        }
        #endif

        public TriDropdownList<string> GetAllHandPoseID() => Catalog.GetDropdownAllID(Category.HandPose);

        public TriDropdownList<string> GetAllStatusEffectID() => Catalog.GetDropdownAllID(Category.Status);

        public TriDropdownList<string> GetAllSpellID() => Catalog.GetDropdownAllID<SpellData>();

        public TriDropdownList<string> GetAllShardsId() => Catalog.GetDropdownAllID(Category.Item);

        public TriDropdownList<string> GetAllEffectID() => Catalog.GetDropdownAllID(Category.Effect);

        public TriDropdownList<string> GetAllSkillID() => Catalog.GetDropdownAllID(Category.Skill);

        public TriDropdownList<string> GetAllItemID() => Catalog.GetDropdownAllID(Category.Item);

        public TriDropdownList<string> GetAllSkillTreeID() => Catalog.GetDropdownAllID(Category.SkillTree);

        public TriDropdownList<string> GetAllDamagerID() => Catalog.GetDropdownAllID(Category.Damager);

        public TriDropdownList<string> GetAllMaterialID() => Catalog.GetDropdownAllID(Category.Material);
    }
}