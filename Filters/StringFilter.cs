using System.Collections.Generic;
using ThunderRoad;

namespace Crystallic.Filters;

public class StringFilter : ConditionFilter<string, List<string>>
{
    public override bool Allows(string item)
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