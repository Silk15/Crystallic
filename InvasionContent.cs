using System.Collections.Generic;
using Sirenix.OdinInspector;
using ThunderRoad;

namespace Crystallic;

public class InvasionContent : ContainerContent
{
    public bool invasionComplete;
    public InvasionContent() { }

    public InvasionContent(InvasionContent invasionContent)
    {
        invasionComplete = invasionContent.invasionComplete;
    }

    public override CatalogData catalogData => new();

    public static InvasionContent GetCurrent()
    {
        var current = (InvasionContent)Player.local.creature.container.contents.Find(c => c is InvasionContent);
        if (current == null)
        {
            current = new InvasionContent();
            Player.local.creature.container.contents.Add(current);
        }

        return current;
    }

    public override ContainerContent Clone()
    {
        return new InvasionContent(this);
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