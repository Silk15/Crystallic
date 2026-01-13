using System;
using System.Collections.Generic;
using Crystallic.Skill.Imbue;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill.Spell;

public class SkillCrystallineSurge : SpellSkillData
{
    public Dictionary<Item, SkillSpellPair> pairedItems = new();
    public Dictionary<Side, float> lastDashTimes = new();
    public List<SkillSpellPair> skillSpellPairs = new();

    [ModOption("Dash Speed", "Controls how far the player is accelerated when you dash."), ModOptionFloatValues(1f, 100f, 1f), ModOptionSlider, ModOptionCategory("Crystalline Surge", 20)]
    public static float dashSpeed = 10f;

    [ModOption("Dash Cooldown", "Controls how long you have to wait to dash again."), ModOptionFloatValues(0.1f, 10f, 0.1f), ModOptionSlider, ModOptionCategory("Crystalline Surge", 20)]
    public static float dashCooldown = 1.5f;

    [ModOption("Dash Imbue Drain", "Controls how much imbue is drained from the item that triggers a dash."), ModOptionFloatValues(1f, 100f, 1f), ModOptionSlider, ModOptionCategory("Crystalline Surge", 20)]
    public static float dashImbueDrain = 10f;

    [ModOption("Dash Button", "Controls the button required to dash.", defaultValueIndex = 0), ModOptionCategory("Crystalline Surge", 20)]
    public static Interactable.Action action = Interactable.Action.UseStart;

    public EffectData effectData;
    public string effectId = "CrystallineSurge";

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        pairedItems.Clear();
        effectData = Catalog.GetData<EffectData>(effectId);
    }

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);

        if (!creature.airHelper)
            return;

        creature.airHelper.OnGroundEvent -= OnGround;
        creature.airHelper.OnGroundEvent += OnGround;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        if (!creature.airHelper) return;
        creature.airHelper.OnGroundEvent -= OnGround;
    }

    private void OnGround(Creature creature) => creature.SetVariable("CanDash", true);

    public override void OnImbueLoad(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        imbue.RunAfter(() =>
        {
            foreach (var pair in skillSpellPairs)
            {
                if (imbue.spellCastBase.id != pair.spellId || !imbue.imbueCreature.HasSkill(pair.skillId))
                    continue;

                if (pair.spellId != "Crystallic")
                {
                    bool hasEnabledImbueBehaviour = false;

                    foreach (ImbueBehaviour imbueBehaviour in imbue.GetComponents<ImbueBehaviour>())
                        if (imbueBehaviour.enabled && imbueBehaviour.crystalImbueSkillData.spellId == pair.spellId)
                            hasEnabledImbueBehaviour = true;

                    if (!hasEnabledImbueBehaviour)
                        continue;
                }

                Item item = imbue.colliderGroup.collisionHandler.item;

                if (pairedItems.ContainsKey(item))
                    pairedItems.Remove(item);

                pairedItems.Add(item, pair);

                item.OnHeldActionEvent -= OnHeldAction;
                item.OnHeldActionEvent += OnHeldAction;
            }
        }, 0.05f);
    }

    public override void OnImbueUnload(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        Item item = imbue.colliderGroup.collisionHandler.item;

        item.OnHeldActionEvent -= OnHeldAction;

        if (pairedItems.ContainsKey(item))
            pairedItems.Remove(item);
    }

    private void OnHeldAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        if (action != SkillCrystallineSurge.action || handle.item.flyDirRef == null)
            return;

        TryDash(ragdollHand.side, handle.item, handle.item.flyDirRef.forward * dashSpeed);
    }

    public void TryDash(Side side, Item item, Vector3 direction)
    {
        bool canDash = !lastDashTimes.ContainsKey(side) || Time.time - lastDashTimes[side] >= dashCooldown;

        if (canDash && item?.mainHandler?.creature is Creature creature)
        {
            if (creature.airHelper.inAir)
                if (creature.TryGetVariable("CanDash", out bool alreadyDashedInAir) && !alreadyDashedInAir)
                    canDash = false;

            if (canDash)
            {
                if (item.imbues[0] is ThunderRoad.Imbue imbue && !ThunderRoad.Imbue.infiniteImbue)
                    imbue.Transfer(imbue.spellCastBase, -dashImbueDrain);

                lastDashTimes[side] = Time.time;
                item.RunAfter(() => item.Haptic(1f), dashCooldown);

                if (creature.airHelper.inAir)
                    creature.SetVariable("CanDash", false);

                Dash(pairedItems[item], direction);
                item.Haptic(1f);
            }
        }
    }

    public void Dash(SkillSpellPair skillSpellPair, Vector3 direction)
    {
        Player.currentCreature.AddForce(direction, ForceMode.VelocityChange);
        if (skillSpellPair.StatusData != null)
            Player.currentCreature.Inflict(skillSpellPair.StatusData, this, skillSpellPair.statusDuration);

        var effectInstance = effectData.Spawn(Player.local.head.cam.transform.position + Player.local.head.cam.transform.forward * 0.175f, Quaternion.LookRotation(Player.local.head.cam.transform.forward, Vector3.up), Player.local.head.cam.transform);

        if (skillSpellPair.spellId != "Crystallic")
            effectInstance.SetColor(Color.Lerp(Catalog.GetData<DyeData>("Dye" + skillSpellPair.spellId).color, Color.white, 0.5f));
        else
            effectInstance.SetColor(Color.white);

        effectInstance.Play();
    }
}