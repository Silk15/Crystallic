using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ThunderRoad;

namespace Crystallic;

public class LorePack
{
    public string contentAddress;
    public string groupId;
    public string itemId;
    public LorePackCondition.LoreLevelOptionCondition[] levelOptionConditions;
    public List<LoreScriptableObject.LoreData> lore;
    public string packId;
    public List<string> prerequisites;
    public LoreScriptableObject.LoreType type;
    public List<LorePackCondition.Visibility> visibilityConditions;

    public LoreScriptableObject.LorePack ToLorePack()
    {
        if (lore == null) return null;
        for (var index = 0; index < lore.Count; ++index)
        {
            lore[index].groupId = groupId;
            lore[index].displayGraphicsInJournal = false;
            var loreData = lore[index];
            if (loreData.itemId == null) loreData.itemId = itemId;
            lore[index].loreType = type;
            lore[index].contentAddress = contentAddress;
        }

        var uninitializedObject = (LorePackCondition)FormatterServices.GetUninitializedObject(typeof(LorePackCondition));
        uninitializedObject.visibilityRequired = visibilityConditions;
        uninitializedObject.levelOptions = levelOptionConditions;
        uninitializedObject.requiredParameters = Array.Empty<string>();
        var intList = new List<int>();
        var prerequisites = this.prerequisites;
        if (prerequisites != null && prerequisites.Count > 0)
            for (var index = 0; index < this.prerequisites.Count; ++index)
                intList.Add(LoreScriptableObject.GetLoreHashId(prerequisites[index]));
        return new LoreScriptableObject.LorePack
        {
            groupId = groupId,
            nameId = packId,
            hashId = LoreScriptableObject.GetLoreHashId(packId),
            loreData = lore,
            lorePackCondition = uninitializedObject,
            loreRequirement = intList,
            spawnPackAsOneItem = lore.Count > 1
        };
    }
}