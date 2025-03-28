using System.Collections.Generic;
using Sirenix.OdinInspector;
using ThunderRoad;

namespace Crystallic;

public class StartContent : ContainerContent
{
    public bool loreFound;
    public StartContent() { }

    public StartContent(StartContent invasionContent)
    {
        loreFound = invasionContent.loreFound;
    }

    public override CatalogData catalogData => new();

    public static StartContent GetCurrent()
    {
        var current = (StartContent)Player.local.creature.container.contents.Find(c => c is StartContent);
        if (current == null)
        {
            current = new StartContent();
            Player.local.creature.container.contents.Add(current);
        }

        return current;
    }

    public override ContainerContent Clone()
    {
        return new StartContent(this);
    }

    public override List<ValueDropdownItem<string>> DropdownOptions()
    {
        return new List<ValueDropdownItem<string>>();
    }

    public override string GetTypeString()
    {
        return GetType().Name;
    }

    public override bool OnCatalogRefresh()
    {
        return true;
    }
}