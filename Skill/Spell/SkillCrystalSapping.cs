using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Pools;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace Crystallic.Skill.Spell;

public class SkillCrystalSapping : SkillSapStatusApplier
{
    public SkillThunderbolt skillThunderbolt;
    public EffectData pylonEffectData;
    public string pylonEffectId = "ShardPylon";
    
    public int requiredHitsUntilPylon;
    public int currentHits;

    public float zapRadius = 3f;

    public Vector2Int minMaxRequiredHits = new(30, 45);

    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        pylonEffectData = Catalog.GetData<EffectData>(pylonEffectId);
        skillThunderbolt = Catalog.GetData<SkillThunderbolt>("Thunderbolt");
    }

    public override void ApplyStatus(SpellCastLightning spell) => spell.AddStatus(this, statusData, statusDuration, param: new CrystallisedParams(Dye.GetEvaluatedColor("Crystallic", "Lightning"), "Lightning"));

    public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellLoad(spell, caster);
        if (spell is not SpellCastLightning spellCastLightning) return;
        spellCastLightning.OnBoltHitEvent -= OnBoltHit;
        spellCastLightning.OnBoltHitEvent += OnBoltHit;
    }

    public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
    {
        base.OnSpellUnload(spell, caster);
        if (spell is not SpellCastLightning spellCastLightning) return;
        spellCastLightning.OnBoltHitEvent -= OnBoltHit;
    }

    private void OnBoltHit(SpellCastLightning spell, SpellCastLightning.BoltHit hit)
    {
        if (spell.spellCaster.other.spellInstance is not SpellCastCrystallic crystallic || !crystallic.spellCaster.isFiring) return;
        currentHits++;

        if (currentHits >= requiredHitsUntilPylon)
        {
            requiredHitsUntilPylon = Random.Range(minMaxRequiredHits.x, minMaxRequiredHits.y);
            currentHits = 0;
            CreatePylon(spell, hit);
        }
    }

    public void CreatePylon(SpellCastLightning spellCastLightning, SpellCastLightning.BoltHit boltHit) => CreatePylon(spellCastLightning, boltHit.closestPoint, Quaternion.LookRotation(-boltHit.normal), boltHit.collider.transform);
    
    public void CreatePylon(SpellCastLightning spellCastLightning, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (parent.TryGetComponentInParent(out ThunderEntity _)) 
            return;

        Transform transform = PoolUtils.GetTransformPoolManager().Get();
        
        transform.SetPositionAndRotation(position, rotation);
        transform.position = position + -transform.forward * zapRadius;
        
        Gradient defaultGradient = skillThunderbolt.defaultBoltGradient;
        skillThunderbolt.defaultBoltGradient = boltGradient;
        skillThunderbolt.FireBoltAt(transform, position);
        skillThunderbolt.defaultBoltGradient = defaultGradient;
        
        GameManager.local.RunAfter(() => PoolUtils.GetTransformPoolManager().Release(transform), 3f);

        EffectInstance pylonEffect = pylonEffectData.Spawn(position, rotation, parent);
        pylonEffect.SetSize(0.5f);
        pylonEffect.Play();

        CapsuleCollider capsuleCollider = pylonEffect.GetRootParticleSystem().GetComponentInChildren<CapsuleCollider>();
        GameManager.local.StartCoroutine(PylonShockCoroutine(spellCastLightning, pylonEffect, capsuleCollider, 5f));
    }

    public IEnumerator PylonShockCoroutine(SpellCastLightning spellCastLightning, EffectInstance effectInstance, CapsuleCollider capsuleCollider, float lifetime)
    {
        float elapsed = 0f;
        float lastZapTime = 0f;
        float nextZapDelay = 1f;
        while (elapsed < lifetime)
        {
            foreach (var thunderEntity in ThunderEntity.InRadiusClosestPoint(capsuleCollider.transform.position, zapRadius))
            {
                if (Time.time - lastZapTime > nextZapDelay)
                {
                    lastZapTime = Time.time;
                    nextZapDelay = Random.Range(0.1f, 0.25f);
                    switch (thunderEntity.entity)
                    {
                        case Item item:
                            item.AddForce((item.transform.position - capsuleCollider.transform.position).normalized * 15f, ForceMode.Impulse);
                            foreach (ColliderGroup colliderGroup in item.colliderGroups)
                            {
                                if (colliderGroup.modifier.imbueType == ColliderGroupData.ImbueType.None) continue;
                                colliderGroup.imbue.Transfer(spellCastLightning, 5f);
                            }
                            break;
                    
                        case Creature creature when !creature.isPlayer:
                            creature.TryPush(Creature.PushType.Magic, (creature.ragdoll.targetPart.transform.position - capsuleCollider.transform.position).normalized, 1);
                            if (Random.Range(0f, 1f) > 0.5f) creature.Inflict("Electrocute", this, 5f);
                            break;
                    }
                    spellCastLightning.PlayBolt(Utils.RandomPointOnCapsule(capsuleCollider), thunderEntity.closestPoint);
                    spellCastLightning.boltHitEffectData.Spawn(thunderEntity.closestPoint, Quaternion.identity).Play();
                }
            }
            
            elapsed += Time.deltaTime;
            yield return Yielders.EndOfFrame;
        }
        effectInstance.End();
    }
}