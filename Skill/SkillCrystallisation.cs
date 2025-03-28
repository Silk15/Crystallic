using Crystallic.AI;
using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;

namespace Crystallic.Skill;

public class SkillCrystallisation : SpellSkillData
{
    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnShardHit += OnShardHit;
    }

    private void OnShardHit(SpellCastCrystallic spellCastCrystallic, ThunderEntity entity, SpellCastCrystallic.ShardshotHit hitInfo)
    {
        if (entity is Creature creature && hitInfo.hitPart && !hitInfo.hitPart.hasMetalArmor && !creature.isPlayer)
        {
            var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            brainModuleCrystal.Crystallise(5, "Crystallic");
            brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, "Crystallic"), "Crystallic");
        }
    }


    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastCrystallic spellCastCrystallic)) return;
        spellCastCrystallic.OnShardHit -= OnShardHit;
    }
}