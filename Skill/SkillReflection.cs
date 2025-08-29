using System.Collections;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillReflection : SpellSkillData
{
    public float targetMoveBias;
    public float maxDistance;
    public float maxAngle;
    public bool aimAssist;
    public string reflectEffectId;
    private EffectData reflectEffectData;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        reflectEffectData = Catalog.GetData<EffectData>(reflectEffectId);
    }

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (spell is not SpellCastCrystallic spellCastCrystallic) return;
        spellCastCrystallic.onShardHit -= OnShardHit;
        spellCastCrystallic.onShardHit += OnShardHit;
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (spell is not SpellCastCrystallic spellCastCrystallic) return;
        spellCastCrystallic.onShardHit -= OnShardHit;
    }

    private void OnShardHit(SpellCastCrystallic spellCastCrystallic, Shard shard, CollisionInstance collisionInstance) => shard.Reflect(collisionInstance, reflectEffectData, aimAssist, targetMoveBias, maxDistance, maxAngle);
}