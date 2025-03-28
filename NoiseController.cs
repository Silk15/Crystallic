using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class NoiseController : MonoBehaviour
{
    private AnimationCurve _curve;
    private EffectInstance _effectInstance;
    private bool _isRunning;
    private float _lastUpdateTime;
    private NoiseMode _mode;
    private Rigidbody _rigidbody;
    private float _updateInterval;

    private void Update()
    {
        if (!_isRunning) return;

        if (Time.time - _lastUpdateTime >= _updateInterval)
        {
            _lastUpdateTime = Time.time;
            ApplyNoiseBasedOnVelocity();
        }
    }

    public void Initialize(Rigidbody rigidbody, EffectInstance effectInstance, AnimationCurve curve, NoiseMode mode, float updateInterval)
    {
        _rigidbody = rigidbody;
        _effectInstance = effectInstance;
        _curve = curve;
        _mode = mode;
        _updateInterval = updateInterval;
        _lastUpdateTime = Time.time;
        _isRunning = true;
    }

    private void ApplyNoiseBasedOnVelocity()
    {
        if (_rigidbody == null || _effectInstance == null) return;
        var velocityMagnitude = _rigidbody.velocity.magnitude;
        var noiseValue = _curve.Evaluate(velocityMagnitude);
        foreach (var particleSystem in _effectInstance.GetParticleSystems())
        {
            var noise = particleSystem.noise;
            if (!noise.enabled) continue;

            switch (_mode)
            {
                case NoiseMode.Strength:
                    noise.strength = noiseValue;
                    break;
                case NoiseMode.StrengthAndFrequency:
                    noise.strength = noiseValue;
                    noise.frequency = noiseValue;
                    break;
                case NoiseMode.Frequency:
                    noise.frequency = noiseValue;
                    break;
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
    }
}

public enum NoiseMode
{
    Strength,
    Frequency,
    StrengthAndFrequency
}