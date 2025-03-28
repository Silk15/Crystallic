using Crystallic.Skill;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class GolemLithoweb : GolemAbility
{
    public EffectData tetherEffectData;
    public EffectData snapEffectData;
    public string snapEffectId = "GravitySnap";
    public string tetherEffectId = "GravityTether";
    public GolemController.AttackMotion attackMotion = GolemController.AttackMotion.SprayDance;
    public EffectInstance tetherEffectInstance;
    public ConfigurableJoint joint;
    public override void Begin(GolemController golem)
    {
        base.Begin(golem);
        tetherEffectData = Catalog.GetData<EffectData>(tetherEffectId);
        snapEffectData = Catalog.GetData<EffectData>(snapEffectId);
        golem.PerformAttackMotion(attackMotion);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if ((golem.magicSprayPoints[0].transform.position - Player.currentCreature.ragdoll.targetPart.transform.position).sqrMagnitude > 2.5f * 2.5f && tetherEffectInstance == null)
        {
            tetherEffectInstance = tetherEffectData.Spawn(golem.magicSprayPoints[0]);
            tetherEffectInstance.SetSourceAndTarget(golem.magicSprayPoints[0], Player.currentCreature.ragdoll.targetPart.transform);
            tetherEffectInstance.Play();
            var mainBody = new GameObject("MainBody").AddComponent<Rigidbody>();
            mainBody.isKinematic = true;
            mainBody.useGravity = false;
            mainBody.transform.parent = golem.magicSprayPoints[0].transform;
            mainBody.transform.position = golem.magicSprayPoints[0].transform.position;
            mainBody.transform.rotation = golem.magicSprayPoints[0].transform.rotation;
            Player.currentCreature.Inflict("Floating", this);
            joint = Utils.CreateConfigurableJoint(mainBody, Player.local.GetPhysicBody().rigidBody, 5000, 35, 2.5f, 5, 0.1f, true, ConfigurableJointMotion.Limited);
            Player.currentCreature.RunAfter(() =>
            {
                joint.connectedBody = null;
                tetherEffectInstance.End();
                Player.currentCreature.Remove("Floating", this);
                GameObject.Destroy(joint);
            }, 5);
        }
    }
}