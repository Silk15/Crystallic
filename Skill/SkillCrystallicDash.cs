using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystallicDash : SpellSkillData
{
    private readonly Dictionary<Item, SkillSpellDash> items = new();
    public Interactable.Action action;
    public List<string> allowedAuthors = new();
    public List<string> allowedCategories = new();
    public List<ItemFlags> allowedFlags = new();
    public List<string> allowedItems = new();
    public List<string> allowedSlots = new();
    public List<SkillSpellDash> allowedSpells = new();
    public List<int> allowedTiers = new();
    public List<ItemData.Type> allowedTypes = new();
    public AnimationCurve hapticCurve = new(new Keyframe(0.0f, 7f), new Keyframe(0.05f, 5f), new Keyframe(0.1f, 3f));
    public Creature thisCreature;

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        thisCreature = creature;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        thisCreature = null;
        items.Clear();
    }

    public override void OnImbueLoad(SpellData spell, Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        var item = imbue?.colliderGroup?.collisionHandler?.item;
        if (item && !items.ContainsKey(item) && allowedSpells.Count > 0)
            foreach (var mix in allowedSpells)
                if (item && imbue?.spellCastBase.id == mix.spellId)
                    if (IsItemAllowed(item) && item.flyDirRef != null)
                    {
                        if (!string.IsNullOrEmpty(mix.skillId) && !thisCreature.HasSkill(mix.skillId)) continue;
                        items.Add(item, mix);
                        if (!string.IsNullOrEmpty(mix.effectId)) mix.dashEffectData = Catalog.GetData<EffectData>(mix.effectId);
                        item.OnHeldActionEvent += OnHeldActionEvent;
                        break;
                    }
    }

    private bool IsItemAllowed(Item item)
    {
        var hasAnyRestrictions = allowedItems.Count > 0 || allowedCategories.Count > 0 || allowedTypes.Count > 0 || allowedTiers.Count > 0 || allowedFlags.Count > 0 || allowedSlots.Count > 0 || allowedAuthors.Count > 0;
        if (!hasAnyRestrictions) return true;
        return (allowedItems.Count > 0 && allowedItems.Contains(item.itemId)) || (allowedCategories.Count > 0 && allowedCategories.Contains(item.data.category)) || (allowedTypes.Count > 0 && allowedTypes.Contains(item.data.type)) || (allowedTiers.Count > 0 && allowedTiers.Contains(item.data.tier)) || (allowedFlags.Count > 0 && allowedFlags.Contains(item.data.flags)) || (allowedSlots.Count > 0 && allowedSlots.Contains(item.data.slot)) || (allowedAuthors.Count > 0 && allowedAuthors.Contains(item.data.author) && allowedAuthors.Contains(item.data.author));
    }


    public override void OnImbueUnload(SpellData spell, Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        var item = imbue?.colliderGroup?.collisionHandler?.item;
        if (item && items.ContainsKey(item))
        {
            item.OnHeldActionEvent -= OnHeldActionEvent;
            items.Remove(item);
        }
    }

    private void OnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
    {
        if (action == this.action && ragdollhand.grabbedHandle == handle && handle.item != null)
        {
            ragdollhand.PlayHapticClipOver(hapticCurve, 0.4f);
            if (items.TryGetValue(handle.item, out var mix)) Accelerate(handle.item.flyDirRef.forward, mix);
        }
    }

    public void Accelerate(Vector3 direction, SkillSpellDash mix)
    {
        direction.Normalize();
        Player.local.locomotion.velocity = direction.normalized * Player.local.locomotion.velocity.magnitude;
        Player.local.locomotion.physicBody.AddForce(direction.normalized * mix.dashSpeed, ForceMode.VelocityChange);
        var effectPosition = Player.local.head.cam.transform.position + Player.local.head.cam.transform.forward * 0.3f;
        var effectRotation = Player.local.head.cam.transform.rotation;
        var eyeEffect = mix.dashEffectData?.Spawn(effectPosition, effectRotation, Player.local.head.transform);
        eyeEffect?.Play();
        if (!string.IsNullOrEmpty(mix.statusId)) thisCreature.RunAfter(() => { thisCreature.Inflict(mix.statusId, this, mix.statusDuration, mix.statusParam); }, mix.statusInflictDelay);
    }
}