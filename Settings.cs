using System.Collections.Generic;
using ThunderRoad;

namespace Crystallic;

public class Settings : CustomData
{
    [ModOption("Debug Mode", "Debugging option for testers. This will spam your log, please do not complain to me if you find it annoying."), ModOptionCategory("Debug", 99)]
    public static bool debug = false;

    public string endingMusicEffectId;
    public Dictionary<TowerLaserType, float> endingTimings = new();
    public string laserAnimatorControllerAddress;
    public string laserFireEffectId;
    public string laserLoadEffectId;
    public string laserMechanicsEffectId;
    public Dictionary<string, List<string>> skills = new();
    public string spellId;
    public string wellEffectId = "EndingWell";
}