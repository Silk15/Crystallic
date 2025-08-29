using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class Lerper
{
    public ColorType currentColorType = ColorType.Solid;
    public Color currentColor = Color.white;
    public string currentSpellId = "Crystallic";
    private Coroutine instance;

    public void SetColor(Color target, ParticleSystem[] particleSystems, string spellId, float time = 1)
    {
        currentColorType = Dye.GetColorType(currentSpellId, spellId);
        if (instance != null) GameManager.local.StopCoroutine(instance);
        instance = GameManager.local.StartCoroutine(SetColorRoutine(particleSystems, target, spellId, time));
    }

    public IEnumerator SetColorRoutine(ParticleSystem[] particles, Color target, string spellId, float tts = 1f)
    {
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
            yield return Yielders.EndOfFrame;
        }

        for (var i = 0; i < particles.Length; i++)
        {
            var colorOverLifetime = particles[i].colorOverLifetime;
            colorOverLifetime.color = target;
        }
        currentSpellId = spellId;
        instance = null;
    }
}