#if !SDK
using Crystallic;
using ThunderRoad;

namespace Crystallic
{
    public static class CrystallisationExtensions
    {
        public static string GetCurrentCrystallisationId(this Creature creature) => creature.brain.instance.GetModule<BrainModuleCrystal>().lerper.currentSpellId;
    }
}
#endif

