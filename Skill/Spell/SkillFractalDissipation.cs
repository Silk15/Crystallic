using System;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill;
using TriInspector;
using UnityEngine;

namespace Crystallic.Skill.Spell;

public class SkillFractalDissipation : SpellSkillData
{
    #if !SDK
    [ModOption("Dissipation Enemy Damage", "Controls the amount of damage dealt to enemies."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Fractal Dissipation", 17)]
    public static float dissipationEnemyDamage = 5f;
    
    [ModOption("Dissipation Radius", "Controls the radius of the detonation."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Fractal Dissipation", 17)]
    public static float dissipationRadius = 2f;
    
    [ModOption("Dissipation Force", "Controls the force applied to enemies and items."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Fractal Dissipation", 17)]
    public static float dissipationForce = 20f;
    
    [ModOption("Dissipation Breakable Damage", "Controls the amount of damage dealt to breakable items like crates and ceramics."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Fractal Dissipation", 17)]
    public static float dissipationBreakForce = 10f;
    #endif
    
    [NonSerialized]
    public EffectData dissipationEffectData;
        
    [Dropdown(nameof(GetAllEffectID))]
    public string dissipationEffectId;
    
    [NonSerialized]
    public Dictionary<Creature, float> lastPushTimes = new();

    #if !SDK
    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        lastPushTimes.Clear();
        dissipationEffectData = Catalog.GetData<EffectData>(dissipationEffectId);
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (spell is not SpellCastCrystallic spellCastCrystallic)
            return;
        
        spellCastCrystallic.onShardDespawn -= OnShardDespawn;
        spellCastCrystallic.onShardDespawn += OnShardDespawn;
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (spell is not SpellCastCrystallic spellCastCrystallic)
            return;
        
        spellCastCrystallic.onShardDespawn -= OnShardDespawn;
    }

    private void OnShardDespawn(SpellCastCrystallic spellCastCrystallic, Shard shard)
    {
        if (shard.hasCollided || !shard.canImplode)
            return;
        
        dissipationEffectData.Spawn(shard.transform.position, Quaternion.identity).Play();

        foreach ((ThunderEntity, Vector3) closestPoint in ThunderEntity.InRadiusClosestPoint(shard.transform.position, dissipationRadius))
        {
            switch (closestPoint.Item1)
            {
                case Creature creature when !creature.isPlayer && creature.IsEnemy(spellCastCrystallic.spellCaster.mana.creature):
                    if (!lastPushTimes.ContainsKey(creature) || Time.time - lastPushTimes[creature] >= 0.5f)
                    {
                        creature.TryPush(Creature.PushType.Magic, (shard.transform.position - creature.ragdoll.targetPart.transform.position).normalized, 1);
                        lastPushTimes[creature] = Time.time;
                        
                        var brainModuleSpeak = creature.brain.instance.GetModule<BrainModuleSpeak>();
                        if (!creature.isKilled)
                            brainModuleSpeak.Play(BrainModuleSpeak.hashHit, false);
                    
                        creature.AddExplosionForce(dissipationForce, shard.transform.position, dissipationRadius, 0.5f, ForceMode.Impulse);
                        creature.Damage(dissipationEnemyDamage);
                    }
                    break;
                
                case Item item when !item.TryGetComponent(out ItemMagicProjectile _):
                    item.AddExplosionForce(dissipationForce, shard.transform.position, dissipationRadius, 0.5f, ForceMode.Impulse);

                    Breakable breakable = item.breakable;
                    if (breakable != null && !breakable.contactBreakOnly)
                        item.breakable.Explode(dissipationBreakForce, shard.transform.position, dissipationRadius, 0.0f, ForceMode.Impulse);
                    break;
            }
        }
    }
    #endif
}