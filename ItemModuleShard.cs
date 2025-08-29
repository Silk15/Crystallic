using Crystallic.Skill;
using ThunderRoad;

namespace Crystallic;

public class ItemModuleShard : ItemModule
{
    public override void OnItemLoaded(Item item)
    {
        base.OnItemLoaded(item);
        item.gameObject.GetOrAddComponent<Shard>();
    }
}