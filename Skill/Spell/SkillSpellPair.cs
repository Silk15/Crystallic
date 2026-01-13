using System;
using ThunderRoad;

namespace Crystallic.Skill.Spell;

[Serializable]
public class SkillSpellPair
{
    public string statusId;
    public string skillId;
        
    [JsonMergeKey]
    public string spellId;
    public float statusDuration;
        
    private StatusData statusData;

    public StatusData StatusData
    {
        get => statusData ?? (statusData = statusId.IsNullOrEmptyOrWhitespace() ? null : Catalog.GetData<StatusData>(statusId));
        set => statusData = value;
    }
}