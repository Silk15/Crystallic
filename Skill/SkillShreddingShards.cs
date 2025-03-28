using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;

namespace Crystallic.Skill;

public class SkillShreddingShards : SpellSkillData
{
    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnShardHit += OnShardHit;
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnShardHit -= OnShardHit;
    }

    private void OnShardHit(SpellCastCrystallic spellCastCrystallic, ThunderEntity entity, SpellCastCrystallic.ShardshotHit hitInfo)
    {
        if (hitInfo.hitPart != null && !hitInfo.wasMetal && hitInfo.hitPart.sliceAllowed && !hitInfo.hitPart.ragdoll.creature.isPlayer)
        {
            hitInfo.hitPart.RunAfter(() =>
            {
                hitInfo.hitPart.TrySlice();
                hitInfo.hitPart.ragdoll.creature.Kill();
            }, 0.05f);
        }
    }
}