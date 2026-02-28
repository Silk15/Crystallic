using System;
using ThunderRoad;
using TriInspector;
using UnityEngine;

namespace Crystallic;

[Serializable]
public class DyeMixture
{
    [Dropdown(nameof(GetAllSpellID))]
    public string mixSpellId;
    public Color mixColor;

    public TriDropdownList<string> GetAllSpellID() => Catalog.GetDropdownAllID<SpellData>();
}