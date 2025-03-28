using Crystallic.AI;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillExplosiveEmbers : SpellSkillData
{
    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (!(spell is SpellCastProjectile spellCastProjectile)) return;
        spellCastProjectile.OnFireballHitEvent += OnFireballHit;
    }
    
    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (!(spell is SpellCastProjectile spellCastProjectile)) return;
        spellCastProjectile.OnFireballHitEvent -= OnFireballHit;
    }

    private void OnFireballHit(SpellCastProjectile spell, ItemMagicProjectile projectile, CollisionInstance collision, SpellCaster caster)
    {
        if (collision?.targetColliderGroup?.collisionHandler?.ragdollPart is RagdollPart ragdollPart)
        {
            var brainModuleCrystal = ragdollPart.ragdoll.creature.brain.instance.GetModule<BrainModuleCrystal>();
            if (!brainModuleCrystal.isCrystallised) return;
            var currentPart = ragdollPart;
            while (currentPart != null)
            {
                if (currentPart.sliceAllowed)
                {
                    currentPart?.TrySlice();
                    ragdollPart.ragdoll.creature.Inflict("Burning", this, parameter: 40);
                    brainModuleCrystal.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, "Fire"), "Fire");
                    currentPart.physicBody.AddForce((currentPart.transform.position - collision.contactPoint).normalized * 25, ForceMode.Impulse);
                    break;
                }
                currentPart = currentPart.parentPart;
            }
        }
    }
}