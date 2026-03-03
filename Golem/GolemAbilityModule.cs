using System.Collections;
using ThunderRoad;
using ThunderRoad.Modules;

namespace Crystallic.Golem
{
    public class GolemAbilityModule : GameModeModule
    {
        #if !SDK
        public override IEnumerator OnLoadCoroutine()
        {
            ThunderRoad.Golem.OnLocalGolemSet += OnLocalGolemSet;
            return base.OnLoadCoroutine();
        }

        public override void OnUnload()
        {
            base.OnUnload();
            ThunderRoad.Golem.OnLocalGolemSet -= OnLocalGolemSet;
        }

        private void OnLocalGolemSet()
        {
            foreach (GolemAbilityData golemAbilityData in Catalog.GetDataList<GolemAbilityData>())
                ThunderRoad.Golem.local.abilities.Add(golemAbilityData.GetGolemAbility());
        }
        #endif
    }
}