using System;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using TriInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Crystallic.Skill.Imbue
{
    public class ImbueFireBehaviour : ImbueBehaviour
    {
        #if !SDK
        [ModOption("Hits Required To Detonate", "The required hit count to detonate an enemy with this crystal imbue.", order = 0), ModOptionCategory("Flaming Crystals", 5), ModOptionSlider, ModOptionIntValues(1, 10, 1)]
        public static int hitsRequiredToDetonate = 2;

        [ModOption("Detonation Force", "The force added to detonated enemies.", order = 1), ModOptionCategory("Flaming Crystals", 5), ModOptionSlider, ModOptionFloatValues(10f, 100f, 1f)]
        public static float explosionForce = 70f;

        [ModOption("Detonation Radius", "The detonation radius for enemies.", order = 2), ModOptionCategory("Flaming Crystals", 5), ModOptionSlider, ModOptionFloatValues(1f, 100f, 1f)]
        public static float detonationRadius = 5f;

        [ModOption("Detonation Upwards Force Multiplier", "The upwards force multiplier for detonated enemies.", order = 3), ModOptionCategory("Flaming Crystals", 4), ModOptionSlider, ModOptionFloatValues(0.1f, 10f, 0.1f)]
        public static float upwardsForceMultiplier = 0.1f;
        #endif

        public AnimationCurve flameCurve = new(new Keyframe(0.0f, 0.5f), new Keyframe(0.05f, 30), new Keyframe(0.1f, 0.5f));

        [NonSerialized]
        public EffectData detonateEffectData;

        [Dropdown(nameof(GetAllEffectID))]
        public string detonateEffectId = "RemoteDetonation";

        [NonSerialized]
        public SpellCastProjectile spellCastProjectile;

        #if !SDK
        public override void Load(CrystalImbueSkillData handler, ThunderRoad.Imbue imbue)
        {
            base.Load(handler, imbue);
            detonateEffectData = Catalog.GetData<EffectData>(detonateEffectId);
            spellCastProjectile = imbue.spellCastBase as SpellCastProjectile;
        }

        public override void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge)
        {
            if (collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is not Creature hitCreature)
                return;

            var item = collisionInstance?.sourceColliderGroup?.collisionHandler?.Entity as Item;
            hitCreature.SetVariable("HasDetonated", hitCreature.GetVariable<int>("HasDetonated") + 1);

            if (item && hitCreature && hitCreature != imbue.imbueCreature && hitCreature.TryGetVariable("HasDetonated", out int flag) && flag == hitsRequiredToDetonate)
            {
                detonateEffectData?.Spawn(hitCreature.ragdoll.targetPart.transform).Play();
                hitCreature.Inflict("Burning", this, parameter: 100);

                if (!hitCreature.isPlayer)
                    hitCreature?.AddExplosionForce(explosionForce, collisionInstance.contactPoint, detonationRadius, upwardsForceMultiplier, ForceMode.Impulse);

                item.PlayHapticClip(flameCurve, 0.25f);
                item?.AddForce((item.transform.position - hitCreature.transform.position).normalized * 2, ForceMode.Impulse);

                int random = Random.Range(1, 4);
                for (int i = 0; i < random; i++)
                {
                    Vector3 vector3 = Random.insideUnitSphere * 1.25f;
                    FireProjectile(hitCreature, collisionInstance.contactPoint + vector3, collisionInstance.contactPoint + vector3.normalized * 8f);
                }
            }
        }

        public void FireProjectile(Creature creature, Vector3 position, Vector3 direction)
        {
            spellCastProjectile.ShootFireSpark(spellCastProjectile.imbueHitProjectileEffectData, position, direction, onSpawnEvent: projectile =>
            {
                projectile.item.IgnoreRagdollCollision(creature.ragdoll);
                projectile.guidance = GuidanceMode.NonGuided;
                projectile.guidanceFunc = null;
                projectile.RunAfter(() => { projectile.homing = true; }, 0.4f);
            });
        }
        #endif
    }
}