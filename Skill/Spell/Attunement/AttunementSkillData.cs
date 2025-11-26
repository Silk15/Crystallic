using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill.Spell.Attunement;

public abstract class AttunementSkillData : SpellSkillData
{
    public Color colorModifier;
    public string runeMeshAddress;
    public string spellId;
    public bool injectInCast = true;
    public bool injectInThrow = true;
    public bool injectInShards = true;
    public bool useCustomAttunementInvocation = false;

    protected Dictionary<SpellCastCharge, Coroutine> activeCoroutines = new();
    public bool wasAttunedLastThrow = false;
    protected bool allowAttunement;
    protected int spellHashId;
    protected int crystallicHashId;
    protected Mesh mesh;

    public bool IsAttuned { get; protected set; }

    public event AttunementDelegate onAttunementStart;
    public event AttunementDelegate onAttunementEnd;

    public delegate void AttunementDelegate(SpellCastCrystallic spellCastCrystallic, SpellCastCharge other);

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        spellHashId = Catalog.GetData<SpellCastCharge>(spellId)?.hashId ?? 0;
        crystallicHashId = Catalog.GetData<SpellCastCrystallic>("Crystallic")?.hashId ?? 0;
    }

    public override IEnumerator LoadAddressableAssetsCoroutine() => Catalog.LoadAssetCoroutine<Mesh>(runeMeshAddress, m => mesh = m, $"{id} Mesh Asset");

    public override void ReleaseAddressableAssets()
    {
        base.ReleaseAddressableAssets();
        Catalog.ReleaseAsset(mesh);
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (spell is not SpellCastCharge spellCastCharge || caster == null || useCustomAttunementInvocation) return;
        if (spellCastCharge.hashId == spellHashId || spell.hashId == crystallicHashId)
        {
            spellCastCharge.OnSpellCastEvent += OnSpellCast;
            spellCastCharge.OnSpellStopEvent += OnSpellStop;
            if (spellCastCharge is SpellCastCrystallic spellCastCrystallic)
                spellCastCrystallic.OnSpellThrowEvent += OnSpellThrowEvent;
        }
    }

    private void OnSpellThrowEvent(SpellCastCharge spell, Vector3 velocity) => wasAttunedLastThrow = IsAttuned;

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (spell is not SpellCastCharge spellCastCharge || caster == null || useCustomAttunementInvocation) return;
        if (spellCastCharge.hashId == spellHashId || spell.hashId == crystallicHashId)
        {
            spellCastCharge.OnSpellCastEvent -= OnSpellCast;
            spellCastCharge.OnSpellStopEvent -= OnSpellStop;
            SpellCastCrystallic crystallic;
            SpellCastCharge other;
            if (spell is SpellCastCrystallic spellCastCrystallic)
            {
                crystallic = spellCastCrystallic;
                other = spellCastCharge.spellCaster.other.spellInstance as SpellCastCharge;
                spellCastCrystallic.OnSpellThrowEvent -= OnSpellThrowEvent;

            }
            else
            {
                crystallic = spellCastCharge.spellCaster.other.spellInstance as SpellCastCrystallic;
                other = spellCastCharge;
            }

            if (crystallic != null && other != null && other.hashId == spellHashId && crystallic.hashId == crystallicHashId)
            {
                OnAttunementStop(crystallic, other);
                allowAttunement = false;
            }
        }
    }

    private void OnSpellCast(SpellCastCharge spell)
    {
        if (activeCoroutines.TryGetValue(spell, out var coroutine)) GameManager.local.StopCoroutine(coroutine);
        activeCoroutines.Add(spell, GameManager.local.StartCoroutine(ChargeRoutine(spell, OnSpellReady)));
    }

    private void OnSpellStop(SpellCastCharge spell)
    {
        if (activeCoroutines.TryGetValue(spell, out var coroutine))
        {
            GameManager.local.StopCoroutine(coroutine);
            activeCoroutines.Remove(spell);
        }

        if (!allowAttunement) return;
        SpellCastCrystallic crystallic;
        SpellCastCharge other;
        if (spell is SpellCastCrystallic c)
        {
            crystallic = c;
            other = spell.spellCaster.other.spellInstance as SpellCastCharge;
        }
        else
        {
            crystallic = spell.spellCaster.other.spellInstance as SpellCastCrystallic;
            other = spell;
        }

        if (crystallic != null && other != null && other.hashId == spellHashId && crystallic.hashId == crystallicHashId)
        {
            crystallic.spellCaster.RunAfter(() => OnAttunementStop(crystallic, other), 0.05f);
            allowAttunement = false;
        }
    }

    private void OnSpellReady(SpellCastCharge spell)
    {
        allowAttunement = true;
        if (IsAttuned) return;
        SpellCastCrystallic crystallic;
        SpellCastCharge other;
        if (spell is SpellCastCrystallic c)
        {
            crystallic = c;
            other = spell.spellCaster.other.spellInstance as SpellCastCharge;
        }
        else
        {
            crystallic = spell.spellCaster.other.spellInstance as SpellCastCrystallic;
            other = spell;
        }

        if (crystallic != null && allowAttunement && other != null && crystallic.spellCaster.isFiring && other.spellCaster.isFiring && other.hashId == spellHashId && crystallic.hashId == crystallicHashId)
        {
            OnAttunementStart(crystallic, other);
            IsAttuned = true;
        }
    }

    protected virtual void OnAttunementStart(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        onAttunementStart?.Invoke(crystallic, other);
        if (crystallic != null)
        {
            if (injectInCast) ReflectiveParticles.Inject(crystallic.chargeEffect, $"attunement{id}", colorModifier);
            crystallic.onShardshotStart += OnShardshotStart;
        }
    }

    private void OnShardshotStart(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance, EventTime eventTime, Vector3 velocity, List<Shard> shards)
    {
        if (eventTime == EventTime.OnStart || !injectInThrow) return;
        ReflectiveParticles.Inject(effectInstance, $"attunementcast{id}", colorModifier);

        foreach (Shard shard in shards)
        {
            ReflectiveParticles.Inject(shard.effectInstance, $"attunementshard{shards.IndexOf(shard)}{id}", colorModifier);
            shard.onDespawn += OnShardDespawn;
        }

        foreach (EffectParticle effectParticle in effectInstance.effects.OfType<EffectParticle>())
        {
            if (effectParticle.childs.Select(c => c.particleSystem).FirstOrDefault(p => p.gameObject.name == "core") is ParticleSystem particleSystem)
                particleSystem.gameObject.GetOrAddComponent<AttunementRuneBehaviour>().Init(mesh);
        }

        shards[0].RunAfter(() => ReflectiveParticles.Remove(effectInstance, $"attunementcast{id}"), 1);

        void OnShardDespawn(Shard shard, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                ReflectiveParticles.Remove(shard.effectInstance, $"attunementshard{shards.IndexOf(shard)}{id}");
                shard.onDespawn -= OnShardDespawn;
            }
        }
    }

    protected virtual void OnAttunementStop(SpellCastCrystallic crystallic, SpellCastCharge other)
    {
        onAttunementEnd?.Invoke(crystallic, other);
        if (crystallic != null && injectInCast)
        {
            ReflectiveParticles.Remove(crystallic.chargeEffect, $"attunement{id}");
            crystallic.onShardshotStart -= OnShardshotStart;
        }

        IsAttuned = false;
    }

    protected virtual IEnumerator ChargeRoutine(SpellCastCharge spellCastCharge, Action<SpellCastCharge> onComplete)
    {
        while (spellCastCharge.currentCharge < spellCastCharge.ReadyThreshold && !spellCastCharge.Ready)
        {
            if (!spellCastCharge.spellCaster.isFiring) yield break;
            yield return Yielders.EndOfFrame;
        }

        onComplete?.Invoke(spellCastCharge);
    }
}