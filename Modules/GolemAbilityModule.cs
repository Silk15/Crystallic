using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Modules;

namespace Crystallic.Modules;

public class GolemAbilityModule : GameModeModule
{
    public List<GolemAbility> abilities;

    public override IEnumerator OnLoadCoroutine()
    {
        Golem.OnLocalGolemSet += OnLocalGolemSet;
        return base.OnLoadCoroutine();
    }

    private void OnLocalGolemSet()
    {
        if (!abilities.IsNullOrEmpty()) Golem.local.abilities.AddRange(abilities);
    }
}