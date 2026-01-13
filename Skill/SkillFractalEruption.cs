using System.Collections.Generic;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillFractalEruption : SkillData
{
    public SpellCastCrystallic spellCastCrystallic;
    public EffectData projectileEffectData;
    public string projectileEffectId;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        projectileEffectData = Catalog.GetData<EffectData>(projectileEffectId);
        spellCastCrystallic = Catalog.GetData<SpellCastCrystallic>("Crystallic");
    }

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        BrainModuleCrystal.onCreatureCrystallised -= OnCreatureCrystallised;
        BrainModuleCrystal.onCreatureCrystallised += OnCreatureCrystallised;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        BrainModuleCrystal.onCreatureCrystallised -= OnCreatureCrystallised;
    }

    private void OnCreatureCrystallised(BrainModuleCrystal brainModuleCrystal, Creature creature, bool active)
    {
        if (active || creature == null || Random.Range(0, 2) != 1) return;
        List<Vector3> directions = new();

        int randomMax = Random.Range(1, 4);
        for (int i = 0; i < randomMax; i++)
        {
            Vector3 point = Vector3.zero;
            do
            {
                point = Random.insideUnitSphere;
            } while (Vector3.Dot(point, Vector3.up) < 0.5f);

            directions.Add(point);
        }

        List<Shard> shards = new();
        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 direction = directions[i];
            spellCastCrystallic.FireShard(projectileEffectData, creature.ragdoll.targetPart.transform.position + direction, direction * 8f, 3f, 0.25f, shard =>
            {
                shards.Add(shard);
                shard.item.IgnoreRagdollCollision(creature.ragdoll);
                shard.crystalliseFunc = creature1 => creature1 != creature && !creature1.isPlayer;
                shard.RunAfter(() =>
                {
                    shard.homing = true;
                    shard.homingIgnoredCreature = creature;
                    shard.targetCreature = shard.GetTargetCreature(shard.item.Velocity, 180f);
                }, 0.4f);
            });
        }
    }
}