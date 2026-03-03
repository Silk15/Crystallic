using System;
using ThunderRoad;

namespace Crystallic.Filters
{
    [Serializable]
    public abstract class ConditionFilter<TKey, TFilter>
    {
        public FilterLogic filterLogic;
        public TFilter filter;

        public abstract bool Allows(TKey item);
    }
}