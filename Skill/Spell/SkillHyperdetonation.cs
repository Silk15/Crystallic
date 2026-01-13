using System.Collections.Generic;
using Crystallic;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill.Spell;

public class SkillHyperdetonation : SpellSkillData
{
    public float depthRequirementRatio = 0.9f;
    public float eventResetRatio = 0.2f;
    public bool requireUnpenetrateToReset = true;
    public float minVelocity = 0.25f;

    public string detonationEffectId = "Hyperdetonation";
    public EffectData detonationEffectData;
    
    public List<SkillSpellPair> skillSpellPairs = new();

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        detonationEffectData = Catalog.GetData<EffectData>(detonationEffectId);
    }

    public override void OnImbueLoad(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        foreach (SkillSpellPair skillSpellPair in skillSpellPairs)
            if (skillSpellPair.spellId == imbue.spellCastBase.id && imbue.imbueCreature.HasSkill(skillSpellPair.skillId))
            {
                MaxDepthDetector maxDepthDetector = imbue.colliderGroup.collisionHandler.item.GetOrAddComponent<MaxDepthDetector>();
                if (maxDepthDetector == null) 
                    return;
                
                List<Damager> damagers = MaxDepthDetector.GetValidDamagers(imbue.colliderGroup.collisionHandler.damagers, false);
                
                if (damagers.Count <= 0) 
                    return;
                
                maxDepthDetector.Activate(this, damagers, depthRequirementRatio, eventResetRatio, requireUnpenetrateToReset);
                maxDepthDetector.onPenetrateMaxDepthEvent -= OnPenetrateMaxDepthEvent;
                maxDepthDetector.onPenetrateMaxDepthEvent += OnPenetrateMaxDepthEvent;
            }
    }

    public override void OnImbueUnload(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        MaxDepthDetector maxDepthDetector = imbue.colliderGroup.collisionHandler.item.GetComponent<MaxDepthDetector>();
        
        if (maxDepthDetector == null)
            return;
        
        maxDepthDetector.Deactivate(this);
        maxDepthDetector.onPenetrateMaxDepthEvent -= OnPenetrateMaxDepthEvent;
    }
    
    private void OnPenetrateMaxDepthEvent(Damager damager, CollisionInstance collision, Vector3 velocity, float depth)
    {
        Creature hitCreature = collision?.targetColliderGroup?.collisionHandler?.ragdollPart?.ragdoll?.creature;
        
        if (!hitCreature || !hitCreature.HasStatus("Crystallised") || collision.impactVelocity.sqrMagnitude < minVelocity * minVelocity || hitCreature.isPlayer)
            return;
        
        hitCreature.Shred();
        hitCreature.AddExplosionForce(60, collision.contactPoint, 2.5f, 0.5f, ForceMode.Impulse);

        BrainModuleCrystal brainModuleCrystal = hitCreature.brain.instance.GetModule<BrainModuleCrystal>();
        Color color = brainModuleCrystal.lerper.targetColor;

        EffectInstance detonationEffect = detonationEffectData.Spawn(hitCreature.ragdoll.targetPart.transform);
        detonationEffect.SetColor(color);
        detonationEffect.Play();
    }
}