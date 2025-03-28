using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillStingshot : SpellSkillData
{
    public StatusData statusData;
    
    public EffectData tetherEffectData;
    public string tetherEffectId = "Stingshot";

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        tetherEffectData = Catalog.GetData<EffectData>(tetherEffectId);
        statusData = Catalog.GetData<StatusData>("Floating");
    }

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        Stinger.onStingerSpawn += OnStingerSpawn;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        Stinger.onStingerSpawn -= OnStingerSpawn;
    }

    private void OnStingerSpawn(Stinger stinger)
    {
        stinger.onStingerStab += OnStingerStab;
    }

    private void OnStingerStab(Stinger stinger, Damager damager, CollisionInstance collisionInstance, Creature hitCreature)
    {
        stinger.onStingerStab -= OnStingerStab;
        stinger.RunAfter(() => { stinger.StartCoroutine(ThrowRoutine(stinger)); }, 0.065f);
    }

    public IEnumerator ThrowRoutine(Stinger stinger)
    {
        EffectInstance effectInstance = null;
        var startTime = Time.time;
        var gripped = false;
        var hand = stinger.spellCastCrystallic.spellCaster.ragdollHand;
        yield return new WaitForSeconds(0.1f);
        stinger.spellCastCrystallic.spellCaster.telekinesis.Disable(this);

        while (Time.time - startTime < 0.5f)
        {
            if (hand.playerHand.controlHand.gripPressed && hand.grabbedHandle == null)
            {
                gripped = true;
                break;
            }

            yield return null;
        }

        if (gripped)
        {
            Player.currentCreature.Inflict(statusData, this, parameter: new FloatingParams(drag: 2f, noSlamAtEnd: true));
            stinger.spellCastCrystallic.spellCaster.telekinesis.Enable(this);

            if (tetherEffectData != null)
            {
                effectInstance = tetherEffectData.Spawn(stinger.spellCastCrystallic.spellCaster.magicSource);
                effectInstance.SetSource(stinger.spellCastCrystallic.spellCaster.magicSource);
                effectInstance.SetTarget(stinger.transform);
                effectInstance.Play();
            }

            while (hand.playerHand.controlHand.gripPressed) yield return null;

            Vector3 forward = stinger.spellCastCrystallic.spellCaster.ragdollHand.Velocity();
            effectInstance?.End();
            Player.currentCreature.Remove(statusData, this);

            if (forward.sqrMagnitude >= SpellCaster.throwMinHandVelocity * SpellCaster.throwMinHandVelocity)
            {
                Player.local.AddForce(-forward, forward.magnitude * 2);
            }
        }
        else
        {
            Player.currentCreature.Remove(statusData, this);
        }
    }

}