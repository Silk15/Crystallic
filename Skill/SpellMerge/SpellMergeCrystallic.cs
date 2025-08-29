using System;
using System.Collections.Generic;
using Crystallic.Skill.Spell;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill.SpellMerge
{
    public class SpellMergeCrystallic : SpellMergeData
    {
        public AnimationCurve sprayForceCurve = new(new Keyframe(0.0f, 10f), new Keyframe(0.05f, 25f), new Keyframe(0.1f, 10f));
        public float sprayHandRotationDamperMultiplier = 0.6f;
        public float sprayHandRotationSpringMultiplier = 0.2f;
        public float sprayHandPositionDamperMultiplier = 1f;
        public float sprayHandPositionSpringMultiplier = 1f;
        public float sprayLocomotionPushForce = 4f;
        public float sprayCastMinHandAngle = 20f;
        public float movementSpeedMult = 0.8f;
        public bool sprayActive;

        public Vector2 minMaxFireDelay = new Vector2(0.075f, 0.15f);
        public float sprayConeAngle = 65f;

        public Vector3 sprayForward;
        public Vector3 sprayUp;

        private float sprayNextFireTime;

        public override void Merge(bool active)
        {
            base.Merge(active);
            if (active) currentCharge = 0.35f;
            else
            {
                mana.creature.locomotion.RemoveSpeedModifier(this);
                currentCharge = 0.0f;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (sprayActive)
            {
                Vector3 forceDir = -mana.mergePoint.transform.forward;
                float force = sprayForceCurve.Evaluate(Time.time);
                mana.casterLeft.ragdollHand.physicBody.AddForce(forceDir * force, ForceMode.Force);
                mana.casterRight.ragdollHand.physicBody.AddForce(forceDir * force, ForceMode.Force);
                Player.local.locomotion.physicBody.AddForce(forceDir * sprayLocomotionPushForce, ForceMode.Acceleration);
            }
        }

        public override void Update()
        {
            base.Update();
            sprayForward = Vector3.Slerp(mana.casterLeft.magicSource.up, mana.casterRight.magicSource.up, 0.5f).normalized;
            sprayUp = mana.mergePoint.up;
            bool canActivateBeam = Vector3.SignedAngle(mana.mergePoint.forward, mana.casterLeft.magicSource.up, Vector3.Cross(mana.creature.centerEyes.position - mana.mergePoint.position, mana.casterLeft.magicSource.position - mana.mergePoint.position).normalized) < -sprayCastMinHandAngle && Vector3.SignedAngle(mana.mergePoint.forward, mana.casterRight.magicSource.up, Vector3.Cross(mana.casterRight.magicSource.position - mana.mergePoint.position, mana.creature.centerEyes.position - mana.mergePoint.position).normalized) > sprayCastMinHandAngle;
            if (canActivateBeam && currentCharge >= 0.8f)
            {
                if (!sprayActive)
                {
                    sprayActive = true;
                    mana.creature.locomotion.SetAllSpeedModifiers(this, movementSpeedMult);
                    mana.casterLeft.ragdollHand.playerHand.link.SetJointModifier(this, sprayHandPositionSpringMultiplier, sprayHandPositionDamperMultiplier, sprayHandRotationSpringMultiplier, sprayHandRotationDamperMultiplier, 0.4f);
                    mana.casterRight.ragdollHand.playerHand.link.SetJointModifier(this, sprayHandPositionSpringMultiplier, sprayHandPositionDamperMultiplier, sprayHandRotationSpringMultiplier, sprayHandRotationDamperMultiplier, 0.4f);
                }
            }
            
            if (!sprayActive) return;

            mana.casterRight.ragdollHand.playerHand.controlHand.HapticLoop(this, 1f, 0.01f);
            mana.casterLeft.ragdollHand.playerHand.controlHand.HapticLoop(this, 1f, 0.01f);

            if (Time.time >= sprayNextFireTime)
            {
                sprayNextFireTime = Time.time + UnityEngine.Random.Range(minMaxFireDelay.x, minMaxFireDelay.y);

                if (mana.casterLeft.spellInstance is SpellCastCrystallic spellCastCrystallic)
                {
                    float[] angles = new float[5];
                    for (int i = 0; i < 5; i++) angles[i] = -75f / 2f + i * 75f / (5 - 1);
                    float angle = angles[UnityEngine.Random.Range(0, angles.Length)];
                    Vector3 shotDir = Quaternion.AngleAxis(angle, sprayUp) * sprayForward;
                    shotDir.Normalize();
                    spellCastCrystallic.FireShard(spellCastCrystallic.shardEffectData, mana.mergePoint.position + shotDir * 0.2f, shotDir * UnityEngine.Random.Range(10f, 15f), 5f, shard =>
                    {
                        shard.homing = true;
                        shard.allowReflect = false;
                    });
                }
            }
        }
    }
}