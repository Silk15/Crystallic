using UnityEngine;

namespace Crystallic;

public struct CrystallisedParams
{
    public Color targetColor;
    public string spellId;
    public float time;
    public float appliedTime;

    public CrystallisedParams(Color targetColor, string spellId, float time = 1f)
    {
        this.targetColor = targetColor;
        this.spellId = spellId;
        this.time = time; 
        appliedTime = Time.time;
    }

    public static CrystallisedParams Identity => new(Color.white, "Crystallic");
}
