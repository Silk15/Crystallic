using ThunderRoad;

namespace Crystallic.Skill;

public class SkillStatusPair : SkillSpellPair
{
    public string statusId;
    public float statusParameter;
    public float statusDuration;
    public bool playEffects;

    public virtual void Inflict(Creature creature) => creature.Inflict(statusId, this, statusDuration, statusParameter, playEffects);
}