using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;

namespace Crystallic.Skill;

public class SkillShardDilation : SpellSkillData
{
    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.speedUpByTimeScale = true;
        spellCastCrystallic.OnShardHit += OnShardHit;
    }

    private void OnShardHit(SpellCastCrystallic spellCastCrystallic, ThunderEntity entity, SpellCastCrystallic.ShardshotHit hitInfo)
    {
        if (hitInfo.hitEntity != null && hitInfo.hitEntity is Creature creature && creature != spellCastCrystallic.spellCaster.mana.creature && SkillSlowTimeData.timeSlowed) creature.Inflict("Slowed", this, 5);
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.speedUpByTimeScale = false;
        spellCastCrystallic.OnShardHit -= OnShardHit;
    }
}