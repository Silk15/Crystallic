using System.Collections;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillHyperintensity : SpellSkillData
{
    public delegate void OnSpellOvercharge(SpellCastCrystallic spellCastCrystallic);

    public delegate void OnSpellReleased(SpellCastCrystallic spellCastCrystallic);

    public static EffectInstance overchargeLeftLoopEffect;
    public static EffectInstance overchargeRightLoopEffect;
    public static bool leftFullyCharged;
    public static bool rightFullyCharged;
    private static bool allowLeftDrain;
    private static bool allowRightDrain;
    public AnimationCurve hapticCurve = new(new Keyframe(0.0f, 20f), new Keyframe(0.05f, 45f), new Keyframe(0.1f, 20f));
    public Coroutine leftCoroutine;
    public float leftLastChargeTime;
    public EffectData overchargeLoopEffectData;
    public string overchargeLoopEffectId;
    public EffectData overchargeStartEffectData;
    public string overchargeStartEffectId;
    public Coroutine rightCoroutine;
    public float rightLastChargeTime;
    public float timeToOvercharge;

    public static event OnSpellOvercharge onSpellOvercharge;

    public static event OnSpellReleased onSpellReleased;

    public static bool isOvercharged(Side side)
    {
        return side == Side.Left ? leftFullyCharged : rightFullyCharged;
    }

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        EventManager.onPossess += OnPossess;
    }

    private void OnPossess(Creature creature, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart) return;
        EventManager.onPossess -= OnPossess;
        EndingContent.GetCurrent().hasT4Skill = true;
    }

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        leftFullyCharged = false;
        rightFullyCharged = false;
        overchargeStartEffectData = Catalog.GetData<EffectData>(overchargeStartEffectId);
        overchargeLoopEffectData = Catalog.GetData<EffectData>(overchargeLoopEffectId);
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (spell is SpellCastCrystallic spellCastCrystallic)
        {
            spellCastCrystallic.OnSpellStopEvent += OnSpellStopEvent;
            spellCastCrystallic.OnSpellThrowEvent += OnSpellThrowEvent;
            spellCastCrystallic.OnSpellUpdateEvent += OnSpellUpdateEvent;
            var instanceForSide = spellCastCrystallic.spellCaster.side == Side.Left ? overchargeLeftLoopEffect : overchargeRightLoopEffect;
            leftFullyCharged = false;
            rightFullyCharged = false;
            if (instanceForSide != null)
            {
                instanceForSide?.End();
                instanceForSide = null;
            }
        }
    }

    private void OnSpellStopEvent(SpellCastCharge spell)
    {
        var crystallic = spell as SpellCastCrystallic;
        if (spell.spellCaster.side == Side.Left)
        {
            leftFullyCharged = false;
            if (overchargeLeftLoopEffect != null)
            {
                overchargeLeftLoopEffect.End();
                overchargeLeftLoopEffect = null;
            }

            if (leftCoroutine != null)
            {
                GameManager.local.StopCoroutine(OverchargeRoutine(spell as SpellCastCrystallic));
                leftCoroutine = null;
            }
        }
        else if (spell.spellCaster.side == Side.Right)
        {
            rightFullyCharged = false;
            if (overchargeRightLoopEffect != null)
            {
                overchargeRightLoopEffect.End();
                overchargeRightLoopEffect = null;
            }

            if (rightCoroutine != null)
            {
                GameManager.local.StopCoroutine(OverchargeRoutine(spell as SpellCastCrystallic));
                rightCoroutine = null;
            }
        }

        GameManager.local.StartCoroutine(UnsubscribeRoutine(0.1f, spell as SpellCastCrystallic));
    }

    private void OnSpellThrowEvent(SpellCastCharge spell, Vector3 velocity)
    {
        var crystallic = spell as SpellCastCrystallic;
        if (spell.spellCaster.side == Side.Left)
        {
            leftFullyCharged = false;
            if (overchargeLeftLoopEffect != null)
            {
                overchargeLeftLoopEffect.End();
                overchargeLeftLoopEffect = null;
            }

            if (leftCoroutine != null)
            {
                GameManager.local.StopCoroutine(OverchargeRoutine(spell as SpellCastCrystallic));
                leftCoroutine = null;
            }
        }
        else if (spell.spellCaster.side == Side.Right)
        {
            rightFullyCharged = false;
            if (overchargeRightLoopEffect != null)
            {
                overchargeRightLoopEffect.End();
                overchargeRightLoopEffect = null;
            }

            if (rightCoroutine != null)
            {
                GameManager.local.StopCoroutine(OverchargeRoutine(spell as SpellCastCrystallic));
                rightCoroutine = null;
            }
        }

        GameManager.local.StartCoroutine(UnsubscribeRoutine(0.1f, spell as SpellCastCrystallic));
    }

    public static void ForceInvokeOvercharged(SpellCastCrystallic spellCastCrystallic)
    {
        onSpellOvercharge?.Invoke(spellCastCrystallic);
    }

    public static void ForceInvokeRelease(SpellCastCrystallic spellCastCrystallic)
    {
        onSpellReleased?.Invoke(spellCastCrystallic);
    }

    public IEnumerator UnsubscribeRoutine(float delay, SpellCastCrystallic spellCastCrystallic)
    {
        yield return new WaitForSeconds(delay);
        onSpellReleased?.Invoke(spellCastCrystallic);
    }


    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (spell is SpellCastCrystallic spellCastCrystallic)
        {
            spellCastCrystallic.OnSpellStopEvent += OnSpellStopEvent;
            spellCastCrystallic.OnSpellThrowEvent -= OnSpellThrowEvent;
            spellCastCrystallic.OnSpellUpdateEvent -= OnSpellUpdateEvent;
            var instanceForSide = spellCastCrystallic.spellCaster.side == Side.Left ? overchargeLeftLoopEffect : overchargeRightLoopEffect;
            leftFullyCharged = false;
            rightFullyCharged = false;
            if (instanceForSide != null)
            {
                instanceForSide?.End();
                instanceForSide = null;
            }
        }
    }

    public static void ToggleDrain(Side side, bool active)
    {
        switch (side)
        {
            case Side.Left:
                allowLeftDrain = active;
                break;
            case Side.Right:
                allowRightDrain = active;
                break;
        }
    }

    private void OnSpellUpdateEvent(SpellCastCharge spell)
    {
        switch (spell.spellCaster.side)
        {
            case Side.Left:
                if (Mathf.Approximately(spell.currentCharge, 1) && !leftFullyCharged)
                {
                    leftLastChargeTime = Time.time;
                    leftFullyCharged = true;
                    if (leftCoroutine != null)
                    {
                        GameManager.local.StopCoroutine(leftCoroutine);
                        leftCoroutine = null;
                    }

                    leftCoroutine = GameManager.local.StartCoroutine(OverchargeRoutine(spell as SpellCastCrystallic));
                }
                else if (spell.currentCharge < 1 && leftFullyCharged && allowLeftDrain)
                {
                    if (overchargeLeftLoopEffect != null)
                    {
                        overchargeLeftLoopEffect.End();
                        overchargeLeftLoopEffect = null;
                    }

                    onSpellReleased?.Invoke(spell as SpellCastCrystallic);
                    leftFullyCharged = false;
                }

                break;
            case Side.Right:
                if (Mathf.Approximately(spell.currentCharge, 1) && !rightFullyCharged)
                {
                    rightLastChargeTime = Time.time;
                    rightFullyCharged = true;
                    if (rightCoroutine != null)
                    {
                        GameManager.local.StopCoroutine(rightCoroutine);
                        rightCoroutine = null;
                    }

                    rightCoroutine = GameManager.local.StartCoroutine(OverchargeRoutine(spell as SpellCastCrystallic));
                }
                else if (spell.currentCharge < 1 && rightFullyCharged && allowRightDrain)
                {
                    if (overchargeRightLoopEffect != null)
                    {
                        overchargeRightLoopEffect.End();
                        overchargeRightLoopEffect = null;
                    }

                    onSpellReleased?.Invoke(spell as SpellCastCrystallic);
                    rightFullyCharged = false;
                }

                break;
        }
    }

    private IEnumerator OverchargeRoutine(SpellCastCrystallic spell)
    {
        if (spell.spellCaster.side == Side.Left)
        {
            while (Time.time - leftLastChargeTime < timeToOvercharge) yield return null;
            Overcharge(spell);
        }
        else
        {
            while (Time.time - rightLastChargeTime < timeToOvercharge) yield return null;
            Overcharge(spell);
        }
    }

    public void Overcharge(SpellCastCrystallic spell)
    {
        if (!spell.spellCaster.isFiring || !Mathf.Approximately(spell.currentCharge, 1)) return;
        spell.spellCaster.ragdollHand.PlayHapticClipOver(hapticCurve, 0.25f);
        overchargeStartEffectData.Spawn(spell.spellCaster.Orb).Play();
        if (spell.spellCaster.side == Side.Left)
        {
            if (overchargeLeftLoopEffect != null) overchargeLeftLoopEffect.End();
            overchargeLeftLoopEffect = overchargeLoopEffectData.Spawn(spell.spellCaster.Orb);
            overchargeLeftLoopEffect.Play();
            overchargeLeftLoopEffect.SetColorImmediate(spell.currentColor);
        }
        else
        {
            if (overchargeRightLoopEffect != null) overchargeRightLoopEffect.End();
            overchargeRightLoopEffect = overchargeLoopEffectData.Spawn(spell.spellCaster.Orb);
            overchargeRightLoopEffect.Play();
            overchargeRightLoopEffect.SetColorImmediate(spell.currentColor);
        }

        onSpellOvercharge?.Invoke(spell);
    }
}