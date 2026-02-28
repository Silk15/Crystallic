#if !SDK
using System.Collections;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Crystallic;

public class Lerper
{
    public ColorType currentColorType = ColorType.Solid;
    public Color targetColor = Color.white;
    public Color currentColor = Color.white;
    public string currentSpellId = "Crystallic";
    public Coroutine rainbowInstance;
    private Coroutine instance;

    public void SetColor(Color target, ParticleSystem[] particleSystems, string spellId, float time = 1)
    {
        if (Dye.rainbowMode) 
            return;
        
        if (instance != null) 
            GameManager.local.StopCoroutine(instance);
        
        targetColor = target;
        currentColorType = Dye.GetColorType(currentSpellId, spellId);
        instance = GameManager.local.StartCoroutine(SetColorCoroutine(particleSystems, target, spellId, time));
    }

    public IEnumerator SetColorCoroutine(ParticleSystem[] particles, Color target, string spellId, float tts = 1f)
    {
        yield return null;
        
        var timeElapsed = 0f;
        for (var i = 0; i < particles.Length; i++)
        {
            var lt = particles[i].colorOverLifetime;
            lt.enabled = true;
        }

        yield return null;

        var originalColors = new Color[particles.Length];
        for (var i = 0; i < particles.Length; i++)
        {
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

        currentSpellId = spellId;
        instance = null;
    }

    public void StartRainbow(ParticleSystem[] particles, float speed = 0.5f)
    {
        StopRainbow();
        rainbowInstance = GameManager.local.StartCoroutine(RainbowCoroutine(particles, speed));
    }

    public void StopRainbow()
    {
        if (rainbowInstance != null)
        {
            GameManager.local.StopCoroutine(rainbowInstance);
            rainbowInstance = null;
        }
    }

    private IEnumerator RainbowCoroutine(ParticleSystem[] particles, float speed)
    {
        float[] offsets = new float[particles.Length];

        for (int i = 0; i < particles.Length; i++)
            offsets[i] = Random.value;

        while (true)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                float hue = (offsets[i] + Time.time * speed) % 1f;

                Gradient gradient = new Gradient();
                gradient.SetKeys(new GradientColorKey[]
                {
                    new(Color.HSVToRGB(hue, 1, 1), 0f),
                    new(Color.HSVToRGB((hue + 0.1f) % 1f, 1, 1), 1f),
                }, new GradientAlphaKey[]
                {
                    new(1f, 0f),
                    new(1f, 1f),
                });

                var colorOverLifetime = particles[i].colorOverLifetime;
                colorOverLifetime.enabled = true;
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            }

            yield return null;
        }
    }
}
#endif