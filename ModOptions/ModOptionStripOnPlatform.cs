#if !SDK
using ThunderRoad;
using UnityEngine;
using QualityLevel = ThunderRoad.QualityLevel;

namespace Crystallic;

public class ModOptionStripOnPlatform : ModOptionAttribute
{
    public QualityLevel qualityLevel;
    
    public ModOptionStripOnPlatform(QualityLevel qualityLevel) => this.qualityLevel = qualityLevel;

    public override void Process()
    {
        base.Process();
        if (Common.GetQualityLevel() == qualityLevel && modOption.uiComponent is MonoBehaviour monoBehaviour)
            monoBehaviour.gameObject.SetActive(false);
    }
}
#endif