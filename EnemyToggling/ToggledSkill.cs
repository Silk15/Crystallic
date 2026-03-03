#if !SDK
using System;
using ThunderRoad;

namespace Crystallic.EnemyToggling
{
    [Serializable]
    public class ToggledSkill
    {
        public string id;
        public bool active;

        [NonSerialized]
        public Creature creature;

        public ToggledSkill(Creature creature, string skillId)
        {
            this.creature = creature;
            id = skillId;
        }

        public ToggledSkill()
        {
        }

        public virtual void Load()
        {
            active = true;
            if (creature.HasSkill(id))
                return;
            creature.container.AddSkillContent(id);
        }

        public virtual void Unload()
        {
            if (!creature.TryRemoveSkill(id))
                return;
            active = false;
        }
    }
}
#endif