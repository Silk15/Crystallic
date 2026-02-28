using System;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

[Serializable]
public class LiquidCrystallicPotion : LiquidData
{
    [Serializable]
    public class CrystallicChargeIndicator : Indicator
    {
        public override string GetName() => "(Crystallic Potion) Crystallic Charge";

        public override float GetValue(LiquidContainer container)
        {
            return 0;
        }
    }
}