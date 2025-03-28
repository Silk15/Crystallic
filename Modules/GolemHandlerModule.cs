using System;
using System.Collections;
using System.Collections.Generic;
using Crystallic.AI;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace Crystallic.Modules;

public class GolemHandlerModule : GameModeModule
{
    public List<string> moduleAddresses = new();

    public override IEnumerator OnLoadCoroutine()
    {
        Golem.OnLocalGolemSet += OnLocalGolemSet;
        return base.OnLoadCoroutine();
    }

    public override void OnUnload()
    {
        base.OnUnload();
        Golem.OnLocalGolemSet -= OnLocalGolemSet;
    }

    private void OnLocalGolemSet()
    {
        Golem.local.Brain().Initialize(moduleAddresses);
    }
}