using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using ThunderRoad;
using ThunderRoad.Skill;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillLithowebs : SpellSkillData
{
    
    public List<Imbue> gravImbues = new();

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        EventManager.onRagdollSliced -= OnRagdollSliced;
        EventManager.onRagdollSliced += OnRagdollSliced;
    }

    private void OnRagdollSliced(RagdollPart part, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart) return;
        for (int i = 0; i < gravImbues.Count; i++)
        {
            var imbue = gravImbues[i];
            foreach (CollisionInstance collisionInstance in imbue.colliderGroup.collisionHandler.item.mainCollisionHandler.collisions)
            {
                if (collisionInstance?.targetColliderGroup?.collisionHandler?.ragdollPart == part)
                {
                    var lastCollision = collisionInstance;
                    if (lastCollision?.sourceColliderGroup?.collisionHandler?.item is Item item && IsImbueInList(item) && Vector3.Distance(item.transform.position, part.transform.position) < 2.5f)
                    {
                        if (part.parentPart != null) part.gameObject.AddComponent<Lithoweb>().Init(item, part, part.parentPart);
                    }
                }
            }
        }
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        EventManager.onRagdollSliced -= OnRagdollSliced;
    }

    public override void OnImbueLoad(SpellData spell, Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        if (imbue.spellCastBase is SpellCastGravity && !gravImbues.Contains(imbue)) gravImbues.Add(imbue);
    }

    public override void OnImbueUnload(SpellData spell, Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        if (imbue.spellCastBase is SpellCastGravity && gravImbues.Contains(imbue)) gravImbues.Remove(imbue);
    }

    public bool IsImbueInList(Item item)
    {
        for (var i = 0; i < item.imbues.Count; i++)
            if (gravImbues.Contains(item.imbues[i]))
                return true;
        return false;
    }
}