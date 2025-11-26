using System.Collections;
using Crystallic;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace Crystallic;

public class Crystallised : Status
{
    public BrainModuleCrystal brainModuleCrystal;
    
    public override void FirstApply()
    {
        base.Apply();
        if (entity is Creature creature)
        {
            brainModuleCrystal = creature.brain.instance.GetModule<BrainModuleCrystal>();
            brainModuleCrystal.StartCrystallise();
        }
    }

    public override void Apply()
    {
        base.Apply();
        if (value is CrystallisedParams crystallisedParams) brainModuleCrystal.SetColor(crystallisedParams.targetColor, crystallisedParams.spellId, crystallisedParams.time);
    }
    
    public override bool ReapplyOnValueChange => true;

    protected override object GetValue()
    {
        CrystallisedParams identity = CrystallisedParams.Identity;
        float latestTime = float.MinValue;

        foreach ((float _, object parameter) in handlers.Values)
        {
            if (parameter is CrystallisedParams crystallisedParams && crystallisedParams.appliedTime > latestTime)
            {
                latestTime = crystallisedParams.appliedTime;
                identity = crystallisedParams;
            }
        }

        return identity;
    }
    
    public override void FullRemove()
    {
        base.Remove();
        if (entity is Creature) brainModuleCrystal.StopCrystallise();
    }
}