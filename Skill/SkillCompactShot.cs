using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;

namespace Crystallic.Skill;

public class SkillCompactShot : SpellSkillData
{
    [ModOption("Shardshot Angle", "Controls the angle of shardshots with this skill unlocked"), ModOptionCategory("Compact Shot", 4), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float shardshotAngle = 15;

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnShardshotStart += OnShardshotStart;
    }

    private void OnShardshotStart(SpellCastCrystallic spellCastCrystallic, EffectInstance effectInstance)
    {
        spellCastCrystallic.SetShardshotAngle(shardshotAngle);
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.SetShardshotAngle(25);
        spellCastCrystallic.OnShardshotStart -= OnShardshotStart;
    }
}