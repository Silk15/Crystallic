using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class Lerper
{
    /// <summary>
    /// This is used to know whether the colour is a mix, or a solid dye. This is determined at runtime with the Dye class.
    /// </summary>
    public ColorType currentColorType = ColorType.Solid;
    public Color currentColor = Color.white;
    public bool isLerping;
    public string currentSpellId = "Crystallic";

    public void SetColor(Color target, ParticleSystem[] particleSystems, string spellId, float time = 1)
    {
        if (isLerping) return;
        currentColorType = Dye.GetColorType(currentSpellId, spellId);
        GameManager.local.StartCoroutine(SetColorRoutine(particleSystems, target, spellId, time));
    }

    public IEnumerator SetColorRoutine(ParticleSystem[] particles, Color target, string spellId, float tts = 1f)
    {
        isLerping = true;
        var timeElapsed = 0f;
        var originalColors = new Color[particles.Length];
        for (var i = 0; i < particles.Length; i++)
        {
            var lt = particles[i].colorOverLifetime;
            lt.enabled = true;
            var colorOverLifetime = particles[i].colorOverLifetime;
            originalColors[i] = colorOverLifetime.color.color;
        }

        while (timeElapsed < tts)
        {
            var lerpFactor = timeElapsed / tts;
            for (var i = 0; i < particles.Length; i++)
            {
                var colorOverLifetime = particles[i].colorOverLifetime;
                var currentColor = originalColors[i];
                this.currentColor = Color.Lerp(currentColor, target, lerpFactor);
                colorOverLifetime.color = Color.Lerp(currentColor, target, lerpFactor);
            }

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        for (var i = 0; i < particles.Length; i++)
        {
            var colorOverLifetime = particles[i].colorOverLifetime;
            colorOverLifetime.color = target;
        }

        isLerping = false;
        this.currentSpellId = spellId;
    }
}