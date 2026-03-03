using System;
using ThunderRoad;
using TriInspector;

namespace Crystallic.Skill.Spell
{
    [Serializable]
    public class SkillSpellDash : SkillSpellPair
    {
        [Group("Tabs"), Tab("Dash")]
        public float dashSpeed = 10f;
        
        [Group("Tabs"), Tab("Dash")]
        public float dashImbueDrain = 10f;
        
        [Group("Tabs"), Tab("Dash")]
        public float hapticIntensity = 1.0f;

        #if !SDK
        public EffectInstance effectInstance;
        #endif
    }
}