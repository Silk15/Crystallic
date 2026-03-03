#if !SDK
using ThunderRoad;

namespace Crystallic.EnemyToggling
{
    public class ToggledSpell : ToggledSkill
    {
        public ToggledSpell(Creature creature, string spellId)
        {
            this.creature = creature;
            id = spellId;
        }

        public ToggledSpell()
        {
        }

        public override void Load()
        {
            active = true;
            if (creature.mana.TryGetSpell(id, out SpellData _))
                return;
            creature.container.AddSpellContent(id);
        }
    }
}
#endif