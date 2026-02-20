using System;
using System.Collections.Generic;
using ThunderRoad;

namespace Crystallic.EnemyToggling;

public class EnemyToggleData : CustomData
{
    public List<Category> categories = new();

    [Serializable]
    public class Category
    {
        public string id;
        public string[] skillIds;
    }
}