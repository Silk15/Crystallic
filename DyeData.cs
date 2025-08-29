using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

[Serializable]
public class DyeData : CustomData
{
    public string spellId;
    public Color color;
    public List<DyeMixture> dyeMixtures;
}