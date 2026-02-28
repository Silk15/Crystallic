#if !SDK
using System.Collections.Generic;
using ThunderRoad;

namespace Crystallic;

public class ModOptionPlatformIndex : ModOptionAttribute
{
    public int[] qualityLevelValueIndexes;
    public QualityLevel[] qualityLevels;

    public ModOptionPlatformIndex(QualityLevel[] qualityLevels, int[] qualityLevelValueIndexes)
    {
        this.qualityLevelValueIndexes = qualityLevelValueIndexes;
        this.qualityLevels = qualityLevels;
    }

    public override void Process()
    {
        base.Process();
        for (int i = 0; i < qualityLevels.Length; i++)
            if (qualityLevels[i] == Common.GetQualityLevel())
                modOption.defaultValueIndex = qualityLevelValueIndexes[i];
    }
}
#endif