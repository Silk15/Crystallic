using System.Collections.Generic;
using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using ThunderRoad.Skill.SpellPower;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillExplosiveSwarm : SkillSlowTimeData
{
    private string statusId;
    private string mixId = "Fire";
    public EffectData projectileEffectData;
    public SkillRemoteDetonation remoteDetonationSkill;
    public string remoteDetonationSkillId = "RemoteDetonation";
    protected SkillRemoteDetonation skillRemoteDetonate;
    protected List<ItemMagicProjectile> activeProjectiles = new();
    public EffectData detonateEffectData;
    public string detonateEffectId = "RemoteDetonation";

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        projectileEffectData = Catalog.GetData<EffectData>("CrystallicFireball");
        remoteDetonationSkill = Catalog.GetData<SkillRemoteDetonation>(remoteDetonationSkillId);
        detonateEffectData = Catalog.GetData<EffectData>(detonateEffectId);
    }

    public override void OnLateSkillsLoaded(SkillData skillData, Creature creature)
    {
        base.OnLateSkillsLoaded(skillData, creature);
        if (skillRemoteDetonate != null)
        {
            skillRemoteDetonate.OnDetonateHitCreatureEvent -= OnDetonateHitCreatureEvent;
            skillRemoteDetonate = null;
        }
        if (!creature.TryGetSkill(remoteDetonationSkill, out skillRemoteDetonate)) return;
        skillRemoteDetonate.OnDetonateHitCreatureEvent -= OnDetonateHitCreatureEvent;
        skillRemoteDetonate.OnDetonateHitCreatureEvent += OnDetonateHitCreatureEvent;
        SkillBoltAbsorption.onBoltAbsorptionTriggered += OnBoltAbsorptionTriggered;
        SkillBoltAbsorption.onEnd += OnEnd;
    }
    

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        if (skillRemoteDetonate == null) return;
        skillRemoteDetonate.OnDetonateHitCreatureEvent -= OnDetonateHitCreatureEvent;
        SkillBoltAbsorption.onBoltAbsorptionTriggered -= OnBoltAbsorptionTriggered;
        SkillBoltAbsorption.onEnd -= OnEnd;
        skillRemoteDetonate = null;
    }

    public override void OnSlowMotionEnter(SpellPowerSlowTime spellPowerSlowTime, float scale)
    {
        base.OnSlowMotionEnter(spellPowerSlowTime, scale);
        mixId = "Mind";
        statusId = "Slowed";
        for (int i = 0; i < activeProjectiles.Count; i++) activeProjectiles[i].effectInstance.SetColorImmediate(Dye.GetEvaluatedColor("Mind", "Mind"));
    }

    public override void OnSlowMotionExit(SpellPowerSlowTime spellPowerSlowTime)
    {
        base.OnSlowMotionExit(spellPowerSlowTime);
        mixId = "Fire";
        statusId = "Burning";
        for (int i = 0; i < activeProjectiles.Count; i++) activeProjectiles[i].effectInstance.SetColorImmediate(Color.white);
    }

    private void OnEnd()
    {
        mixId = "Fire";
        statusId = "Burning";
        for (int i = 0; i < activeProjectiles.Count; i++) activeProjectiles[i].effectInstance.SetColorImmediate(Color.white);
    }

    private void OnDetonateHitCreatureEvent(ItemMagicProjectile projectile, SpellCastProjectile spell, ThunderEntity hitEntity, Vector3 closestPoint, float distance)
    {
        if (hitEntity.IsBurning)
        {
            for (int i = 0; i < Random.Range(1, 3); i++) spell.ShootFireSpark(projectileEffectData, closestPoint + Vector3.up * Random.Range(0.1f, 0.3f), (Vector3.up * 2f + Random.insideUnitSphere) * 2.5f, onSpawnEvent: OnSpawnEvent);
        }
    }

    private void OnSpawnEvent(ItemMagicProjectile projectile)
    {
        activeProjectiles.Add(projectile);
        projectile.OnProjectileCollisionEvent += OnProjectileCollisionEvent;
        projectile.RunAfter(() => { projectile.homing = true; }, 0.2f);
    }

    private void OnBoltAbsorptionTriggered(Color color, SpellCastCrystallic main, SpellCastLightning other)
    {
        mixId = other.id;
        mixId = "Electrocute";
        for (int i = 0; i < activeProjectiles.Count; i++) activeProjectiles[i].effectInstance.SetColorImmediate(color);
    }

    private void OnProjectileCollisionEvent(ItemMagicProjectile projectile, CollisionInstance collisionInstance)
    {
        activeProjectiles.Remove(projectile);
        projectile.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
        if (collisionInstance?.targetColliderGroup?.collisionHandler?.Entity is Creature creature && !creature.isPlayer)
        {
            detonateEffectData?.Spawn(creature.ragdoll.targetPart.transform).Play();
            var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            brainModuleCrystal.Crystallise(5);
            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, mixId), mixId);
            if (Player.currentCreature.HasSkill("OverchargedCore") && collisionInstance.impactVelocity.magnitude > 8) SkillOverchargedCore.Detonate(creature, Dye.GetEvaluatedColor(mixId, mixId));
            creature.Inflict(statusId, this, parameter: 100);
        }
    }
}