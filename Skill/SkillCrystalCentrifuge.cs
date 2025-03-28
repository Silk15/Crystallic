using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crystallic.AI;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalCentrifuge : SkillData
{
    public List<SkillSpellPair> skillSpellPairs;
    public AnimationCurve fullyChargedCurve;
    public string overchargedEffectId;
    public EffectData overchargedEffectData;
    public string overchargeEffectId;
    public EffectData overchargeEffectData;
    public Dictionary<Item, Coroutine> runningRoutines = new();
    public Dictionary<Item, EffectInstance> runningEffectInstances = new();

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        overchargedEffectData = Catalog.GetData<EffectData>(overchargedEffectId);
        overchargeEffectData = Catalog.GetData<EffectData>(overchargeEffectId);
    }

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        Item.OnItemSpawn -= OnItemSpawn;
        Item.OnItemSpawn += OnItemSpawn;
        Item.OnItemDespawn -= OnItemDespawn;
        Item.OnItemDespawn += OnItemDespawn;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        Item.OnItemSpawn -= OnItemSpawn;
        Item.OnItemDespawn -= OnItemDespawn;
    }

    private void OnItemSpawn(Item item)
    {
        item.OnTKSpinStart += OnTKSpinStart;
        item.OnTKSpinEnd += OnTKSpinEnd;
    }

    private void OnItemDespawn(Item item)
    {
        item.OnTKSpinStart -= OnTKSpinStart;
        item.OnTKSpinEnd -= OnTKSpinEnd;
    }

    private void OnTKSpinStart(Handle held, bool spinning, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart || !spinning) return;
        for (int i = 0; i < skillSpellPairs.Count; i++)
            if (held.item.imbues.HasImbue(skillSpellPairs[i].spellId) && (Player.currentCreature.HasSkill(skillSpellPairs[i].skillId) || skillSpellPairs[i].skillId == null))
            {
                Imbue imbue = held.item.imbues.First(imbue1 => imbue1.spellCastBase.id == skillSpellPairs[i].spellId);
                runningRoutines.Add(held.item, held.item.StartCoroutine(SpinRoutine(held.item, imbue)));
                held.item.SetVariable("Spinning", true);
            }
    }

    private void OnTKSpinEnd(Handle held, bool spinning, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart || spinning) return;
        if (runningRoutines.ContainsKey(held.item) && runningRoutines[held.item] != null)
        {
            held.item.StopCoroutine(runningRoutines[held.item]);
            runningRoutines.Remove(held.item);
        }

        held.item.SetVariable("Spinning", false);
    }

    public IEnumerator SpinRoutine(Item item, Imbue imbue)
    {
        while (item.GetVariable<bool>("Spinning"))
        {
            float currentCharge = item.GetVariable<float>("SpinCharge");
            if (currentCharge >= 1f) yield break;
            foreach (SpellCaster spellCaster in item.tkHandlers) spellCaster.ragdollHand.playerHand.controlHand.HapticLoop(this, currentCharge, 0.01f);
            item.SetVariable("SpinCharge", currentCharge + (Time.deltaTime / 6.5f));
            yield return Yielders.EndOfFrame;
        }
        foreach (SpellCaster spellCaster in item.tkHandlers) spellCaster.ragdollHand.PlayHapticClipOver(fullyChargedCurve, 0.15f);
        overchargeEffectData.Spawn(item.Bounds.center, Quaternion.identity).Play();
        var instance = overchargedEffectData.Spawn(imbue.colliderGroup.imbueEffectRenderer.transform);
        instance.SetRenderer(imbue.colliderGroup.imbueEffectRenderer, false);
        instance.Play();
        var damagers = item.GetComponentsInChildren<Damager>();
        for (int i = 0; i < damagers.Length; i++)
            if (damagers[i].penetrationDepth > 0)
                damagers[i].OnPenetrateEvent += OnPenetrateEvent;
        yield return Yielders.ForSeconds(5);
        for (int i = 0; i < damagers.Length; i++)
            if (damagers[i].penetrationDepth > 0)
                damagers[i].OnPenetrateEvent -= OnPenetrateEvent;
        instance.End();
    }

    private void OnPenetrateEvent(Damager damager, CollisionInstance collision, EventTime time)
    {
        if (collision?.targetColliderGroup?.collisionHandler?.Entity is Creature creature)
        {
            var color = creature.brain.instance.GetModule<BrainModuleCrystal>().lerper.currentColor;
            SkillOverchargedCore.Detonate(creature, color);
        }
    }
}
