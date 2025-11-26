using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill.Spell;

public class SkillShardImplosion : SpellSkillData
{
        
    [ModOption("Enemy Damage", "Controls the amount of damage dealt to enemies"), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Shard Implosion", 10)]
    public static float enemyDamage = 20f;
    
    [ModOption("Implosion Radius", "Controls the radius of the implosion"), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Shard Implosion", 10)]
    public static float radius = 3f;
    
    [ModOption("Implosion Force", "Controls the force applied to each entity around the implosion point."), ModOptionFloatValues(0f, 1000f, 1f), ModOptionSlider, ModOptionCategory("Shard Implosion", 10)]
    public static float force = 120f;
    
    [ModOption("Implosion Break Force", "Controls the amount of force applied to breakable items, increases the explosion force."), ModOptionFloatValues(0f, 1000f, 1f), ModOptionSlider, ModOptionCategory("Shard Implosion", 10)]
    public static float breakForce = 50f;
    
    public Dictionary<Side, float> lastImplosionTimes = new();
    public EffectData implosionEffectData;
    public string implosionEffectId;
    
    public ForceMode forceMode = ForceMode.Impulse;

    public event OnImplode onImplode;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        lastImplosionTimes.FillWithDefault(1f);
        implosionEffectData = Catalog.GetData<EffectData>(implosionEffectId);
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (spell is not SpellCastCrystallic spellCastCrystallic) return;
        spellCastCrystallic.onButtonPressed -= OnButtonPressed;
        spellCastCrystallic.onButtonPressed += OnButtonPressed;
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (spell is not SpellCastCrystallic spellCastCrystallic) return;
        spellCastCrystallic.onButtonPressed -= OnButtonPressed;
    }
    
    private void OnButtonPressed(SpellCastCrystallic spellCastCrystallic, PlayerControl.Hand.Button button, bool pressed, bool casting)
    {
        if (casting && pressed && button == PlayerControl.Hand.Button.Use && Time.time - spellCastCrystallic.lastShardshotTime < spellCastCrystallic.ShardLifetime && spellCastCrystallic.lastShards.Any(s => !s.hasCollided))
        {
            if (Time.time - lastImplosionTimes[spellCastCrystallic.spellCaster.side] < spellCastCrystallic.ShardLifetime) return;
            lastImplosionTimes[spellCastCrystallic.spellCaster.side] = Time.time;

            float average = spellCastCrystallic.lastShards.Select(s => Vector3.Distance(s.transform.position, spellCastCrystallic.spellCaster.Orb.position)).Average();
            Vector3 position = spellCastCrystallic.spellCaster.Orb.position + spellCastCrystallic.lastShardshotVelocity.normalized * average;
            EffectInstance effectInstance = implosionEffectData.Spawn(position, Quaternion.identity);
            effectInstance.Play();

            foreach (Shard shard in spellCastCrystallic.lastShards.Where(s => !s.hasCollided))
            {
                shard.StartCoroutine(DespawnWhenCloseCoroutine(shard, position));
                shard.item.physicBody.rigidBody.velocity = (position - shard.transform.position).normalized * 10f;
                foreach (Shard other in spellCastCrystallic.lastShards)
                {
                    if (other == shard) continue;
                    shard.item.IgnoreItemCollision(other.item);
                }
            }

            (ThunderEntity, Vector3)[] entities = ThunderEntity.InRadiusClosestPoint(position, radius).ToArray();
            foreach ((ThunderEntity thunderEntity, Vector3 closestPoint) in entities)
            {
                float distanceMagnitude = (closestPoint - position).magnitude;
                switch (thunderEntity)
                {
                    case Creature creature:
                        float scaler = Mathf.InverseLerp(radius, 0.0f, distanceMagnitude);
                        if (creature.isPlayer)
                            break;
                        
                        creature.Damage(enemyDamage * scaler);
                        creature.ragdoll.targetPart.physicBody.AddExplosionForce(force, position, radius, 0.5f, forceMode);
                        creature.Inflict("Crystallised", this, 5, parameter: new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Crystallic"), "Crystallic"));
                        break;
                    
                    case Item item when !item.GetComponent<Shard>():
                        item.physicBody.AddExplosionForce(force, position, radius, 0.5f, forceMode);
                        Breakable breakable = item.breakable;
                        if (breakable != null && !breakable.contactBreakOnly)
                            item.breakable.Explode(breakForce, position, radius, 0.0f, forceMode);
                        break;
                    
                    case GolemController golemController when golemController.isAwake:
                        golemController.StaggerImpact(position);
                        break;
                }
            }
            
            onImplode?.Invoke(spellCastCrystallic, position, effectInstance, entities);
        }
    }

    public IEnumerator DespawnWhenCloseCoroutine(Shard shard, Vector3 position)
    {
        while (Vector3.Distance(shard.transform.position, position) > 0.1f)
            yield return Yielders.EndOfFrame;
        shard.DelayedDespawn(null, 0.01f);
    }

    public delegate void OnImplode(SpellCastCrystallic spellCastCrystallic, Vector3 position, EffectInstance effectInstance, (ThunderEntity, Vector3)[] hitEntities);
}