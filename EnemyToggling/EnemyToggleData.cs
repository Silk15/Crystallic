using System;
using System.Collections.Generic;
using ThunderRoad;
using TriInspector;

namespace Crystallic.EnemyToggling
{
    public class EnemyToggleData : CustomData
    {
        public List<Category> categories = new();

        #if UNITY_EDITOR
        public override string GetCatalogPath()
        {
            string result = $"EnemyToggles";
            return result;
        }
        #endif

        [Serializable]
        public class Category
        {
            public string id;

            [DropdownList(nameof(GetAllSkillID))]
            public string[] skillIds;

            public TriDropdownList<string> GetAllSkillID() => Catalog.GetDropdownAllID(ThunderRoad.Category.Skill);
        }
    }
}