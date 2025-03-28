using Crystallic.Skill.Spell;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillThickSkin : SpellSkillData
{
    public int key;
    public float damageMultiplier = 0;
    public Vector2 defaultRandomness;
    private static Vector2 randomness;
    private static Vector2 defaultStaticRandomness;
    public SpellCastCrystallic spellCastCrystallic;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        spellCastCrystallic = Catalog.GetData<SpellCastCharge>("Crystallic") as SpellCastCrystallic;
    }

    public static void SetRandomness(Vector2 randomness) => SkillThickSkin.randomness = randomness;
    
    public static void ClearRandomness() => randomness = defaultStaticRandomness;
    
    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        defaultStaticRandomness = defaultRandomness;
        EventManager.onCreatureHit += OnCreatureHit;
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        EventManager.onCreatureHit -= OnCreatureHit;
    }

    private void OnCreatureHit(Creature creature, CollisionInstance collisionInstance, EventTime eventTime)
    {
        if (creature.isPlayer && eventTime == EventTime.OnStart && Random.Range((int)randomness.x, (int)randomness.y) == key)
        {
            switch (collisionInstance.sourceColliderGroup?.collisionHandler?.Entity)
            {
                case Item item when item.owner != Item.Owner.Player && item.mainHandler != null:
                    Hit(item.mainHandler.creature);
                    break;
                case Creature creature1 when !creature.isPlayer:
                    Hit(creature1);
                    break;
            }
            if (spellCastCrystallic != null) spellCastCrystallic?.imbueCollisionEffectData?.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance?.targetCollider?.transform).Play();
            creature?.SetDamageMultiplier(this, damageMultiplier);
        }
        else if (creature.isPlayer && eventTime == EventTime.OnEnd) creature.RemoveDamageMultiplier(this);
        void Hit(Creature pushedCreature) => pushedCreature.TryPush(Creature.PushType.Parry, pushedCreature.transform.position - creature.transform.position, 1, RagdollPart.Type.Torso);
        
    }
}