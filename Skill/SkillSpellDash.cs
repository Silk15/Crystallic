using System;
using ThunderRoad;

namespace Crystallic.Skill;

[Serializable]
public class SkillSpellDash : SkillSpellPair
{
    public float dashSpeed;
    public string effectId;
    public string statusId;
    public float statusInflictDelay;
    public float statusDuration;
    public float statusParam;
    public EffectData dashEffectData;
}