using Crystallic.AI;
using Crystallic.Skill;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic;

public class ImbueLightningBehavior : ImbueBehavior
{
    public string boltEffectId = "SpellLightningBoltZapSingle";

    public AnimationCurve zapCurve = new(new Keyframe(0.0f, 0.5f), new Keyframe(0.05f, 10), new Keyframe(0.1f, 0.5f));
    public string zapEffectId = "BoltZap";
    private readonly Vector2 boltCooldown = new(0.15f, 0.1f);
    protected EffectData boltEffectData;
    private float lastBoltTime;
    protected SpellCastCharge spellCastLightning;
    protected EffectData zapEffectData;

    public void Update()
    {
        if (!isActive) return;
        var item = imbue?.colliderGroup?.collisionHandler?.item;
        if (item && item.isFlying)
            foreach (var thunderEntity in ThunderEntity.InRadius(imbue.transform.position, 5f))
                TrySpawnBolt(thunderEntity);
    }

    public override void Activate(Imbue imbue, SkillCrystalImbueHandler handler)
    {
        base.Activate(imbue, handler);
        spellCastLightning = imbue.spellCastBase;
        boltEffectData = Catalog.GetData<EffectData>(boltEffectId);
        zapEffectData = Catalog.GetData<EffectData>(zapEffectId);
    }

    public void TrySpawnBolt(ThunderEntity thunderEntity)
    {
        if (Time.time - lastBoltTime > Random.Range(boltCooldown.x, boltCooldown.y))
        {
            lastBoltTime = Time.time;
            if (thunderEntity is Creature creature && creature && creature != imbue.imbueCreature)
            {
                creature?.TryPush(Creature.PushType.Magic, (creature.ragdoll.targetPart.transform.position - transform.position).normalized, 1);
                creature?.Inflict("Electrocute", this, 5);
                var brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
                brainModuleCrystal?.Crystallise(5);
                brainModuleCrystal?.SetColor(Dye.GetEvaluatedColor(brainModuleCrystal.lerper.currentSpellId, "Lightning"), "Lightning");
                SpawnBolt(transform, thunderEntity.transform);
            }
            else if (thunderEntity is Item item && item && item.holder == null && item.magnets.IsNullOrEmpty())
            {
                if (item && item?.mainHandler && item?.mainHandler?.creature != null && item.mainHandler.creature != imbue.imbueCreature)
                {
                    item?.mainHandler?.creature?.TryPush(Creature.PushType.Magic, (item.mainHandler.creature.ragdoll.targetPart.transform.position - item.transform.position).normalized, 1);
                    item?.mainHandler?.creature?.Inflict("Electrocute", this, 5);
                    if (item?.handles.Count > 0)
                        foreach (var handle in item?.handles)
                            handle.Release();
                }

                foreach (var colliderGroup in item.colliderGroups)
                    if (colliderGroup && colliderGroup.imbueEffectRenderer != null)
                        colliderGroup?.imbue?.Transfer(spellCastLightning, 30 * Time.deltaTime);
                if (item.lastHandler != null) item?.lastHandler?.PlayHapticClipOver(zapCurve, 0.25f);
                SpawnBolt(transform, thunderEntity.transform);
            }
        }
    }

    public override void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge, Creature hitCreature = null, Item hitItem = null)
    {
        base.Hit(collisionInstance, spellCastCharge, hitCreature, hitItem);
        foreach (var thunderEntity in ThunderEntity.InRadius(collisionInstance.contactPoint, 4))
        {
            if (hitCreature != null && thunderEntity == hitCreature) continue;
            if (hitItem != null && thunderEntity == hitItem) continue;
            TrySpawnBolt(thunderEntity);
        }
    }

    public void SpawnBolt(Transform source, Transform target)
    {
        var sourceObject = new GameObject(source.name);
        sourceObject.transform.position = source.position;
        sourceObject.transform.rotation = source.rotation;
        sourceObject.transform.parent = target;
        Destroy(sourceObject, 1);
        var targetObject = new GameObject(target.name);
        targetObject.transform.position = target.position;
        targetObject.transform.rotation = target.rotation;
        targetObject.transform.parent = target;
        Destroy(targetObject, 1);
        zapEffectData.Spawn(target.transform).Play();
        SpellCastLightning.PlayBolt(boltEffectData, sourceObject.transform, targetObject.transform, sourceObject.transform.position, targetObject.transform.position);
    }
}