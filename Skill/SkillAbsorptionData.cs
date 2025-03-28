using System.Collections;
using System.Collections.Generic;
using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillAbsorptionData : SpellSkillData
{
    [ModOption("Min Absorption Charge", "The minimum charge a spell has to be to start an absorption."), ModOptionCategory("Absorption", 21), ModOptionSlider, ModOptionFloatValues(0.1f, 1, 0.1f)]
    public static float absorptionMinCharge = 1f;
    public Dictionary<SpellCastCharge, Coroutine> chargingSpells = new();
    public string spellId;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        chargingSpells.Clear();
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCharge spellCastCharge)) return;
        spellCastCharge.OnSpellCastEvent += OnSpellCastEvent;
        spellCastCharge.OnSpellStopEvent += OnSpellStopEvent;
    }

    protected virtual void OnSprayStartWhileAbsorbing(SpellCastCrystallic spellCastCrystallic) { }

    protected virtual void OnSprayLoopWhileAbsorbing(SpellCastCrystallic spellCastCrystallic) { }

    private void OnSprayEndWhileAbsorbing(SpellCastCrystallic spellCastCrystallic) { }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCharge spellCastCharge)) return;
        spellCastCharge.OnSpellCastEvent -= OnSpellCastEvent;
        spellCastCharge.OnSpellStopEvent -= OnSpellStopEvent;
    }

    protected virtual void OnShardshotStartWhileAbsorbing(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance)
    {
        spellCastCrystallic.OnShardHit += OnShardshotHitWhileAbsorbing;
    }

    protected virtual void OnShardshotHitWhileAbsorbing(SpellCastCrystallic spellCastCrystallic, ThunderEntity entity, SpellCastCrystallic.ShardshotHit hitInfo) { }

    protected virtual void OnShardshotEndWhileAbsorbing(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance)
    {
        spellCastCrystallic.OnShardHit -= OnShardshotHitWhileAbsorbing;
    }

    public IEnumerator ChargeRoutine(SpellCastCharge spellCastCharge)
    {
        while (!Mathf.Approximately(spellCastCharge.currentCharge, absorptionMinCharge)) yield return null;
        Absorb(spellCastCharge);
    }

    public void Absorb(SpellCastCharge spell)
    {
        if (spell != null && spell.spellCaster != null && spell.spellCaster.isFiring && !(spell?.id == "Crystallic" && spell?.spellCaster?.other != null && !string.IsNullOrEmpty(spell?.spellCaster?.other?.spellInstance?.id) && spell.spellCaster?.other?.spellInstance?.id == "Crystallic"))
        {
            PlayAbsorbEffect(spell);
            chargingSpells.Remove(spell);
            if (spell.id == spellId)
            {
                Stinger.onStingerSpawn += OnStingerSpawnWithSpell;
                if (spell.spellCaster.other.isFiring && spell.spellCaster.other.spellInstance.id == "Crystallic") DyeSpell(spell.spellCaster.other.spellInstance as SpellCastCrystallic, true);
            }
            else if (spell.id == "Crystallic" && spell.spellCaster.other.isFiring && spell.spellCaster.other.spellInstance.id == spellId) DyeSpell(spell as SpellCastCrystallic, true);
            if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
            spellCastCrystallic.onSprayStart += OnSprayStartWhileAbsorbing;
            spellCastCrystallic.onSprayLoop += OnSprayLoopWhileAbsorbing;
            spellCastCrystallic.onSprayEnd += OnSprayEndWhileAbsorbing;
            spellCastCrystallic.OnShardshotStart += OnShardshotStartWhileAbsorbing;
            spellCastCrystallic.OnShardshotEnd += OnShardshotEndWhileAbsorbing;
        }
    }

    protected virtual void OnSpellCastEvent(SpellCastCharge spell)
    {
        if (!chargingSpells.ContainsKey(spell)) chargingSpells.Add(spell, GameManager.local.StartCoroutine(ChargeRoutine(spell)));
    }

    protected virtual void OnSpellStopEvent(SpellCastCharge spell)
    {
        if (chargingSpells.TryGetValue(spell, out var chargingSpell) && chargingSpell != null)
        {
            GameManager.local.StopCoroutine(chargingSpell);
            chargingSpells.Remove(spell);
        }

        if (spell.id == spellId)
        {
            Stinger.onStingerSpawn -= OnStingerSpawnWithSpell;
            if (spell.spellCaster.other.isFiring && spell.spellCaster.other.spellInstance.id == "Crystallic") DyeSpell(spell.spellCaster.other.spellInstance as SpellCastCrystallic, false);
        }
        else if (spell.id == "Crystallic")
        {
            DyeSpell(spell as SpellCastCrystallic, false);
        }

        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.onSprayStart -= OnSprayStartWhileAbsorbing;
        spellCastCrystallic.onSprayLoop -= OnSprayLoopWhileAbsorbing;
        spellCastCrystallic.onSprayEnd -= OnSprayEndWhileAbsorbing;
        spellCastCrystallic.OnShardshotStart -= OnShardshotStartWhileAbsorbing;
        spellCastCrystallic.OnShardshotEnd -= OnShardshotEndWhileAbsorbing;
    }

    protected virtual void OnStingerSpawnWithSpell(Stinger stinger)
    {
        stinger.SetColor(Dye.GetEvaluatedColor(spellId, spellId), spellId, 0.01f);
        stinger.onStingerStab += OnStingerStab;
    }

    protected virtual void OnStingerStab(Stinger stinger, Damager damager, CollisionInstance collisionInstance, Creature hitCreature)
    {
        stinger.onStingerStab -= OnStingerStab;
        if (hitCreature)
        {
            var brainModuleCrystal = hitCreature.brain.instance.GetModule<BrainModuleCrystal>();
            brainModuleCrystal.Crystallise(5);
            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, spellId), spellId);
        }
    }

    public virtual void PlayAbsorbEffect(SpellCastCharge main)
    {
        if (main.spellCaster.isFiring && main.spellCaster.other.isFiring)
        {
            if (main.id == "Crystallic")
            {
                var other = main.spellCaster.other.spellInstance as SpellCastCharge;
                other?.readyEffectData.Spawn(main.spellCaster.Orb).Play();
            }
            else if (main.spellCaster.other.spellInstance.id == "Crystallic")
            {
                var other = main.spellCaster.other.spellInstance as SpellCastCharge;
                main.readyEffectData.Spawn(other?.spellCaster.Orb).Play();
            }
        }
    }

    public virtual void DyeSpell(SpellCastCrystallic spellCastCrystallic, bool mix)
    {
        if (spellCastCrystallic != null)
        {
            if (mix) spellCastCrystallic?.SetColor(Dye.GetEvaluatedColor(spellId, spellId), spellId, 0.05f);
            else spellCastCrystallic.SetColor(Dye.GetEvaluatedColor("Crystallic", "Crystallic"), "Crystallic", 0.05f);
        }
    }
}