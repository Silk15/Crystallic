using Crystallic.Skill;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill;

public class ImbueFireBehaviour : ImbueBehaviour
{
    public EffectData detonateEffectData;
    public string detonateEffectId = "RemoteDetonation";
    public AnimationCurve flameCurve = new(new Keyframe(0.0f, 0.5f), new Keyframe(0.05f, 30), new Keyframe(0.1f, 0.5f));

    public override void Load(SkillCrystalImbueHandler handler, Imbue imbue)
    {
        base.Load(handler, imbue);
        detonateEffectData = Catalog.GetData<EffectData>(detonateEffectId);
    }

    public override void Hit(CollisionInstance collisionInstance, SpellCastCharge spellCastCharge)
    {
        base.Hit(collisionInstance, spellCastCharge);
        if (collisionInstance.targetColliderGroup.collisionHandler.Entity is not Creature hitCreature) return;
        int flag;
        var item = collisionInstance?.sourceColliderGroup?.collisionHandler?.Entity as Item;
        hitCreature.SetVariable("HasDetonated", hitCreature.GetVariable<int>("HasDetonated") + 1);
        if (item && hitCreature && hitCreature != imbue.imbueCreature && hitCreature.TryGetVariable("HasDetonated", out flag) && flag == 2)
        {
            detonateEffectData?.Spawn(hitCreature.ragdoll.targetPart.transform).Play();
            hitCreature.Inflict("Burning", this, parameter: 100);
            if (!hitCreature.isPlayer) hitCreature?.AddExplosionForce(70, collisionInstance.contactPoint, 5, 0.1f, ForceMode.Impulse);
            item.PlayHapticClip(flameCurve, 0.25f);
            item?.AddForce((item.transform.position - hitCreature.transform.position).normalized * 2, ForceMode.Impulse);
        }
    }
}