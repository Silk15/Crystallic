using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.DebugViz;
using ThunderRoad.Skill.Spell;
using TriInspector;
using UnityEngine;
using QualityLevel = ThunderRoad.QualityLevel;
using Random = UnityEngine.Random;

namespace Crystallic.Skill.Spell;

public class SpellCastCrystallic : SpellCastCharge
{
    #if !SDK
    [ModOption("Allow Imbue Crystallisation", "Controls whether the crystallic imbue can crystallise people or not.", order = 0), ModOptionCategory("Spell", 1)]
    public static bool allowImbueCrystallisation = true;

    [ModOption("Allow Shard Crystallisation", "Controls whether the crystallic shards can crystallise people or not.", order = 1), ModOptionCategory("Spell", 1)]
    public static bool allowShardCrystallisation = true;

    [ModOption("Allow Staff Crystallisation", "Controls whether the crystallic staff slam can crystallise people or not.", order = 2), ModOptionCategory("Spell", 1)]
    public static bool allowStaffCrystallisation = true;

    [ModOption("Default Shard Count", "Controls how many shards spawn (BY DEFAULT) when the spell is thrown, this value is modified by skills as well.", order = 3), ModOptionSlider, ModOptionCategory("Spell", 1), ModOptionIntValues(1, 12, 1)]
    public static int defaultShardCount = 3;
    #endif

    public AnimationCurve pulseCurve = new(new Keyframe(0.0f, 10f), new Keyframe(0.05f, 25f), new Keyframe(0.1f, 10f));

    public float slamUpwardsForceMult = 0.35f;
    public float staffSlamMinForce = 10f;
    public float staffSlamMaxForce = 60f;
    public float staffSlamMaxRadius = 7.5f;
    public float efficiencyPerSkill = 0.5f;
    public float intensityPerSkill = 0.1f;
    public float lastShardshotTime = 0f;

    public float musketRadius = 2f;
    public float musketForce = 50f;
    public float musketExplosionRadius = 5f;
    public float musketExplosionForce = 2f;
    public float musketHitCrystalDamage = 60f;
    public float musketExplosionCrystalDamage = 15f;
    public float musketShootSpeed = 32f;
    public float musketPlayerForce = 10f;

    [NonSerialized]
    public EffectData imbueCollisionEffectData;

    [Dropdown(nameof(GetAllEffectID))]
    public string imbueCollisionEffectId;

    [NonSerialized]
    public EffectData shardEffectData;

    [Dropdown(nameof(GetAllEffectID))]
    public string shardEffectId;

    [NonSerialized]
    public EffectData pulseEffectData;

    [Dropdown(nameof(GetAllEffectID))]
    public string pulseEffectId;

    [NonSerialized]
    public DamagerData shardDamagerData;

    [Dropdown(nameof(GetAllEffectID))]
    public string shardDamagerId;

    [NonSerialized]
    public EffectData staffSlamEffectData;

    [Dropdown(nameof(GetAllEffectID))]
    public string staffSlamEffectId;

    [NonSerialized]
    public ItemData shardItemData;

    [Dropdown(nameof(GetAllEffectID))]
    public string shardItemId;

    [NonSerialized]
    public ItemData musketProjectileData;

    [Dropdown(nameof(GetAllEffectID))]
    public string musketProjectileId = "DynamicAreaProjectile";

    [NonSerialized]
    public EffectData musketProjectileExplosionEffectData;

    [Dropdown(nameof(GetAllEffectID))]
    public string musketProjectileExplosionEffectId = "HitMusketCrystallic";

    [NonSerialized]
    public EffectData musketProjectileEffectData;

    [Dropdown(nameof(GetAllEffectID))]
    public string musketProjectileEffectId = "CrystalShard";

    [NonSerialized]
    public Vector3 lastShardshotVelocity;

    [NonSerialized]
    public SkillHyperdetonation skillHyperdetonation;

    #if !SDK
    [NonSerialized]
    public List<Shard> lastShards = new();

    [NonSerialized]
    public EffectInstance chargeEffect;

    private int unlockedTierBlockers;
    private float cooldown = 0.1f;
    private float lastTime = 1;

    public event SprayDelegate onSprayStart;
    public event SprayDelegate onSprayLoop;
    public event SprayDelegate onSprayEnd;

    public event ShardHitEvent onShardHit;
    public event ShardEvent onShardSpawn;
    public event ShardEvent onShardDespawn;
    public event ShardshotStartEvent onShardshotStart;
    public event ShardshotEndEvent onShardshotEnd;
    public event ButtonDelegate onButtonPressed;

    [JsonIgnore]
    public Dictionary<object, float> AdditionalLifetime { get; } = new();

    [JsonIgnore]
    public Dictionary<object, int> AdditionalShards { get; } = new();

    [JsonIgnore]
    public int ShardCount
    {
        get
        {
            int baseCount = defaultShardCount;
            foreach (int value in AdditionalShards.Values) baseCount += value;
            return baseCount + unlockedTierBlockers;
        }
    }

    [JsonIgnore]
    public float ShardLifetime
    {
        get
        {
            float shardLifetime = 0.75f;
            foreach (int value in AdditionalLifetime.Values) shardLifetime += value;
            return shardLifetime;
        }
    }

    public new SpellCastCrystallic Clone() => MemberwiseClone() as SpellCastCrystallic;

    public void AddShardCountModifier(object handler, int count) => AdditionalShards[handler] = count;

    public void RemoveShardCountModifier(object handler)
    {
        if (AdditionalShards.ContainsKey(handler))
            AdditionalShards.Remove(handler);
    }

    public void AddShardLifetimeModifier(object handler, float value) => AdditionalLifetime[handler] = value;

    public void RemoveShardLifetimeModifier(object handler)
    {
        if (AdditionalLifetime.ContainsKey(handler))
            AdditionalLifetime.Remove(handler);
    }

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        skillHyperdetonation = Catalog.GetData<SkillHyperdetonation>("Hyperdetonation");
        staffSlamEffectData = Catalog.GetData<EffectData>(staffSlamEffectId);
        pulseEffectData = Catalog.GetData<EffectData>(pulseEffectId);
        shardEffectData = Catalog.GetData<EffectData>(shardEffectId);
        shardDamagerData = Catalog.GetData<DamagerData>(shardDamagerId);
        imbueCollisionEffectData = Catalog.GetData<EffectData>(imbueCollisionEffectId);
        shardItemData = Catalog.GetData<ItemData>(shardItemId);
        musketProjectileEffectData = Catalog.GetData<EffectData>(musketProjectileEffectId);
        musketProjectileExplosionEffectData = Catalog.GetData<EffectData>(musketProjectileExplosionEffectId);
        musketProjectileData = Catalog.GetData<ItemData>(musketProjectileId);
    }

    public override void Load(SpellCaster spellCaster)
    {
        base.Load(spellCaster);
        if (!spellCaster.ragdollHand.ragdoll.creature.isPlayer) return;
        spellCaster.ragdollHand.playerHand.controlHand.OnButtonPressEvent += OnButtonPressWhileCasting;
        spellCaster.mana.OnSpellUnloadEvent += OnSpellUnload;
    }

    public override void Unload()
    {
        base.Unload();
        if (imbue == null && imbueEffect != null)
            foreach (var particleSystem in imbueEffect.GetParticleSystems())
            {
                var particles = new ParticleSystem.Particle[particleSystem.particleCount];
                int count = particleSystem.GetParticles(particles);

                for (int i = 0; i < count; i++)
                    particles[i].remainingLifetime = 0.5f;

                particleSystem.SetParticles(particles, count);
            }
    }

    public override void OnLateSkillsLoaded(SkillData skillData, Creature creature)
    {
        base.OnLateSkillsLoaded(skillData, creature);
        unlockedTierBlockers = Catalog.GetDataList<SkillData>().Count(s => creature.HasSkill(s) && s.primarySkillTreeId == primarySkillTreeId && s.secondarySkillTreeId.IsNullOrEmptyOrWhitespace() && s.isTierBlocker) - 1;
    }

    public override void Load(ThunderRoad.Imbue imbue)
    {
        base.Load(imbue);

        if (!Dye.rainbowModeWasActivatedThisSession)
            return;

        foreach (Effect effect in imbueEffect.effects)
        {
            if (effect is EffectShader effectShader)
                foreach (EffectModule effectModule in imbueEffect.effectData.modules)
                    if (effectModule is EffectModuleShader effectModuleShader)
                        switch (Common.GetQualityLevel())
                        {
                            case QualityLevel.Windows:
                                effectShader.SetMainGradient(ThunderRoad.Utils.CreateGradient(effectModuleShader.mainColorStart, effectModuleShader.mainColorEnd));
                                break;

                            case QualityLevel.Android:
                                effectShader.SetMainGradient(ThunderRoad.Utils.CreateGradient(effectModuleShader.mainNoHdrColorStart, effectModuleShader.mainNoHdrColorEnd));
                                break;
                        }

            if (effect is EffectParticle effectParticle)
                foreach (EffectModule effectModule in imbueEffect.effectData.modules)
                    if (effectModule is EffectModuleParticle effectModuleParticle)
                    {
                        switch (Common.GetQualityLevel())
                        {
                            case QualityLevel.Windows:
                                effectParticle.SetMainGradient(ThunderRoad.Utils.CreateGradient(effectModuleParticle.mainColorStart, effectModuleParticle.mainColorEnd));
                                break;

                            case QualityLevel.Android:
                                effectParticle.SetMainGradient(ThunderRoad.Utils.CreateGradient(effectModuleParticle.mainNoHdrColorStart, effectModuleParticle.mainNoHdrColorEnd));
                                break;
                        }
                    }
        }
    }

    private void OnSpellUnload(SpellData spellInstance, SpellCaster caster)
    {
        if (spellInstance == this && caster.mana.creature.isPlayer)
        {
            spellCaster.mana.OnSpellUnloadEvent -= OnSpellUnload;
            spellCaster.ragdollHand.playerHand.controlHand.OnButtonPressEvent -= OnButtonPressWhileCasting;
        }
    }

    private void OnButtonPressWhileCasting(PlayerControl.Hand.Button button, bool pressed) => onButtonPressed?.Invoke(this, button, pressed, spellCaster.isFiring);

    public override void LoadSkillPassives(int skillCount)
    {
        base.LoadSkillPassives(skillCount);
        AddModifier(this, Modifier.Intensity, 1.0f + intensityPerSkill * skillCount);
        AddModifier(this, Modifier.Efficiency, Mathf.Max(25f, 40f - efficiencyPerSkill * skillCount));
    }

    public override void Fire(bool active)
    {
        base.Fire(active);
        if (active)
        {
            EventManager.InvokeSpellUsed("Crystallic", spellCaster.ragdollHand.creature, spellCaster.side);
            chargeEffect = chargeEffectInstance;
        }
        else currentCharge = 0f;
    }

    public override void Throw(Vector3 velocity)
    {
        base.Throw(velocity);
        int total = 0;
        lastShardshotVelocity = velocity;

        if (Common.IsAndroid)
            spellCaster.ragdollHand.HapticTick(10);

        else
            spellCaster.ragdollHand.PlayHapticClipOver(pulseCurve, 1);

        Vector3 origin = spellCaster.magicSource.position + velocity.normalized * 0.1f;
        var effectInstance = pulseEffectData.Spawn(origin, Quaternion.LookRotation(velocity));
        effectInstance.onEffectFinished += OnEffectFinished;
        InvokeShardshotStart(effectInstance, EventTime.OnStart, velocity);
        lastShardshotTime = Time.time;
        effectInstance.Play();

        List<Shard> shards = new();
        List<(Vector3 pos, Vector3 dir)> shardData = new();
        Quaternion baseRot = Quaternion.LookRotation(velocity);

        for (int i = 0; i < ShardCount; i++)
        {
            float angleRad = ShardCount == 1 ? 0f : Mathf.Lerp(-GetModifier(Modifier.Efficiency), GetModifier(Modifier.Efficiency), (float)i / (ShardCount - 1)) * Mathf.Deg2Rad;
            Vector3 dirLocal = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
            Vector3 direction = (baseRot * dirLocal).normalized;
            shardData.Add((origin + direction * 0.2f, direction));
        }

        shardData.Sort((a, b) => Vector3.Dot(a.pos - origin, spellCaster.magicSource.right).CompareTo(Vector3.Dot(b.pos - origin, spellCaster.magicSource.right)));
        foreach (var (position, direction) in shardData)
        {
            FireShard(shardEffectData, position, direction * (velocity.magnitude * 2.5f), ShardLifetime, 1.0f, shard =>
            {
                total++;
                shards.Add(shard);
                if (total == ShardCount)
                {
                    InvokeShardshotStart(effectInstance, EventTime.OnEnd, velocity, shards);
                    lastShards = shards;
                }
            });
        }

        void OnEffectFinished(EffectInstance effectInstance)
        {
            effectInstance.onEffectFinished -= OnEffectFinished;
            InvokeShardshotEnd(effectInstance);
        }
    }

    public void FireShard(EffectData shardEffect, Vector3 shootPos, Vector3 shootVelocity, float lifetime, float damageMultiplier = 1f, Action<Shard> onSpawned = null, int colliderLayer = default, Ragdoll ignoredRagdoll = null, Collider[] ignoredColliders = null)
    {
        shardItemData.SpawnAsync(shard =>
        {
            shard.ResetColliderCollision();

            if (colliderLayer != default)
                shard.SetColliderLayer(colliderLayer);

            if (ignoredRagdoll != null)
                shard.IgnoreRagdollCollision(ignoredRagdoll);

            if (ignoredColliders != null)
                foreach (var ignoredCollider in ignoredColliders)
                    shard.IgnoreColliderCollision(ignoredCollider);

            shard.SetColliders(false);
            shard.RunAfter(() => shard.SetColliders(true), 0.5f);
            shard.transform.position = shootPos;
            shard.transform.rotation = Quaternion.LookRotation(shootVelocity);
            RagdollHand ragdollHand = imbue?.colliderGroup.collisionHandler.item.lastHandler ?? spellCaster?.ragdollHand;
            if (ragdollHand?.ragdoll) shard.IgnoreRagdollCollision(ragdollHand.ragdoll);
            FloatHandler floatHandler = new FloatHandler();
            floatHandler.Add(this, damageMultiplier);
            foreach (CollisionHandler collisionHandler in shard.collisionHandlers)
                foreach (Damager damager in collisionHandler.damagers)
                {
                    damager.Load(shardDamagerData, collisionHandler);
                    damager.skillDamageMultiplierHandler = floatHandler;
                }

            Shard component = shard.GetComponent<Shard>();
            if (component)
            {
                component.destroyInWater = true;
                component.guidance = GuidanceMode.NonGuided;
                component.speed = 20;
                component.item.lastHandler = ragdollHand;
                component.item.physicBody.useGravity = false;
                component.allowDeflect = false;
                component.imbueSpellCastCharge = this;
                component.Load(this);
                component.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
                component.OnProjectileCollisionEvent += OnProjectileCollisionEvent;
                component.Fire(shootVelocity, shardEffect, imbue?.colliderGroup.collisionHandler.item, imbue?.colliderGroup.collisionHandler.ragdollPart?.ragdoll ?? spellCaster?.ragdollHand?.ragdoll, homing: false);
                onSpawned?.Invoke(component);
                component.item.physicBody.angularVelocity = Vector3.zero;
                component.DelayedDespawn(null, lifetime);
            }
        });
    }

    public override void OnMusketShoot(bool playEffect = true)
    {
        base.OnMusketShoot(playEffect);
        Creature shootingCreature = spellCaster?.mana.creature ?? imbue.imbueCreature ?? Player.currentCreature;
        Item imbueItem = imbue.colliderGroup.collisionHandler.item;
        Transform imbueShoot = imbue.colliderGroup.imbueShoot;

        foreach (ThunderEntity thunderEntity in ThunderEntity.InRadius(imbueShoot.position + imbueShoot.forward.normalized * musketRadius, musketRadius, Filter.AllBut(shootingCreature)))
            if (thunderEntity != imbueItem && thunderEntity != shootingCreature)
            {
                if (thunderEntity is Creature creature && !creature.isPlayer) creature.TryPush(Creature.PushType.Magic, imbueShoot.forward * musketForce, 2);
                thunderEntity.AddExplosionForce(musketForce, imbueShoot.position, musketRadius * 2f, 0.0f, ForceMode.Impulse);
            }

        musketProjectileData.SpawnAsync(projectileItem =>
        {
            projectileItem.transform.localScale = new Vector3(2, 2, 2);
            ItemMagicAreaProjectile projectile = projectileItem.GetComponent<ItemMagicAreaProjectile>();
            foreach (CollisionHandler collisionHandler in projectileItem.collisionHandlers)
            {
                foreach (Damager damager in collisionHandler.damagers)
                    damager.Load(shardDamagerData, collisionHandler);
            }

            projectile.OnCreatureHit += OnCreatureHit;
            projectile.OnHandlerHit += OnHandlerHit;
            projectile.OnHit += OnHit;
            projectile.explosionEffectData = musketProjectileExplosionEffectData;
            projectile.homing = false;
            projectile.guidance = GuidanceMode.NonGuided;
            projectile.doExplosion = true;
            projectile.areaExpandDuration = 0.3f;
            projectile.Fire(imbueShoot.forward * musketShootSpeed, musketProjectileEffectData, imbueItem, shootingCreature.ragdoll, imbueItem.mainHandler.side == Side.Left ? HapticDevice.LeftController : HapticDevice.RightController);
            projectile.item.physicBody.useGravity = false;

            void OnHit(CollisionInstance collision)
            {
                projectile.OnHit -= OnHit;
                foreach (ThunderEntity thunderEntity in ThunderEntity.InRadius(collision.contactPoint, musketRadius))
                    if (thunderEntity is Creature creature)
                    {
                        if (creature.isPlayer)
                            if (Player.local.airHelper.inAir) Player.local.locomotion.physicBody.AddExplosionForce(musketPlayerForce, projectile.transform.position, musketExplosionRadius, 1f, ForceMode.VelocityChange);
                            else
                            {
                                if (!creature.isKilled && !creature.isPlayer) creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                                creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
                                creature.AddExplosionForce(musketExplosionForce, projectile.transform.position, musketExplosionRadius, 0.0f, ForceMode.Impulse);
                            }
                    }
                    else if (thunderEntity is Item item && item != imbueItem && item.mainHandler == null && !item.data.id.Contains("Shard"))
                        item.physicBody.AddExplosionForce(musketExplosionForce, projectile.transform.position, musketExplosionRadius, 0.0f, ForceMode.Impulse);

                if (ThunderRoad.Golem.local == null) return;

                SimpleBreakable breakable = collision.targetCollider.GetComponentInParent<SimpleBreakable>();

                if (breakable != null && (!(breakable is GolemCrystal golemCrystal) || !golemCrystal.shieldActive))
                    breakable.Hit(musketExplosionCrystalDamage);

                foreach (GolemCrystal linkedArenaCrystal in ThunderRoad.Golem.local.linkedArenaCrystals)
                    if (linkedArenaCrystal.transform.position.PointInRadius(collision.contactPoint, musketRadius) && !linkedArenaCrystal.shieldActive)
                        linkedArenaCrystal.Hit(musketExplosionCrystalDamage);

                foreach (GolemCrystal crystal in ThunderRoad.Golem.local.crystals)
                    if (crystal.transform.position.PointInRadius(collision.contactPoint, musketRadius) && !crystal.shieldActive)
                        crystal.Hit(musketExplosionCrystalDamage);

                GetDirectionsInCone(collision.contactNormal, Random.Range(3, 6), 30f, out var points);
                foreach (var direction in points)
                    FireShard(shardEffectData, collision.contactPoint + direction * 0.3f, direction * 12f, ShardLifetime * 1.6f);
            }

            void OnHandlerHit(CollisionInstance collision, CollisionHandler handler)
            {
                projectile.OnHandlerHit -= OnHandlerHit;
                handler.physicBody.AddForce(collision.impactVelocity * 3f, ForceMode.Impulse);
                if (handler.item == null || !handler.item.IsHeld()) return;
                handler.item.ForceUngrabAll();
            }

            void OnCreatureHit(CollisionInstance collision, Creature creature)
            {
                projectile.OnCreatureHit -= OnCreatureHit;
                if (!Player.selfCollision && creature.isPlayer || creature == shootingCreature) return;
                creature.Inflict("Crystallised", GameManager.local, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
                skillHyperdetonation.Detonate(creature, collision.contactPoint);
            }

            void GetDirectionsInCone(Vector3 inDirection, int count, float angle, out Vector3[] points)
            {
                points = new Vector3[count];
                float cosAngle = Mathf.Cos(angle * Mathf.Deg2Rad);

                int currentPoint = 0;
                while (currentPoint < count)
                {
                    Vector3 direction = Random.onUnitSphere;
                    if (Vector3.Dot(direction, inDirection) >= cosAngle)
                    {
                        points[currentPoint] = direction;
                        currentPoint++;
                    }
                }
            }
        }, imbueShoot.position + imbueShoot.forward * 0.1f);
    }

    public override bool OnCrystalSlam(CollisionInstance collisionInstance)
    {
        base.OnCrystalSlam(collisionInstance);
        var instance = staffSlamEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal), null, collisionInstance, true, null, false, collisionInstance.intensity, 0.0f);
        instance?.Play();
        var owner = imbue?.colliderGroup?.collisionHandler?.item?.mainHandler?.creature;
        foreach (var entity in ThunderEntity.InRadius(collisionInstance.contactPoint, staffSlamMaxRadius))
            switch (entity)
            {
                case Creature creature when creature != null && owner != null && creature.factionId != 0 && creature != owner && creature.factionId != owner.factionId:
                {
                    creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    var directionToContact = creature.transform.position - collisionInstance.contactPoint;
                    var distance = directionToContact.magnitude;
                    if (distance > 0)
                    {
                        directionToContact.Normalize();
                        var forceAmount = Mathf.Lerp(staffSlamMinForce, staffSlamMaxForce, 1 - Mathf.Clamp01(distance / staffSlamMaxRadius));
                        creature.AddForce(directionToContact * forceAmount, ForceMode.Impulse);
                        creature.AddForce(Vector3.up * slamUpwardsForceMult, ForceMode.Impulse);
                    }

                    if (allowStaffCrystallisation)
                        creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
                }
                    break;

                case Item item when item != imbue.colliderGroup.collisionHandler.item:
                {
                    var directionToContact = item.transform.position - collisionInstance.contactPoint;
                    var distance = directionToContact.magnitude;
                    if (distance > 0)
                    {
                        directionToContact.Normalize();
                        var forceAmount = Mathf.Lerp(staffSlamMinForce, staffSlamMaxForce, 1 - Mathf.Clamp01(distance / staffSlamMaxRadius));
                        item.AddForce(directionToContact * forceAmount, ForceMode.Impulse);
                        foreach (var colliderGroup in item.colliderGroups)
                            if (colliderGroup != null && colliderGroup.allowImbueEffect && colliderGroup.imbue != null && colliderGroup.collisionHandler.item.holder == null)
                                colliderGroup?.imbue?.Transfer(this, forceAmount, owner);
                    }

                    if (item.breakable is Breakable breakable)
                    {
                        breakable.Explode(80, collisionInstance.contactPoint, staffSlamMaxRadius, 0.25f, ForceMode.Impulse);

                        for (var i = 0; i < breakable.subBrokenItems.Count; ++i)
                        {
                            var subItem = breakable.subBrokenItems[i];
                            var physicBody = subItem.physicBody;

                            if (subItem.breakable is Breakable breakable1)
                                for (var j = 0; j < breakable.subBrokenItems.Count; ++j)
                                {
                                    var subPhysicBody = breakable.subBrokenItems[j].physicBody;
                                    if (subPhysicBody)
                                    {
                                        var forceDirection = subPhysicBody.transform.position - collisionInstance.contactPoint;
                                        forceDirection.Normalize();
                                        subPhysicBody.AddForceAtPosition(forceDirection * 10 * GetModifier(Modifier.Intensity), subPhysicBody.transform.position, ForceMode.Impulse);
                                    }
                                }

                            if (physicBody)
                            {
                                var forceDirection = physicBody.transform.position - collisionInstance.contactPoint;
                                forceDirection.Normalize();
                                physicBody.AddForceAtPosition(forceDirection * 10 * GetModifier(Modifier.Intensity), physicBody.transform.position, ForceMode.Impulse);
                            }
                        }

                        for (var k = 0; k < breakable.subBrokenBodies.Count; ++k)
                        {
                            var subBrokenBody = breakable.subBrokenBodies[k];
                            if (subBrokenBody)
                            {
                                var forceDirection = subBrokenBody.transform.position - collisionInstance.contactPoint;
                                forceDirection.Normalize();
                                subBrokenBody.AddForceAtPosition(forceDirection * 10 * GetModifier(Modifier.Intensity), subBrokenBody.transform.position, ForceMode.Impulse);
                            }
                        }
                    }
                }
                    break;
            }

        return true;
    }

    private void OnProjectileCollisionEvent(ItemMagicProjectile projectile, CollisionInstance collisionInstance)
    {
        var shard = projectile as Shard;
        if (shard.despawnCoroutine != null && shard.despawnOnHit)
        {
            shard.StopCoroutine(shard.despawnCoroutine);
            shard.Lifetime = ShardLifetime;
            shard.ElapsedLifetime = 0f;
        }

        projectile.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
        if (collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && (shard.crystalliseFunc == null || shard.crystalliseFunc.Invoke(creature)) && (spellCaster == null || spellCaster.mana == null || creature != spellCaster.mana.creature && creature.factionId != spellCaster.mana.creature.factionId) && collisionInstance.targetMaterial != null && allowShardCrystallisation)
            creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
    }

    public override bool OnImbueCollisionStart(CollisionInstance collisionInstance)
    {
        if (Time.time - lastTime > cooldown && collisionInstance.impactVelocity.magnitude > 7.5f)
        {
            lastTime = Time.time;
            imbueCollisionEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.targetCollider.transform).Play();
            if (collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && creature != null && creature != spellCaster.mana.creature && collisionInstance.targetMaterial != null && !collisionInstance.targetMaterial.IsMetal() && allowImbueCrystallisation)
                creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
        }

        return base.OnImbueCollisionStart(collisionInstance);
    }

    public override void UpdateImbue(float speedRatio)
    {
        base.UpdateImbue(speedRatio);
        if (imbue && Dye.rainbowMode)
            imbueEffect.SetMainGradient(Utils.MakeShiftingGradient());
    }

    public override void OnSprayStart()
    {
        base.OnSprayStart();
        InvokeSpray(Effect.Step.Start);
    }

    public override void OnSprayLoop()
    {
        base.OnSprayLoop();
        InvokeSpray(Effect.Step.Loop);
    }

    public override void OnSprayStop()
    {
        base.OnSprayStop();
        InvokeSpray(Effect.Step.End);
    }

    public void InvokeShardHit(Shard shard, CollisionInstance collisionInstance) => onShardHit?.Invoke(this, shard, collisionInstance);
    public void InvokeShardSpawn(Shard shard) => onShardSpawn?.Invoke(this, shard);
    public void InvokeShardDespawn(Shard shard) => onShardDespawn?.Invoke(this, shard);
    public void InvokeShardshotStart(EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards = null) => onShardshotStart?.Invoke(this, effectInstance, eventTime, velocity, shards);
    public void InvokeShardshotEnd(EffectInstance effectInstance) => onShardshotEnd?.Invoke(this, effectInstance);

    public void InvokeSpray(Effect.Step step)
    {
        switch (step)
        {
            case Effect.Step.Start:
                onSprayStart?.Invoke(this);
                break;
            case Effect.Step.Loop:
                onSprayLoop?.Invoke(this);
                break;
            case Effect.Step.End:
                onSprayEnd?.Invoke(this);
                break;
        }
    }

    public delegate void ShardHitEvent(SpellCastCrystallic spellCastCrystallic, Shard shard, CollisionInstance collisionInstance);

    public delegate void ShardEvent(SpellCastCrystallic spellCastCrystallic, Shard shard);

    public delegate void ShardshotStartEvent(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards = null);

    public delegate void ShardshotEndEvent(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance);

    public delegate void ButtonDelegate(SpellCastCrystallic spellCastCrystallic, PlayerControl.Hand.Button button, bool pressed, bool casting);

    public delegate void SprayDelegate(SpellCastCrystallic spellCastCrystallic);

    #endif
}