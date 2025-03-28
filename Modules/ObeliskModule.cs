using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace Crystallic.Modules;

public class ObeliskModule : GameModeModule
{
    [ModOption("Play Obelisk Vfx", "Controls whether placing a crystal in the obelisk plays custom Vfx or not."), ModOptionCategory("Obelisk", 1)]
    public static bool playObeliskVfx = true;

    protected EffectData coreEffectData;
    public string coreEffectId;
    protected EffectData effectData;
    public string effectId;
    protected List<EffectInstance> effectInstances = new();
    public string levelId;
    protected List<SkillTreeReceptacle> skillTreeReceptacles = new();
    public List<string> triggerItemIds = new();

    public override IEnumerator OnLoadCoroutine()
    {
        EventManager.onLevelLoad += OnLevelLoad;
        EventManager.onLevelUnload += OnLevelUnload;
        effectData = Catalog.GetData<EffectData>(effectId);
        coreEffectData = Catalog.GetData<EffectData>(coreEffectId);
        return base.OnLoadCoroutine();
    }

    private void OnLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd && levelData.id == levelId)
        {
            foreach (var skillTreeReceptacle in SkillTree.instance.receptacles)
            {
                skillTreeReceptacle.itemMagnet.OnItemCatchEvent += OnItemCatchEvent;
                skillTreeReceptacle.itemMagnet.OnItemReleaseEvent += OnItemReleaseEvent;
                skillTreeReceptacles.Add(skillTreeReceptacle);
            }
        }
    }

    private void OnLevelUnload(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd && levelData.id == levelId)
            foreach (var skillTreeReceptacle in skillTreeReceptacles)
            {
                skillTreeReceptacle.itemMagnet.OnItemCatchEvent -= OnItemCatchEvent;
                skillTreeReceptacle.itemMagnet.OnItemReleaseEvent -= OnItemReleaseEvent;
            }

        skillTreeReceptacles.Clear();
    }

    private void OnItemCatchEvent(Item item, EventTime time)
    {
        if (!triggerItemIds.Contains(item.data.id) || time == EventTime.OnStart || !playObeliskVfx) return;
        foreach (var skillTreeReceptacle in SkillTree.instance.receptacles)
        {
            if (item.magnets.Contains(skillTreeReceptacle.itemMagnet)) continue;
            var coreEffectInstance = coreEffectData.Spawn(SkillTree.instance.shardReceptacle.transform);
            effectInstances.Add(coreEffectInstance);
            coreEffectInstance.Play();
            var effectInstance = effectData.Spawn(skillTreeReceptacle.itemMagnet.transform);
            effectInstances.Add(effectInstance);
            effectInstance.Play();
        }

        if (time == EventTime.OnEnd)
        {
            foreach (var skillTreeReceptacle in SkillTree.instance.receptacles)
            {
                foreach (SkillTree.SkillTreeTierNode tierNode in skillTreeReceptacle.mainTierNodes)
                {
                    var capsule = tierNode?.skillOrbTierBlocker?.mainCollider as CapsuleCollider;
                    if (capsule != null)
                    {
                        capsule.radius = 0.025f;
                        tierNode.skillOrbTierBlocker.handle.touchRadius = 0.3f;
                    }
                }
            }
        }
    }

    private void OnItemReleaseEvent(Item item, EventTime time)
    {
        if (triggerItemIds.Contains(item.data.id) && time == EventTime.OnEnd)
            foreach (var effectInstance in effectInstances)
                effectInstance.End();
    }
}