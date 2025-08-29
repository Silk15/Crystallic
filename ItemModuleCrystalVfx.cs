using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;

namespace Crystallic;

public class ItemModuleCrystalVfx : ItemModule
{
    public override void OnItemLoaded(Item item)
    {
        base.OnItemLoaded(item);
        Item.OnItemSpawn -= OnItemSpawn;
        Item.OnItemSpawn += OnItemSpawn;
    }

    private void OnItemSpawn(Item item)
    {
        if (item.data.id != itemData.id) return;
        Catalog.GetData<ItemData>("CrystalBodyT1").SpawnAsync(item1 =>
        {
            if (item1.TryGetComponent(out SkillTreeCrystal originalCrystal) && item.TryGetComponent(out SkillTreeCrystal thisCrystal))
            {
                GameObject parent = new GameObject("VFX");
                parent.transform.SetParent(item.transform);
                parent.transform.localPosition = Vector3.zero;
                parent.transform.localRotation = Quaternion.identity;
                thisCrystal.SetField("mergeVfx", GameObject.Instantiate(originalCrystal.mergeVfxWindows, parent.transform));
                thisCrystal.SetField("linkVfx", GameObject.Instantiate(originalCrystal.linkVfxWindows, parent.transform));
                thisCrystal.SetField("mergeVfxTarget", parent.transform.GetChildrenByNameRecursive("Target")[0]);
                thisCrystal.SetField("linkVfxTarget", parent.transform.GetChildrenByNameRecursive("Target")[1]);
                var mergeVfx = thisCrystal.GetField("mergeVfx") as VisualEffect;
                var linkVfx = thisCrystal.GetField("linkVfx") as VisualEffect;
                mergeVfx?.SetVector4("Source Color", thisCrystal.skillTreeEmissionColor);
                linkVfx?.SetVector4("Source Color", thisCrystal.skillTreeEmissionColor);
                item1.Despawn(0.1f);
            }
        });
    }
}