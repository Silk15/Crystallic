using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public static class ReflectiveParticles 
{
    private static readonly Dictionary<ParticleSystem, (Gradient original, List<InjectedKey> keys)> injectedMap = new();

    public static void Inject(EffectInstance effectInstance, string id, Color color)
    {
        var particleSystems = effectInstance.GetParticleSystems();
        for (int i = 0; i < particleSystems.Length; i++) Inject(particleSystems[i], id, color);
    }

    public static void Inject(ParticleSystem particleSystem, string id, Color color)
    {
        var colLifetime = particleSystem.colorOverLifetime;
        if (!colLifetime.enabled) return;

        if (!injectedMap.ContainsKey(particleSystem))
        {
            var grad = new Gradient();
            grad.SetKeys(colLifetime.color.gradient.colorKeys, colLifetime.color.gradient.alphaKeys);
            injectedMap[particleSystem] = (grad, new List<InjectedKey>());
        }

        var entry = injectedMap[particleSystem];
        entry.keys.Add(new InjectedKey(id, color, 0f));
   
        var originalTimes = new HashSet<float>();
        foreach (var key in entry.original.colorKeys) originalTimes.Add(key.time);

        for (int i = 0; i < entry.keys.Count; i++)
        {
            float desiredTime = 1f / (entry.keys.Count + 1) * (i + 1);
            foreach (float t in originalTimes)
                if (Mathf.Abs(desiredTime - t) < 0.05f)
                {
                    desiredTime = t + 0.05f;
                    break;
                }

            entry.keys[i].time = Mathf.Clamp01(desiredTime);
        }

        var newColorKeys = new List<GradientColorKey>(entry.original.colorKeys);
        foreach (var k in entry.keys) newColorKeys.Add(new GradientColorKey(k.color, k.time));

        newColorKeys.Sort((a, b) => a.time.CompareTo(b.time));

        var newGradient = new Gradient();
        newGradient.SetKeys(newColorKeys.ToArray(), entry.original.alphaKeys);

        var gradColor = colLifetime.color;
        gradColor.gradient = newGradient;
        colLifetime.color = gradColor;

        injectedMap[particleSystem] = (entry.original, entry.keys);
    }

    public static void RemoveAll(EffectInstance effectInstance)
    {
        var particleSystems = effectInstance.GetParticleSystems();
        for (int i = 0; i < particleSystems.Length; i++)
            foreach (KeyValuePair<ParticleSystem, (Gradient original, List<InjectedKey> keys)> kvp in injectedMap)
            foreach (var key in kvp.Value.keys)
                if (kvp.Key == particleSystems[i])
                    Remove(particleSystems[i], key.id);
    }

    public static void Remove(EffectInstance effectInstance, string id)
    {
        var particleSystems = effectInstance.GetParticleSystems();
        for (int i = 0; i < particleSystems.Length; i++) Remove(particleSystems[i], id);
    }

    public static void Remove(ParticleSystem particleSystem, string id)
    {
        if (!injectedMap.TryGetValue(particleSystem, out var entry)) return;

        entry.keys.RemoveAll(k => k.id == id);

        if (entry.keys.Count == 0)
        {
            Reset(particleSystem);
            return;
        }

        var originalTimes = new HashSet<float>();
        foreach (var key in entry.original.colorKeys) originalTimes.Add(key.time);

        for (int i = 0; i < entry.keys.Count; i++)
        {
            float desiredTime = 1f / (entry.keys.Count + 1) * (i + 1);
            foreach (float t in originalTimes)
                if (Mathf.Abs(desiredTime - t) < 0.05f)
                {
                    desiredTime = t + 0.05f;
                    break;
                }

            entry.keys[i].time = Mathf.Clamp01(desiredTime);
        }

        var newColorKeys = new List<GradientColorKey>(entry.original.colorKeys);
        foreach (var k in entry.keys) newColorKeys.Add(new GradientColorKey(k.color, k.time));

        newColorKeys.Sort((a, b) => a.time.CompareTo(b.time));

        var colLifetime = particleSystem.colorOverLifetime;
        var gradColor = colLifetime.color;
        gradColor.gradient = new Gradient();
        gradColor.gradient.SetKeys(newColorKeys.ToArray(), entry.original.alphaKeys);
        colLifetime.color = gradColor;

        injectedMap[particleSystem] = (entry.original, entry.keys);
    }

    public static void Reset(EffectInstance effectInstance)
    {
        var particleSystems = effectInstance.GetParticleSystems();
        for (int i = 0; i < particleSystems.Length; i++) if (injectedMap.ContainsKey(particleSystems[i])) Reset(particleSystems[i]);
    }

    public static void Reset(ParticleSystem particleSystem)
    {
        if (!injectedMap.ContainsKey(particleSystem)) return;

        var colLifetime = particleSystem.colorOverLifetime;
        if (!colLifetime.enabled) return;

        var (original, _) = injectedMap[particleSystem];
        var col = colLifetime.color;
        col.gradient = original;
        colLifetime.color = col;

        injectedMap.Remove(particleSystem);
    }
    
    [Serializable]
    public class InjectedKey
    {
        public string id;
        public Color color;
        public float time;

        public InjectedKey(string id, Color color, float time)
        {
            this.id = id;
            this.color = color;
            this.time = time;
        }
    }
}
