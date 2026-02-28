using System;
using Newtonsoft.Json;
using ThunderRoad;
using TriInspector;

namespace Crystallic.Skill.Spell;

[Serializable, DeclareTabGroup("Tabs")]
public class SkillSpellPair
{
    [Group("Tabs"), Tab("Status"), Dropdown(nameof(GetAllStatusID))]
    public string statusId;
        
    [Group("Tabs"), Tab("Status")]
    public float statusDuration;
        
    [Group("Tabs"), Tab("Skill & Spell"), Dropdown(nameof(GetAllSkillID))]
    public string skillId;
        
    [Group("Tabs"), Tab("Skill & Spell"), JsonMergeKey, Dropdown(nameof(GetAllSkillID))]
    public string spellId;

    private StatusData statusData;

    [JsonIgnore]
    public StatusData StatusData
    {
        get => statusData ?? (statusData = statusId.IsNullOrEmptyOrWhitespace() ? null : Catalog.GetData<StatusData>(statusId));
        set => statusData = value;
    }

    public TriDropdownList<string> GetAllSkillID() => Catalog.GetDropdownAllID(Category.Skill);
        
    public TriDropdownList<string> GetAllStatusID() => Catalog.GetDropdownAllID(Category.Status);
}