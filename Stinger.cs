using System.Collections.Generic;
using Crystallic.AI;
using Crystallic.Skill;
using Crystallic.Skill.Spell;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class Stinger : ThunderBehaviour
{
    public delegate void OnStingerStab(Stinger stinger, Damager damager, CollisionInstance collisionInstance, Creature hitCreature = null);

    public delegate void StingerEvent(Stinger stinger);

    public static HashSet<Stinger> all = new();
    public static string projectileItemId = "CrystallicProjectile";
    private static ItemData itemData;
    public Item item;
    public Creature creature;
    public EffectData hitEffectData;
    public Lerper lerper;
    private EffectInstance projectileEffect;
    private EffectInstance projectileTrailEffect;
    public SpellCastCrystallic spellCastCrystallic;
    public static event StingerEvent onStingerSpawn;
    public event OnStingerStab onStingerStab;

    public static Stinger SpawnStinger(EffectData effectData, EffectData trailEffectData, EffectData hitEffectData, Vector3 position, Quaternion rotation, Vector3 velocity, float lifetime, SpellCastCrystallic spellCastCrystallic, Creature owner = null, bool forceReleaseOnSpawn = false)
    {
        Stinger stinger = null;
        if (itemData == null) itemData = Catalog.GetData<ItemData>(projectileItemId);
        itemData.SpawnAsync(item =>
        {
            item.Despawn(10);
            if (owner && !owner.isPlayer)
            {
                var direction = (Player.currentCreature.ragdoll.targetPart.transform.position - item.transform.position).normalized;
                item.AddForce(direction * velocity.magnitude * 3f, ForceMode.Impulse);
            }
            else if (owner == null)
            {
                owner = Player.currentCreature;
            }

            item.AddForce(item.transform.forward * (velocity.magnitude * 2.15f), ForceMode.Impulse);
            item.gameObject.GetOrAddComponent<Stinger>().Init(item, effectData, trailEffectData, hitEffectData, owner, spellCastCrystallic, forceReleaseOnSpawn);
        }, position, rotation);
        return stinger;
    }

    public void Init(Item item, EffectData effectData, EffectData trailEffectData, EffectData hitEffectData, Creature owner, SpellCastCrystallic spellCastCrystallic, bool forceReleaseOnSpawn = false)
    {
        all.Add(this);
        lerper = new Lerper();
        item.OnDespawnEvent += OnDespawnEvent;
        SetColor(Color.white, "Crystallic");
        this.item = item;
        creature = owner;
        this.spellCastCrystallic = spellCastCrystallic;
        item.GetComponentInChildren<Damager>().OnPenetrateEvent += OnPenetrateEvent;
        projectileEffect = effectData?.Spawn(item.transform);
        projectileTrailEffect = trailEffectData?.Spawn(item.transform);
        this.hitEffectData = hitEffectData;
        projectileEffect?.Play();
        projectileTrailEffect?.Play();
        onStingerSpawn?.Invoke(this);
        if (forceReleaseOnSpawn) SkillHyperintensity.ForceInvokeRelease(null);
    }

    private void OnDespawnEvent(EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart) all.Remove(this);
    }

    private void OnPenetrateEvent(Damager damager, CollisionInstance collision, EventTime time)
    {
        if (time == EventTime.OnStart) return;
        var item = collision?.sourceColliderGroup?.collisionHandler?.item;
        var hitItem = collision?.targetColliderGroup?.collisionHandler?.item;
        var creature = collision?.targetColliderGroup?.collisionHandler?.Entity as Creature;
        projectileTrailEffect.End();
        if (!item) return;
        damager.OnPenetrateEvent -= OnPenetrateEvent;
        onStingerStab?.Invoke(this, damager, collision, creature);
        if (!hitItem && !creature) this.RunAfter(() => { item.physicBody.rigidBody.isKinematic = true; }, 0.05f);
        if (collision.impactVelocity.magnitude > 3.5f)
        {
            var hitEffectInstance = hitEffectData?.Spawn(collision.contactPoint, Quaternion.LookRotation(collision.contactNormal, collision.sourceColliderGroup.transform.up), collision.targetCollider.transform);
            hitEffectInstance.Play();
            hitEffectInstance.SetColorImmediate(lerper.currentColor);
        }

        if (creature && creature != this.creature && !collision.targetMaterial.isMetal)
        {
            var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            if (!creature.isPlayer) creature.AddExplosionForce(60, collision.contactPoint, 3, 0.1f, ForceMode.Impulse);
            brainModuleCrystal.Crystallise(5);
            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, lerper.currentSpellId), lerper.currentSpellId);
        }
        if (collision.targetCollider.GetComponentInParent<GolemBrain>() != null && Golem.local.Brain().TryGetModule<GolemBrainModuleCrystal>(out var brainModuleGolemCrystal)) brainModuleGolemCrystal.Crystallise(5);
    }

    public void SetColor(Color color, string target, float time = 0.1f)
    {
        if (lerper.isLerping) return;
        var systems = new List<ParticleSystem>();
        systems.AddRange(projectileEffect.GetParticleSystems());
        systems.AddRange(projectileTrailEffect.GetParticleSystems());
        lerper.SetColor(color, systems.ToArray(), target, time);
    }
}