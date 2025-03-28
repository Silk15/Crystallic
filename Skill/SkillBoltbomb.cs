using System.Collections.Generic;
using Crystallic.AI;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillBoltbomb : SpellSkillData
{
    [ModOption("Bolt Hits", "Controls how many bolts it takes to generate a boltbomb. Each time this count is reached the counter is reset."), ModOptionCategory("Boltbomb", 30), ModOptionSlider, ModOptionFloatValues(1f, 1000f, 1f)]
    public static float boltHits = 50f;
    protected EffectData boltEffectData;
    public string boltEffectId = "Thunderbolt";
    protected EffectData zapEffectData;
    public string zapEffectId = "BoltZap";
    protected EffectData thunderboltEffectData;
    public string thunderboltEffectId = "SpellThunderbolt";
    protected EffectData thunderboltImpactEffectData;
    public string thunderboltImpactEffectId = "SpellThunderboltImpact";
    
    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        zapEffectData = Catalog.GetData<EffectData>(zapEffectId);
        boltEffectData = Catalog.GetData<EffectData>(boltEffectId);
        thunderboltEffectData = Catalog.GetData<EffectData>(thunderboltEffectId);
        thunderboltImpactEffectData = Catalog.GetData<EffectData>(thunderboltImpactEffectId);
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastLightning spellCastLightning)) return;
        spellCastLightning.OnBoltHitColliderGroupEvent -= OnBoltHitColliderGroupEvent;
        spellCastLightning.OnBoltHitColliderGroupEvent += OnBoltHitColliderGroupEvent;
    }
    
    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastLightning spellCastLightning)) return;
        spellCastLightning.OnBoltHitColliderGroupEvent -= OnBoltHitColliderGroupEvent;
    }

    private void OnBoltHitColliderGroupEvent(SpellCastLightning spell, ColliderGroup colliderGroup, Vector3 position, Vector3 normal, Vector3 velocity, float intensity, ColliderGroup source, HashSet<ThunderEntity> seenEntities)
    {
        var hitEntity = colliderGroup.collisionHandler.Entity;
        if (hitEntity is Creature creature)
        {
            int num = creature.GetVariable<int>("BoltHits") + 1;
            creature.SetVariable("BoltHits", num);
            if (num >= boltHits)
            {
                var obj = new GameObject("Bolt Temp");
                obj.transform.position = creature.ragdoll.targetPart.transform.position + new Vector3(0, Random.Range(4, 6), 0);
                obj.transform.rotation = Quaternion.LookRotation(Vector3.down);
                var instance = thunderboltEffectData.Spawn(obj.transform);
                instance.SetSourceAndTarget(obj.transform, creature.ragdoll.targetPart.transform);
                instance.Play();
                thunderboltImpactEffectData.Spawn(creature.ragdoll.targetPart.transform).Play();
                creature.SetVariable("BoltHits", 0);
                boltEffectData.Spawn(creature.ragdoll.targetPart.transform).Play();
                var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                brainModuleCrystal.Crystallise(5);
                brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, "Lightning"), "Lightning");
                foreach (var creature1 in Creature.InRadius(creature.ragdoll.targetPart.transform.position, 5))
                {
                    if (creature1.isPlayer) continue;
                    for (var i = 0; i < Random.Range(1, 2); i++)
                    {
                        zapEffectData.Spawn(creature1.ragdoll.targetPart.transform).Play();
                        spell?.PlayBolt(creature.ragdoll.targetPart.transform, creature1.ragdoll.targetPart.transform);
                    }

                    var brainModuleCrystal1 = creature1.brain.instance.GetModule<BrainModuleCrystal>();
                    brainModuleCrystal1.Crystallise(5);
                    brainModuleCrystal1.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, "Lightning"), "Lightning");
                    creature1.Inflict("Electrocute", this, 5);
                }
            }
        }
    }
}