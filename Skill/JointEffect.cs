using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill;


[Serializable]
public class JointEffect
{
    #if !SDK
    public List<EffectInstance> effectInstances;
    public ConfigurableJoint configurableJoint;

    public JointEffect(ConfigurableJoint configurableJoint)
    {
        effectInstances = new List<EffectInstance>();
        this.configurableJoint = configurableJoint;
    }
    #endif
}