using System;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using TriInspector;

namespace Crystallic.Skill.Spell
{
    public class SkillCrystalPunch : SkillSpellPunch
    {
        [NonSerialized]
        public DyeData dyeData;

        [Dropdown(nameof(GetAllDyeID))]
        public string dyeDataId = "DyeBody";

        #if !SDK
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            dyeData = Catalog.GetData<DyeData>(dyeDataId);
        }

        public override void OnPunchHit(RagdollHand hand, CollisionInstance hit, bool fist)
        {
            base.OnPunchHit(hand, hit, fist);
            if (fist)
            {
                ThunderEntity thunderEntity = hit.targetColliderGroup?.collisionHandler?.Entity;
                if (!thunderEntity || thunderEntity is not Creature creature) return;
                creature.Inflict(statusData, this, duration, new CrystallisedParams(Dye.GetEvaluatedColor(creature.GetCurrentCrystallisationId(), "Body"), "Body"));
            }
        }
        #endif

        public TriDropdownList<string> GetAllDyeID() => Catalog.GetDropdownAllID<DyeData>();
    }
}