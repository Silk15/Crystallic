using System.Collections.Generic;
using Sirenix.OdinInspector;
using ThunderRoad;
using UnityEngine.Events;

namespace Crystallic;

public class EndingContent : ContainerContent
{
    public bool endingComplete;
    public bool hasT4Skill;
    public EndingContent() { }

    public EndingContent(EndingContent endingContent)
    {
        endingComplete = endingContent.endingComplete;
        hasT4Skill = endingContent.hasT4Skill;
    }

    public override CatalogData catalogData => new();

    public static EndingContent GetCurrent()
    {
        Breakable breakable;
        var current = (EndingContent)Player.local.creature.container.contents.Find(c => c is EndingContent);
        if (current == null)
        {
            current = new EndingContent();
            Player.local.creature.container.contents.Add(current);
        }

        return current;
    }

    public override ContainerContent Clone()
    {
        return new EndingContent(this);
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