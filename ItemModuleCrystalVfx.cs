using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;

namespace Crystallic;

public class ItemModuleCrystalVfx : ItemModule
{
    #if !SDK
    public static readonly int SourceColor = Shader.PropertyToID("Source Color");
    
    public override void OnItemLoaded(Item thisItem)
    {
        base.OnItemLoaded(thisItem);
        Catalog.GetData<ItemData>("CrystalBodyT1").SpawnAsync(bodyItem =>
        {
            SkillTreeCrystal thisCrystal = thisItem.GetComponent<SkillTreeCrystal>();
            SkillTreeCrystal bodyCrystal = bodyItem.GetComponent<SkillTreeCrystal>();
            
            if (thisCrystal == null)
            {
                Debug.LogError($"[Crystallic] No SkillTreeCrystal component found on item: {itemData.id}!");
                return;
            }

            GameObject visualEffectParent = Object.Instantiate(Common.IsWindows ? bodyCrystal.linkVfxWindows.transform.parent.gameObject : bodyCrystal.linkVfxAndroid.transform.parent.gameObject, thisCrystal.transform);
            visualEffectParent.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var linkVfx = visualEffectParent.transform.GetChild(1).GetComponent<VisualEffect>();
            var mergeVfx = visualEffectParent.transform.GetChild(0).GetComponent<VisualEffect>();

            var linkVfxTarget = linkVfx.transform.GetChild(1);
            var mergeVfxTarget = mergeVfx.transform.GetChild(1);

            thisCrystal.linkVfx = linkVfx;
            thisCrystal.mergeVfx = mergeVfx;
            thisCrystal.linkVfxTarget = linkVfxTarget;
            thisCrystal.mergeVfxTarget = mergeVfxTarget;
            
            linkVfx.SetVector4(SourceColor, thisCrystal.skillTreeEmissionColor);
            mergeVfx.SetVector4(SourceColor, thisCrystal.skillTreeEmissionColor);
            bodyItem.Despawn();
        });
    }
    #endif
}
