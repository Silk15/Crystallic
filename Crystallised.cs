using Crystallic.AI;
using ThunderRoad;

namespace Crystallic;

public class Crystallised : Status
{
    public override void Apply()
    {
        base.Apply();
        if (entity is Creature creature && creature) creature.brain.instance.GetModule<BrainModuleCrystal>().Crystallise(5, "Lightning");
    }
}