#if !SDK
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Crystallic
{
    public class Lerper
    {
        public ColorType currentColorType = ColorType.Solid;
        public Color targetColor = Color.white;
        public Color currentColor = Color.white;
        public string currentSpellId = "Crystallic";
        private Coroutine instance;

        public void SetColor(Color targetColor, ParticleSystem[] particleSystems, string spellId, float time = 1)
        {
            if (Dye.rainbowMode)
                return;

            if (instance != null)
                GameManager.local.StopCoroutine(instance);

            this.targetColor = targetColor;
            currentColorType = Dye.GetColorType(currentSpellId, spellId);
            instance = GameManager.local.StartCoroutine(SetColorCoroutine(particleSystems, targetColor, spellId, time));
        }

        public IEnumerator SetColorCoroutine(ParticleSystem[] particles, Color targetColor, string spellId, float tts = 1f)
        {
            yield return Yielders.EndOfFrame;
            Color[] originalColors = new Color[particles.Length];
            float timeElapsed = 0f;
            
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.ColorOverLifetimeModule colorOverLifetimeModule = particles[i].colorOverLifetime;
                colorOverLifetimeModule.enabled = true;
            }

            yield return Yielders.EndOfFrame;

            for (int i = 0; i < particles.Length; i++)
                originalColors[i] = particles[i].colorOverLifetime.color.color;

            while (timeElapsed < tts)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    ParticleSystem.ColorOverLifetimeModule colorOverLifetimeModule = particles[i].colorOverLifetime;
                    Color currentColor = originalColors[i];
                    this.currentColor = Color.Lerp(currentColor, targetColor, Mathf.Clamp01(timeElapsed / tts));
                    colorOverLifetimeModule.color = this.currentColor;
                }

                timeElapsed += Time.deltaTime;
                yield return Yielders.EndOfFrame;
            }

            for (var i = 0; i < particles.Length; i++)
            {
                ParticleSystem.ColorOverLifetimeModule colorOverLifetimeModule = particles[i].colorOverLifetime;
                colorOverLifetimeModule.color = targetColor;
            }

            currentSpellId = spellId;
            instance = null;
        }
    }
}
#endif