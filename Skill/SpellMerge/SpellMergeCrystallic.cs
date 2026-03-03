using System;
using System.Collections;
using System.Collections.Generic;
using Crystallic.Skill.Spell;
using ThunderRoad;
using TriInspector;
using UnityEngine;

namespace Crystallic.Skill.SpellMerge
{
    public class SpellMergeCrystallic : SpellMergeData
    {
        public AnimationCurve projectileIntensityCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 0.5f, 1f);

        public float projectileVelocity = 0.4f;
        public float coreFireLifetime = 6f;
        public float coreFireDelay = 2f;
        public float dragDelay;
        public float drag;
        public bool projectileUseCollision;
        public bool projectileUseGravity;

        [NonSerialized]
        public EffectData fireHitEffectData;

        [Dropdown(nameof(GetAllEffectID))]
        public string fireHitEffectId;

        [NonSerialized]
        public EffectData coreFireEffectData;

        [Dropdown(nameof(GetAllEffectID))]
        public string coreFireEffectId;

        [NonSerialized]
        public EffectData coreEffectData;

        [Dropdown(nameof(GetAllEffectID))]
        public string coreEffectId;

        [NonSerialized]
        public ItemData coreItemData;

        [Dropdown(nameof(GetAllEffectID))]
        public string coreItemId;

        #if !SDK
        [NonSerialized]
        public Dictionary<Item, EffectInstance> activeFireEffects = new();
        #endif

        [NonSerialized]
        public Dictionary<Creature, float> lastPushTimes = new();

        private List<ParticleCollisionEvent> collisionEvents = new();

        #if !SDK
        [NonSerialized]
        public ItemMagicProjectile lastThrownProjectile;

        public event CollapseDelegate onCoreCollapsed;
        public event ThrowDelegate onProjectileThrown;
        public event FireDelegate onShardsFired;

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            fireHitEffectData = Catalog.GetData<EffectData>(fireHitEffectId);
            coreFireEffectData = Catalog.GetData<EffectData>(coreFireEffectId);
            coreEffectData = Catalog.GetData<EffectData>(coreEffectId);
            coreItemData = Catalog.GetData<ItemData>(coreItemId);

            lastPushTimes.Clear();
            activeFireEffects.Clear();
        }

        public override void Merge(bool active)
        {
            base.Merge(active);
            if (!active)
                currentCharge = 0f;
        }

        public override void Throw(Vector3 velocity)
        {
            base.Throw(velocity);
            coreItemData.SpawnAsync(item =>
            {
                if (!item.TryGetComponent(out ItemMagicProjectile itemMagicProjectile))
                {
                    Debug.LogWarning($"[Crystallic] Item: {item.itemId} has no ItemMagicProjectile component, this item cannot be used as a merge projectile!");
                    return;
                }

                item.DisallowDespawn = true;
                lastThrownProjectile = itemMagicProjectile;

                itemMagicProjectile.guidance = GuidanceMode.NonGuided;
                itemMagicProjectile.guidanceAmount = 0f;
                itemMagicProjectile.homing = false;

                itemMagicProjectile.effectIntensityCurve = projectileIntensityCurve;
                itemMagicProjectile.speed = projectileVelocity;
                itemMagicProjectile.destroyInWater = true;
                itemMagicProjectile.despawnOnHit = true;

                if (!projectileUseGravity)
                    item.SetPhysicModifier(this, 0.0f);

                if (!projectileUseCollision)
                    item.SetColliders(false, true);

                item.physicBody.AddForce(velocity * projectileVelocity, ForceMode.Impulse);
                itemMagicProjectile.Fire(velocity, coreEffectData, shooterRagdoll: mana.creature.ragdoll, haptic: HapticDevice.LeftController | HapticDevice.RightController);
                onProjectileThrown?.Invoke(this, itemMagicProjectile);

                itemMagicProjectile.StartCoroutine(ActivationCoroutine(itemMagicProjectile));

            }, mana.mergePoint.transform.position, mana.mergePoint.transform.rotation);
            mana.creature.ragdoll.ForBothHands(hand => hand.playerHand.controlHand.HapticPlayClip(Catalog.gameData.haptics.telekinesisThrow));
        }

        public IEnumerator ActivationCoroutine(ItemMagicProjectile projectile)
        {
            yield return Yielders.ForSeconds(dragDelay);
            projectile.item.physicBody.drag = drag;

            yield return Yielders.ForSeconds(coreFireDelay - dragDelay);
            EffectInstance coreEffectInstance = coreFireEffectData.Spawn(projectile.transform);
            coreEffectInstance.OnParticleCollisionEvent -= OnParticleCollisionEvent;
            coreEffectInstance.OnParticleCollisionEvent += OnParticleCollisionEvent;
            coreEffectInstance.Play();
            coreEffectInstance.SetIntensity(1f);

            activeFireEffects[projectile.item] = coreEffectInstance;

            onShardsFired?.Invoke(this, coreEffectInstance);
            float elapsed = 0f;

            projectile.item.OnDespawnEvent -= OnDespawn;
            projectile.item.OnDespawnEvent += OnDespawn;

            SpellCastCrystallic spellCastCrystallic = Catalog.GetData<SpellCastCrystallic>("Crystallic");
            int num = UnityEngine.Random.Range(5, 9);

            for (int index = 0; index < num; ++index)
            {
                Vector3 vector3 = UnityEngine.Random.insideUnitSphere * 1.25f;
                spellCastCrystallic.FireShard(spellCastCrystallic.shardEffectData, projectile.transform.position + vector3, projectile.transform.position + vector3.normalized * 8f, 2f, 1.0f);
            }

            while (elapsed < coreFireLifetime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            coreEffectInstance.OnParticleCollisionEvent -= OnParticleCollisionEvent;
            coreEffectInstance.SetParent(null);
            coreEffectInstance.End();
            coreEffectInstance = null;

            projectile.item.DisallowDespawn = false;
            projectile.effectInstance.End();

            onCoreCollapsed?.Invoke(this, projectile);

            void OnDespawn(EventTime eventTime)
            {
                if (eventTime == EventTime.OnStart && coreEffectInstance != null)
                {
                    projectile.item.OnDespawnEvent -= OnDespawn;
                    coreEffectInstance.OnParticleCollisionEvent -= OnParticleCollisionEvent;
                    coreEffectInstance.SetParent(null);
                    coreEffectInstance.End();
                    coreEffectInstance = null;
                }
            }

            void OnParticleCollisionEvent(GameObject other)
            {
                if (other.TryGetComponentInParent(out Creature creature) && creature != mana.creature && creature != mana.creature)
                {
                    if (!lastPushTimes.ContainsKey(creature) || Time.time - lastPushTimes[creature] >= 0.5f)
                    {
                        creature.TryPush(Creature.PushType.Magic, (creature.ragdoll.targetPart.transform.position - projectile.transform.position).normalized, 1);
                        lastPushTimes[creature] = Time.time;
                        creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
                    }
                }

                int numEvents = coreEffectInstance.GetParticleSystem("SmallCrystals").GetCollisionEvents(other, collisionEvents);
                for (int i = 0; i < numEvents; i++)
                {
                    ParticleCollisionEvent collisionEvent = collisionEvents[i];
                    fireHitEffectData.Spawn(collisionEvent.intersection, Quaternion.LookRotation(-collisionEvent.normal, collisionEvent.colliderComponent.transform.up), collisionEvent.colliderComponent.transform).Play();
                }
            }
        }

        public delegate void ThrowDelegate(SpellMergeCrystallic spellMergeCrystallic, ItemMagicProjectile itemMagicProjectile);

        public delegate void FireDelegate(SpellMergeCrystallic spellMergeCrystallic, EffectInstance effectInstance);

        public delegate void CollapseDelegate(SpellMergeCrystallic spellMergeCrystallic, ItemMagicProjectile itemMagicProjectile);

        #endif
    }
}