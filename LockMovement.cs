using ThunderRoad;

namespace Crystallic;

public class LockMovement : Status
{
    public override void Apply()
    {
        base.Apply();
        if (!(this.entity is Creature entity)) return;
        if (entity.isPlayer)
        {
            entity.currentLocomotion.globalMoveSpeedMultiplier.Add(this, 0);
            entity.mana.chargeSpeedMult.Add(this, 0.1f);
        }
    }

    public override void Remove()
    {
        base.Remove();
        if (!(this.entity is Creature entity)) return;
        if (entity.isPlayer)
        {
            entity.currentLocomotion.globalMoveSpeedMultiplier.Remove(this);
            entity.mana.chargeSpeedMult.Remove(this);
        }
    }
}