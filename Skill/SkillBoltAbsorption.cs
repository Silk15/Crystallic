using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillBoltAbsorption : SkillAbsorptionData
{
    private readonly float cooldown = 0.075f;
    private float lastHitTime = 1;
    protected EffectData boltEffectData;
    public string boltEffectId = "Thunderbolt";
    protected EffectData zapEffectData;
    public string zapEffectId = "BoltZap";
    public static event OnBoltAbsorptionTriggered onBoltAbsorptionTriggered;
    public delegate void OnBoltAbsorptionTriggered(Color color, SpellCastCrystallic main, SpellCastLightning other);

    public delegate void End();
    public static event End onEnd;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        zapEffectData = Catalog.GetData<EffectData>(zapEffectId);
        boltEffectData = Catalog.GetData<EffectData>(boltEffectId);
    }

    protected override void OnStingerSpawnWithSpell(Stinger stinger)
    {
        base.OnStingerSpawnWithSpell(stinger);
        boltEffectData.Spawn(stinger.transform).Play();
    }

    protected override void OnSpellStopEvent(SpellCastCharge spell)
    {
        base.OnSpellStopEvent(spell);
        onEnd?.Invoke();
    }

    public override void PlayAbsorbEffect(SpellCastCharge main)
    {
        base.PlayAbsorbEffect(main);
        bool hasSpell = Player.currentCreature.handLeft.caster.spellInstance != null && !string.IsNullOrEmpty(Player.currentCreature.handLeft.caster.spellInstance.id) && Player.currentCreature.handRight.caster.spellInstance != null && !string.IsNullOrEmpty(Player.currentCreature.handRight.caster.spellInstance.id);
        bool canRun = Player.currentCreature?.handLeft?.caster?.spellInstance?.id == "Lightning" && Player.currentCreature.handLeft.caster.isFiring && Player.currentCreature?.handRight?.caster?.spellInstance?.id == "Crystallic" && Player.currentCreature.handRight.caster.isFiring || Player.currentCreature?.handRight?.caster?.spellInstance?.id == "Lightning" && Player.currentCreature.handRight.caster.isFiring && Player.currentCreature?.handLeft?.caster?.spellInstance?.id == "Crystallic" && Player.currentCreature.handLeft.caster.isFiring;
        if (!canRun || !hasSpell) return;
        if (main is SpellCastLightning spellCastLightning)
        {
            spellCastLightning.PlayBolt(main.spellCaster.Orb, main.spellCaster.other.Orb);
            onBoltAbsorptionTriggered?.Invoke(Dye.GetEvaluatedColor("Lightning", "Lightning"), main.spellCaster.other.spellInstance as SpellCastCrystallic, main as SpellCastLightning);
        }
        else if (main.spellCaster.other.spellInstance is SpellCastLightning spellCastLightning1)
        {
            spellCastLightning1.PlayBolt(spellCastLightning1.spellCaster.Orb, main.spellCaster.Orb);
            onBoltAbsorptionTriggered?.Invoke(Dye.GetEvaluatedColor("Lightning", "Lightning"), main as SpellCastCrystallic, main.spellCaster.other.spellInstance as SpellCastLightning);
        }
    }

    protected override void OnStingerStab(Stinger stinger, Damager damager, CollisionInstance collisionInstance, Creature hitCreature)
    {
        base.OnStingerStab(stinger, damager, collisionInstance, hitCreature);
        bool hasSpell = Player.currentCreature.handLeft.caster.spellInstance != null && !string.IsNullOrEmpty(Player.currentCreature.handLeft.caster.spellInstance.id) && Player.currentCreature.handRight.caster.spellInstance != null && !string.IsNullOrEmpty(Player.currentCreature.handRight.caster.spellInstance.id);
        bool canRun = Player.currentCreature?.handLeft.caster?.spellInstance?.id == "Lightning" && Player.currentCreature.handLeft.caster.isFiring && Player.currentCreature?.handRight?.caster?.spellInstance?.id == "Crystallic" && Player.currentCreature.handRight.caster.isFiring || Player.currentCreature?.handRight?.caster?.spellInstance?.id == "Lightning" && Player.currentCreature.handRight.caster.isFiring && Player.currentCreature?.handLeft?.caster?.spellInstance?.id == "Crystallic" && Player.currentCreature.handLeft.caster.isFiring;
        if (!canRun || !hasSpell) return;
        hitCreature.Inflict("Electrocute", this, 5);
        var spellCastLightning = Player.currentCreature.handLeft.caster.spellInstance.id == "Lightning" ? Player.currentCreature.handLeft.caster.spellInstance as SpellCastLightning : Player.currentCreature.handRight.caster.spellInstance as SpellCastLightning;
        spellCastLightning?.PlayBolt(stinger.spellCastCrystallic.spellCaster.Orb, spellCastLightning.spellCaster.Orb);
        spellCastLightning?.PlayBolt(stinger.spellCastCrystallic.spellCaster.Orb, hitCreature.ragdoll.targetPart.transform);
        foreach (var creature in Creature.InRadius(collisionInstance.contactPoint, 5))
        {
            if (creature.isPlayer) continue;
            for (var i = 0; i < Random.Range(1, 2); i++)
            {
                zapEffectData.Spawn(creature.ragdoll.targetPart.transform).Play();
                spellCastLightning?.PlayBolt(hitCreature.ragdoll.targetPart.transform, creature.ragdoll.targetPart.transform);
            }

            var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            brainModuleCrystal.Crystallise(5);
            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, spellId), spellId);
            creature.Inflict("Electrocute", this, 5);
        }
    }

    protected override void OnShardshotHitWhileAbsorbing(SpellCastCrystallic spellCastCrystallic, ThunderEntity entity, SpellCastCrystallic.ShardshotHit hitInfo)
    {
        base.OnShardshotHitWhileAbsorbing(spellCastCrystallic, entity, hitInfo);
        if (Time.time - lastHitTime < cooldown) return;
        lastHitTime = Time.time;
        if (entity is Creature creature1) creature1.brain.instance.GetModule<BrainModuleCrystal>().SetColor(Dye.GetEvaluatedColor(creature1.brain.instance.GetModule<BrainModuleCrystal>().lerper.currentSpellId, spellId), spellId);
        var other = spellCastCrystallic.spellCaster.other.spellInstance as SpellCastLightning;
        foreach (var creature in Creature.InRadius(hitInfo.hitPoint, 2.5f))
        {
            if (creature.isPlayer || creature == entity) continue;
            zapEffectData.Spawn(creature.ragdoll.targetPart.transform).Play();
            other.PlayBolt(hitInfo.hitCollider.transform, creature.ragdoll.targetPart.transform);
            creature.Inflict("Electrocute", this, 5);
            var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            brainModuleCrystal.Crystallise(5);
            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, spellId), spellId);
        }
    }
}