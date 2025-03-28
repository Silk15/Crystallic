using System.Collections;
using System.Collections.Generic;
using Crystallic.AI;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class CrystalCluster : ThunderBehaviour
{
    public bool ignorePlayer = true;
    public SpellCastCharge spellCastCharge;
    public Coroutine despawnRoutine;
    protected TriggerDetector detector;
    protected EffectInstance dropEffect;
    protected HashSet<ThunderEntity> entitiesThisFrame;
    protected bool isPlayingEffect;
    public Lerper lerper;
    protected Rigidbody rb;
    protected float startTime;
    protected CapsuleCollider trigger;
    protected EffectInstance wallEffect;
    public WaterHandler waterHandler;
    public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

    private void Awake()
    {
        lerper = new Lerper();
        entitiesThisFrame = new HashSet<ThunderEntity>();
        spellCastCharge = Catalog.GetData<SpellCastCharge>("Crystallic");
        waterHandler = new WaterHandler(false, false);
        waterHandler.OnWaterEnter += OnWaterEnter;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (isPlayingEffect) return;
        var rigidbody = other.rigidbody;
        if (rigidbody != null && !rigidbody.isKinematic) return;
        dropEffect?.End();
        wallEffect?.Play();
        trigger.enabled = true;
        isPlayingEffect = true;
    }

    public void OnTriggerStay(Collider other)
    {
        var componentInParent = other.attachedRigidbody?.GetComponentInParent<ThunderEntity>();
        if (componentInParent == null || !entitiesThisFrame.Add(componentInParent) || (componentInParent is Creature creature1 && creature1.isPlayer && ignorePlayer)) return;
        var creature2 = componentInParent as Creature;
        if (creature2 == null) return;
    }

    public static CrystalCluster Create(Vector3 position, Quaternion rotation = default)
    {
        return new GameObject(nameof(CrystalCluster)) { transform = { position = position, rotation = rotation } }.AddComponent<CrystalCluster>();
    }

    private void OnWaterEnter()
    {
        Despawn();
    }

    public void SetColor(Color color, string spellId)
    {
        var systems = wallEffect.GetParticleSystems();
        lerper.SetColor(color, systems, spellId, 0.01f);
    }


    public void Init(string spellId, EffectData dropEffectData, EffectData wallEffectData, float thickness, float height, float heatRadius, float downwardForce, float duration, bool allowXZMotion = false, bool drop = true)
    {
        if (drop)
        {
            dropEffect = dropEffectData?.Spawn(transform, null, true, null, false, 0.0f);
            dropEffect?.Play();
        }

        if (wallEffectData != null) wallEffect = wallEffectData?.Spawn(transform, null, false, null, false, 0.0f);
        SetColor(Dye.GetEvaluatedColor(lerper.currentSpellId, spellId), spellId);
        foreach (var inRadius in ThunderEntity.InRadius(transform.position, thickness / 0.5f))
            if (inRadius is Creature creature && creature && !creature.isPlayer)
            {
                creature.TryPush(Creature.PushType.Magic, (creature.transform.position - transform.position).normalized * 2f, 1);
                creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                creature.AddExplosionForce(50, transform.position, thickness, 0.2f, ForceMode.Impulse);
                var module = creature.brain.instance.GetModule<BrainModuleCrystal>();
                module.Crystallise(5f);
                module.SetColor(Dye.GetEvaluatedColor(module.lerper.currentSpellId, lerper.currentSpellId), lerper.currentSpellId);
            }

        startTime = Time.time;
        if (!drop && wallEffect != null) wallEffect?.Play();
        rb = gameObject.AddComponent<Rigidbody>();
        if (!drop) rb.isKinematic = true;
        Despawn(duration);
    }


    protected override void ManagedUpdate()
    {
        base.ManagedUpdate();
        wallEffect?.SetIntensity(Mathf.InverseLerp(0.0f, 1f, Time.time - startTime));
        entitiesThisFrame.Clear();
        waterHandler.Update(transform.position, transform.position.y, transform.position.y + 0.1f, 0.1f);
    }

    public void Despawn(float duration = 0.0f)
    {
        if (despawnRoutine != null) StopCoroutine(despawnRoutine);
        despawnRoutine = StartCoroutine(DespawnRoutine(duration));
        foreach (var inRadius in ThunderEntity.InRadius(transform.position, 2f))
            if (inRadius && inRadius is Creature creature && creature && !creature.isPlayer)
                creature.TryPush(Creature.PushType.Magic, (creature.transform.position - transform.position).normalized * 2f, 1);
    }

    public IEnumerator DespawnRoutine(float duration)
    {
        var flameWall = this;
        yield return new WaitForSeconds(duration);
        flameWall.wallEffect?.SetParent(null);
        flameWall.wallEffect?.End();
        flameWall.wallEffect?.End();
        Destroy(this);
        yield return 0;
    }
}