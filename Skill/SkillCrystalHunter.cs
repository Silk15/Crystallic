using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic.Skill;

public class SkillCrystalHunter : SpellSkillData
{
    [ModOption("Cluster Thickness", "Controls the thickness of the cluster trigger."), ModOptionCategory("Crystal Hunter", 5), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float thickness = 1.3f;

    [ModOption("Cluster Height", "Controls the height of the cluster trigger."), ModOptionCategory("Crystal Hunter", 5), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float height = 1.5f;

    [ModOption("Cluster Lifetime", "Controls the lifetime of each cluster."), ModOptionCategory("Crystal Hunter", 5), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float duration = 5f;

    private EffectData clusterEffectData;

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        clusterEffectData = Catalog.GetData<EffectData>("CrystalCluster");
    }

    public override void OnSkillLoaded(SkillData skillData, Creature creature)
    {
        base.OnSkillLoaded(skillData, creature);
        Stinger.onStingerSpawn += OnStingerSpawn;
    }

    private void OnStingerSpawn(Stinger stinger)
    {
        stinger.onStingerStab += OnStingerStab;
    }

    private void OnStingerStab(Stinger stinger, Damager damager, CollisionInstance collision, Creature hitCreature)
    {
        stinger.onStingerStab -= OnStingerStab;
        if (hitCreature != null) return;
        var cluster = CrystalCluster.Create(collision.contactPoint, Quaternion.LookRotation(collision.contactNormal, collision.sourceColliderGroup.transform.up));
        cluster.Init(stinger.lerper.currentSpellId, null, clusterEffectData, thickness, height, 1, 0, duration, false, false);
    }

    public override void OnSkillUnloaded(SkillData skillData, Creature creature)
    {
        base.OnSkillUnloaded(skillData, creature);
        Stinger.onStingerSpawn -= OnStingerSpawn;
    }
}