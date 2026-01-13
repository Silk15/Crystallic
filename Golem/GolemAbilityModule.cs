using System.Collections;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace Crystallic.Golem;

public class GolemAbilityModule : GameModeModule
{
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
        
        foreach (GolemAbility ability in ThunderRoad.Golem.local.abilities)
            if (ability is GolemThrow golemAbility)
            {
                Debug.Log(golemAbility.holdForce);
                Debug.Log(golemAbility.holdDamper);
                Debug.Log(golemAbility.holdPosition);
            }
    }
}