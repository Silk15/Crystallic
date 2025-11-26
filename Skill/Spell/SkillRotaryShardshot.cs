using System.Collections;
using System.Collections.Generic;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill.Spell;

public class SkillRotaryShardshot : SpellSkillData
{
    public Dictionary<Side, Coroutine> rotaryCoroutines = new();
    public Dictionary<Side, bool> shardshotEnabled = new();

    [ModOption("Targeting Angle", "Controls the angle of aim assist."), ModOptionFloatValues(0f, 360f, 1f), ModOptionSlider, ModOptionCategory("Rotary Shardshot", 8)]
    public static float targetingAngle = 20f;
    
    [ModOption("Targeting Max Distance", "Controls the max distance of aim assist."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Rotary Shardshot", 8)]
    public static float targetingDistance = 5f;

    public SkillCrystalReservoir skillCrystalReservoir;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        rotaryCoroutines.FillWithDefault();
        shardshotEnabled.FillWithDefault();
    }

    public override void OnLateSkillsLoaded(SkillData skillData, Creature creature)
    {
        base.OnLateSkillsLoaded(skillData, creature);
        creature.TryGetSkill("CrystalReservoir", out skillCrystalReservoir);
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
        if (casting) return;
        Side side = spellCastCrystallic.spellCaster.side;
        switch (button)
        {
            case PlayerControl.Hand.Button.Grip when pressed && !shardshotEnabled[side]:
                ToggleRotaryShardshot(side, true);
                break;

            case PlayerControl.Hand.Button.Grip when !pressed && shardshotEnabled[side]:
                ToggleRotaryShardshot(side, false);
                break;

            case PlayerControl.Hand.Button.Use when pressed && shardshotEnabled[side] && spellCastCrystallic.spellCaster.ragdollHand.grabbedHandle == null && spellCastCrystallic.spellCaster.telekinesis.catchedHandle == null && skillCrystalReservoir.wristHolders[side].GetRandomPoint() is ArcPointsManager.PointData pointData:

                Transform orbTransform = spellCastCrystallic.spellCaster.Orb;
                Transform spawnTransform = pointData.transform;
                Vector3 direction = orbTransform.up.normalized;
                Vector3 velocity = direction * 5;

                if (Physics.Raycast(orbTransform.position, direction, out RaycastHit hit, Mathf.Infinity, ShardRaycastMask))
                {
                    Vector3 towardsHit = (hit.point - spawnTransform.position).normalized;
                    velocity = Vector3.Lerp(direction, towardsHit, 0.5f).normalized * 5;
                }

                ThunderEntity thunderEntity = Creature.AimAssist(orbTransform.position, velocity.normalized, targetingDistance, targetingAngle, out Transform targetPoint, Filter.EnemyOf(spellCastCrystallic.spellCaster.ragdollHand.ragdoll.creature), CreatureType.Golem | CreatureType.Human);
                if (thunderEntity != null)
                {
                    Vector3 offset = thunderEntity is Creature c ? c.locomotion.moveDirection * 0.5f : Vector3.zero;
                    velocity = (targetPoint.position + offset - spawnTransform.position).normalized * velocity.magnitude;
                }

                spellCastCrystallic.FireShard(spellCastCrystallic.shardEffectData, spawnTransform.position, velocity * 2.5f, spellCastCrystallic.ShardLifetime, shard =>
                {
                    spellCastCrystallic.spellCaster.ragdollHand.HapticTick();
                    skillCrystalReservoir.wristHolders[spellCastCrystallic.spellCaster.ragdollHand.side].RemovePoint(pointData);
                    spellCastCrystallic.spellCaster.ragdollHand.PlayHapticClipOver(spellCastCrystallic.pulseCurve, 0.15f);
                    EffectInstance pulse = spellCastCrystallic.pulseEffectData.Spawn(spawnTransform.position + velocity.normalized * 0.25f, Quaternion.LookRotation(velocity));
                    pulse.Play();
                    pulse.SetSize(0.5f);
                });
                break;
        }
    }

    public void ToggleRotaryShardshot(Side side, bool active)
    {
        ArcPointsManager arcPointsManager = skillCrystalReservoir.wristHolders[side];
        Coroutine rotaryCoroutine = rotaryCoroutines[side];

        GameManager.local.StopAndStartCoroutine(ModifyRadiusCoroutine(side, arcPointsManager, active), ref rotaryCoroutine);
        arcPointsManager.spin = active;

        shardshotEnabled[side] = active;
    }

    public IEnumerator ModifyRadiusCoroutine(Side side, ArcPointsManager pointsManager, bool enabled)
    {
        RagdollHand hand = Player.currentCreature.GetHand(side);
        float elapsed = 0f;
        float total = 0.1f;

        while (elapsed < total)
        {
            if (hand.grabbedHandle != null)
            {
                pointsManager.radius = pointsManager.defaultRadius;
                yield break;
            }

            yield return Yielders.EndOfFrame;
            elapsed += Time.deltaTime;
            pointsManager.radius = Mathf.Lerp(enabled ? pointsManager.defaultRadius : pointsManager.radius, enabled ? 0.15f : pointsManager.defaultRadius, Mathf.Clamp01(elapsed / total));
        }
    }

    public static int ShardRaycastMask
    {
        get
        {
            int mask = 0;
            mask |= 1 << GameManager.GetLayer(LayerName.Avatar);
            mask |= 1 << GameManager.GetLayer(LayerName.Default);
            mask |= 1 << GameManager.GetLayer(LayerName.Ragdoll);
            mask |= 1 << GameManager.GetLayer(LayerName.BodyLocomotion);
            mask |= 1 << GameManager.GetLayer(LayerName.LocomotionOnly);
            mask |= 1 << GameManager.GetLayer(LayerName.ItemAndRagdollOnly);
            return mask;
        }
    }
}