using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill.Imbue;

public class ImbueLightningBehaviour : ImbueBehaviour
{
    public float randomStartDelay = 0.5f;
    public float boltRange = 3f;
    public float boltPeriod = 0.5f;
    public float boltPeriodVariance = 0.25f;
    
    public ColliderGroup colliderGroup;
    public Item item;

    private SpellCastLightning spellCastLightning;
    private float nextBoltTime;

    public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

    public override void Load(CrystalImbueSkillData crystalImbueSkillData, ThunderRoad.Imbue imbue)
    {
        base.Load(crystalImbueSkillData, imbue);
        spellCastLightning = imbue.spellCastBase as SpellCastLightning;
        nextBoltTime = Time.time + Random.Range(0, randomStartDelay);
        item = imbue.colliderGroup.collisionHandler.item;
        item.OnThrowEvent += OnThrow;
        item.OnFlyStartEvent += OnFlyStart;
        colliderGroup = imbue.colliderGroup;
    }

    private void OnThrow(Item item1) => nextBoltTime = Time.time + Random.Range(0, randomStartDelay);

    public override void Unload(ThunderRoad.Imbue imbue)
    {
        base.Unload(imbue);
        item.OnFlyStartEvent -= OnFlyStart;
        item.OnThrowEvent -= OnThrow;
    }

    private void OnFlyStart(Item item) => nextBoltTime = Time.time + Random.Range(0, randomStartDelay);

    protected override void ManagedUpdate()
    {
        base.ManagedUpdate();
        if (spellCastLightning == null || Time.time > nextBoltTime || !item.isFlying) 
            return;
        
        nextBoltTime = Time.time + Random.Range(0, boltPeriod + boltPeriodVariance);
        SpellCastLightning.BoltHit boltHit = GetHit(transform.position, boltRange);
        
        if (boltHit.collider == null) return;
        spellCastLightning.Hit(boltHit.collider.GetComponentInParent<ColliderGroup>(), boltHit.closestPoint, boltHit.normal, boltHit.direction, 1f, false, null, 1f, null, boltHit.collider);
        spellCastLightning.PlayBolt(this.colliderGroup.ClosestPoint(boltHit.closestPoint), boltHit.closestPoint);
    }

    public SpellCastLightning.BoltHit GetHit(Vector3 origin, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(origin, radius, Utils.GetProjectileRaycastMask(), QueryTriggerInteraction.Ignore);
        float minDistance = float.MaxValue;
        SpellCastLightning.BoltHit boltHit = new SpellCastLightning.BoltHit();
        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            Vector3 colliderDirection = collider.transform.position - origin;
            if (!Physics.Raycast(origin, Utils.GetRandomVelocityInCone(colliderDirection, 45f, 1f), out RaycastHit hit, radius * 2, Utils.GetProjectileRaycastMask(), QueryTriggerInteraction.Ignore))
                continue;
            
            ColliderGroup group = collider.GetComponentInParent<ColliderGroup>();
            
            if (group != null && (group.collisionHandler.item == null || group.collisionHandler.item.data.id == "Shard")) 
                continue;
            
            if (hit.distance > minDistance) 
                continue;

            minDistance = hit.distance;
            boltHit.collider = collider;
            boltHit.closestPoint = hit.point;
            boltHit.normal = hit.normal;
            boltHit.direction = -hit.normal;

            if (group != null && (group.isMetal || group.collisionHandler.isBreakable || collider.GetComponentInParent<Creature>()))
                break;
        }

        return boltHit;
    }
}