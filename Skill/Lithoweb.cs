using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill;

public class Lithoweb : ThunderBehaviour
{
    [ModOption("Lithoweb Spring", "The spring applied to the joint connecting both limbs, this is the value that decides how tightly two limbs are bound, from loosely floaty to tight."), ModOptionCategory("Lithoweb", 20), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
    public static float spring = 200f;
    
    [ModOption("Lithoweb Damper", "The damping applied to the joint connecting both limbs, this acts as a smoother, damping out movement to act floaty."), ModOptionCategory("Lithoweb", 20), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
    public static float damper = 30f;
    
    [ModOption("Min Lithoweb Distance", "The min distance two limbs can be from one another."), ModOptionCategory("Lithoweb", 20), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float minDistance = 1f;
    
    [ModOption("Max Lithoweb Distance", "The max distance two limbs can be from one another."), ModOptionCategory("Lithoweb", 20), ModOptionSlider, ModOptionFloatValues(0.1f, 100, 0.1f)]
    public static float maxDistance = 1.5f;
    
    [ModOption("Lithoweb Snap Spring", "The spring applied to the joint after the lifetime expires, limbs will be slammed together."), ModOptionCategory("Lithoweb", 20), ModOptionSlider, ModOptionFloatValues(1, 10000, 0.5f)]
    public static float snapSpring = 2000f;
    
    [ModOption("Lithoweb Float Drag", "The drag creatures experience while tied with a lithoweb."), ModOptionCategory("Lithoweb", 20), ModOptionSlider, ModOptionFloatValues(1, 10, 0.5f)]
    public static float floatDrag = 2f;

    [ModOption("Lithoweb Lifetime", "Controls the lifetime of lithowebs"), ModOptionCategory("Lithoweb", 20), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float lifetime = 2.5f;
        
    [ModOption("Lithoweb Slam Force Multiplier", "Controls how hard limbs will be thrown into the ground upon expiring."), ModOptionCategory("Lithoweb", 20), ModOptionSlider, ModOptionFloatValues(1, 100, 0.5f)]
    public static float slamForceMult = 50f;
    private Creature creature;
    public ConfigurableJoint joint;
    public StatusData statusData;
    public EffectData tetherEffectData;
    public EffectData slamEffectData;
    public EffectInstance tetherEffectInstance;
    public string tetherEffectId = "GravityTether";
    public Item slicer;
    public RagdollPart source;
    public RagdollPart target;
    public EffectData snapEffectData;
    public string snapEffectId = "GravitySnap";
    
    public void Init(Item slicer, RagdollPart source, RagdollPart target, bool overrideJointDefaults = false, float overrideSpring = 0f, float overrideDamper = 0f, float overrideMinDistance = 0f, float overrideMaxDistance = 0f, float overrideMassScale = 0f, float overrideLifetime = 0f)
    {
        creature = source.ragdoll.creature;
        creature.OnDespawnEvent += OnDespawnEvent;
        statusData = Catalog.GetData<StatusData>("Floating");
        tetherEffectData = Catalog.GetData<EffectData>(tetherEffectId);
        slamEffectData = Catalog.GetData<EffectData>("SpellGravityPush");
        snapEffectData = Catalog.GetData<EffectData>(snapEffectId);
        this.slicer = slicer;
        this.source = source;
        this.target = target;
        creature.Inflict(statusData, this, parameter: new FloatingParams(drag: floatDrag, noSlamAtEnd: true));
        if (tetherEffectInstance != null) tetherEffectInstance.End();
        tetherEffectInstance = tetherEffectData.Spawn(source.physicBody.rigidBody.gameObject.transform);
        tetherEffectInstance.SetSource(source.physicBody.rigidBody.gameObject.transform);
        tetherEffectInstance.SetTarget(target.physicBody.rigidBody.gameObject.transform);
        tetherEffectInstance.Play();
        joint = !overrideJointDefaults ? Utils.CreateConfigurableJoint(source?.physicBody.rigidBody, target?.physicBody.rigidBody, spring, damper, minDistance, maxDistance, 0.1f) : Utils.CreateConfigurableJoint(source?.physicBody.rigidBody, target?.physicBody.rigidBody, overrideSpring, overrideDamper, overrideMinDistance, overrideMaxDistance, overrideMassScale);
        if (source != null && target != null) StartCoroutine(AutoExpireRoutine(!overrideJointDefaults ? lifetime : overrideLifetime));
    }

    private void OnDespawnEvent(EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart)
        {
            creature.OnDespawnEvent -= OnDespawnEvent;
            Deactivate();
        }
    }

    public IEnumerator AutoExpireRoutine(float autoExpireTime)
    {
        yield return Yielders.ForSeconds(autoExpireTime);
        creature.Remove(statusData, this);
        yield return Yielders.ForSeconds(1.75f);
        source.physicBody.AddForce(Vector3.down * slamForceMult, ForceMode.Impulse);
        slamEffectData.Spawn(source.physicBody.rigidBody.gameObject.transform.position + new Vector3(0, 1, 0), Quaternion.LookRotation(Vector3.down)).Play();
        snapEffectData.Spawn(joint.transform).Play();
        Deactivate();
    }


    public void Deactivate()
    { 
        tetherEffectInstance?.End();
        joint.connectedBody = null;
        Destroy(joint);
        creature.Remove(statusData, this);
        Destroy(this);
    }
}