using System;
using System.Collections.Generic;
using ThunderRoad;

namespace Crystallic.Filters
{
    [Serializable]
    public class ItemTypeFilter : ConditionFilter<ItemData.Type, List<ItemData.Type>>
    {
        public override bool Allows(ItemData.Type item)
        {
            bool contains = filter.Contains(item);

            switch (filterLogic)
            {
                case FilterLogic.AnyExcept:
                    return !contains;
                case FilterLogic.NoneExcept:
                    return contains;
                default:
                    return true;
            }
        }
    }
}