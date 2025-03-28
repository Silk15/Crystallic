using System.Collections.Generic;
using Crystallic.AI;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalWarrior : SkillSpellPunch
{
    public List<SkillStatusPair> skillStatusPairs;
    public SkillStatusPair active;
    public string armEffectId;
    public string deflectEffectId;
    public AnimationCurve hapticCurve = new(new Keyframe(0.0f, 10f), new Keyframe(0.05f, 25f), new Keyframe(0.1f, 10f));
    public EffectData imbueCollisionEffectData;
    private bool leftGripping;
    public Lerper lerper;
    public EffectData lowerArmData;
    public EffectInstance lowerLeftArmInstance;
    public EffectInstance lowerRightArmInstance;
    private string leftCurrent = "Body";
    private string rightCurrent = "Body";
    private bool rightGripping;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        lerper = new Lerper();
        lowerArmData = Catalog.GetData<EffectData>(armEffectId);
        imbueCollisionEffectData = Catalog.GetData<EffectData>(deflectEffectId);
    }

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        PlayerControl.local.OnButtonPressEvent -= OnButtonPressEvent;
        PlayerControl.local.OnButtonPressEvent += OnButtonPressEvent;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        PlayerControl.local.OnButtonPressEvent -= OnButtonPressEvent;
    }
    
    public override void OnLateSkillsLoaded(SkillData skillData, Creature creature)
    {
        base.OnLateSkillsLoaded(skillData, creature);
        SkillThickSkin.SetRandomness(new Vector2(0, 4));
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (spell is SpellCastCharge spellCastCharge)
        {
            spellCastCharge.OnSpellCastEvent -= OnSpellCastEvent;
            spellCastCharge.OnSpellStopEvent -= OnSpellStopEvent;
            spellCastCharge.OnSpellCastEvent += OnSpellCastEvent;
            spellCastCharge.OnSpellStopEvent += OnSpellStopEvent;
        }
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (spell is SpellCastCharge spellCastCharge)
        {
            spellCastCharge.OnSpellCastEvent -= OnSpellCastEvent;
            spellCastCharge.OnSpellStopEvent -= OnSpellStopEvent;
        }
    }

    private void OnSpellCastEvent(SpellCastCharge spell)
    {
        SkillCrystallicDive.spellId = spell.id;
        for (int i = 0; i < skillStatusPairs.Count; i++) if (skillStatusPairs[i].spellId == spell.id) active = skillStatusPairs[i];
        SkillCrystallicDive.active = active;
        switch (spell.spellCaster.side)
        {
            case Side.Left when !leftGripping && rightGripping:
                SetColor(Dye.GetEvaluatedColor("Body", spell.id), spell.id, Side.Right);
                rightCurrent = spell.id;
                break;
            case Side.Right when leftGripping && !rightGripping:
                SetColor(Dye.GetEvaluatedColor("Body", spell.id), spell.id, Side.Left);
                leftCurrent = spell.id;
                break;
            default:
                return;
        }
    }
    
    private void OnSpellStopEvent(SpellCastCharge spell)
    {
        SkillCrystallicDive.spellId = "Body";
        active = null;
        SkillCrystallicDive.active = null;
        switch (spell.spellCaster.side)
        {
            case Side.Left when !leftGripping && rightGripping:
                SetColor(Dye.GetEvaluatedColor("Body", "Body"), "Body", Side.Right);
                rightCurrent = "Body";
                break;
            case Side.Right when leftGripping && !rightGripping:
                SetColor(Dye.GetEvaluatedColor("Body", "Body"), "Body", Side.Left);
                leftCurrent = "Body";
                break;
            default:
                return;
        }
    }

    private void OnButtonPressEvent(PlayerControl.Hand hand, PlayerControl.Hand.Button button, bool pressed)
    {
        if (button is PlayerControl.Hand.Button.AlternateUse && pressed)
        {
            if (leftGripping && hand.side == Side.Left) lowerLeftArmInstance.End();
            else if (rightGripping && hand.side == Side.Right) lowerRightArmInstance.End();
        }
        else if (button is PlayerControl.Hand.Button.Use && pressed && leftGripping && hand.side == Side.Left)
        {
            lowerLeftArmInstance.End();
            lowerLeftArmInstance.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
            Player.currentCreature.OnDamageEvent -= OnDamageEvent;
        }
        else if (button is PlayerControl.Hand.Button.Use && pressed && rightGripping && hand.side == Side.Right)
        {
            lowerRightArmInstance.End();
            lowerRightArmInstance.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
            Player.currentCreature.OnDamageEvent -= OnDamageEvent;
        }
    }

    public override void OnFist(PlayerHand hand, bool gripping)
    {
        base.OnFist(hand, gripping);
        if (gripping) Player.currentCreature.OnDamageEvent += OnDamageEvent;
        else Player.currentCreature.OnDamageEvent -= OnDamageEvent;
        switch (hand.side)
        {
            case Side.Left:
                leftGripping = gripping;
                if (gripping)
                {
                    var lowerLeftArm = hand.ragdollHand.creature.ragdoll.GetPartByName("LeftForeArm");
                    lowerLeftArmInstance = lowerArmData.Spawn(lowerLeftArm.transform.position, Quaternion.LookRotation(lowerLeftArm.upDirection, lowerLeftArm.forwardDirection), lowerLeftArm.transform);
                    lowerLeftArmInstance?.Play();
                    SetColor(Dye.GetEvaluatedColor("Body", leftCurrent), leftCurrent, Side.Left);
                }
                else if (lowerLeftArmInstance != null)
                {
                    lowerLeftArmInstance?.End();
                    lowerLeftArmInstance.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                break;
            case Side.Right:
                rightGripping = gripping;
                if (gripping)
                {
                    var lowerRightArm = hand.ragdollHand.creature.ragdoll.GetPartByName("RightForeArm");
                    lowerRightArmInstance = lowerArmData.Spawn(lowerRightArm.transform.position, Quaternion.LookRotation(lowerRightArm.upDirection, lowerRightArm.forwardDirection), lowerRightArm.transform);
                    lowerRightArmInstance?.Play();
                    SetColor(Dye.GetEvaluatedColor("Body", rightCurrent), rightCurrent, Side.Right);
                }
                else if (lowerRightArmInstance != null)
                {
                    lowerRightArmInstance?.End();
                    lowerRightArmInstance.ForceStop(ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                break;
        }
    }

    private void OnDamageEvent(CollisionInstance collisioninstance, EventTime eventtime)
    {
        var hand = Player.local.handLeft.isFist ? Player.local.handLeft : Player.local.handRight;
        if (Vector3.Distance(collisioninstance.contactPoint, hand.transform.position) <= 0.55f)
        {
            collisioninstance.ignoreDamage = true;
            Player.currentCreature.Heal(collisioninstance.damageStruct.damage);
            collisioninstance.damageStruct.damage = 0;
            collisioninstance.skipVignette = true;
            imbueCollisionEffectData?.Spawn(collisioninstance.contactPoint, Quaternion.identity, collisioninstance.targetCollider.transform).Play();
            hand.ragdollHand.PlayHapticClipOver(hapticCurve, 1);
        }
    }

    public override void OnPunchHit(RagdollHand hand, CollisionInstance hit, bool fist)
    {
        base.OnPunchHit(hand, hit, fist);
        if (fist)
        {
            var creature = hit?.targetColliderGroup?.collisionHandler?.Entity as Creature;
            if (creature && !creature.isPlayer)
            {
                var module = creature.brain.instance.GetModule<BrainModuleCrystal>();
                module.Crystallise(5);
                if (active != null) active.Inflict(creature);
                module.SetColor(Dye.GetEvaluatedColor(module.lerper.currentSpellId,  hand.side == Side.Left ? leftCurrent : rightCurrent), hand.side == Side.Left ? leftCurrent : rightCurrent);
            }

            hand.PlayHapticClipOver(hapticCurve, 1);
        }
    }

    public void SetColor(Color color, string spellId, Side side, float time = 0.15f)
    {
        var systems = side == Side.Left ? lowerLeftArmInstance?.GetParticleSystems() : lowerRightArmInstance?.GetParticleSystems();
        lerper.SetColor(color, systems, spellId, time);
    }
}