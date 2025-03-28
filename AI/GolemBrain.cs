using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crystallic.Modules;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Serialization;

namespace Crystallic.AI;

public class GolemBrain : ThunderBehaviour
{
    private static GolemBrain _instance;
    
    /// <summary>
    /// A singleton instance of the GolemBrain class.
    /// </summary>
    public static GolemBrain Instance
    {
        get
        {
            if (_instance == null)
            {
                if (Golem.local != null) _instance = Golem.local.gameObject.AddComponent<GolemBrain>();
                else Debug.LogWarning("GolemBrain: Golem is null, cannot instantiate or access GolemBrain!");
            }
            return _instance;
        }
    }
    public delegate void BrainModuleEvent(GolemBrainModule golemBrainModule);
    public event BrainModuleEvent onGolemBrainModuleLoaded;
    public event BrainModuleEvent onGolemBrainModuleUnloaded;
    /// <summary>
    /// A list of active golemBrainModule types. These are unique, not exactly singletons but the brain isn't able to load more than one of a type unless forcefully added.
    /// </summary>
    public List<GolemBrainModule> modules = new();
    public List<string> moduleAddresses = new();
    public static bool effectsActive;
    /// <summary>
    /// A dictionary of each part on the golem, use GetPartByName or GetPart to actually access these.
    /// </summary>
    public Dictionary<Part, GolemPart> parts = new();
    public List<EffectInstance> activeEffects = new();
    
    private void OnDestroy()
    {
        for (int i = 0; i < modules.Count; i++)
        {
            modules[i].Unload(Golem.local);
            onGolemBrainModuleUnloaded?.Invoke(modules[i]);
        }
        modules.Clear();
    }

    /// <summary>
    /// Called once by the handler module to initialize all modules and the brain, this is then destroyed when he's defeated, which unloads every module. Please do not call this more than once.
    /// </summary>
    /// <param name="moduleAddresses"></param>
    public void Initialize(List<string> moduleAddresses)
    {
        this.moduleAddresses = moduleAddresses;
        Debug.Log($"Golem brain instance created with {this.moduleAddresses.Count} modules loaded:\n - " + string.Join("\n - ", this.moduleAddresses));
        Golem.local.defeatEvent.RemoveListener(OnDefeat);
        Golem.local.defeatEvent.AddListener(OnDefeat);
        var initializedTypes = new HashSet<Type>(modules.ConvertAll(m => m.GetType()));
        Golem.local.Brain().InitialiseParts();
        for (var i = 0; i < moduleAddresses.Count; i++)
        {
            var type = Type.GetType(moduleAddresses[i]);
            if (type == null) Debug.LogError($"Type not found for address: {moduleAddresses[i]}");
            else if (typeof(GolemBrainModule).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                try
                {
                    if (!initializedTypes.Contains(type))
                    {
                        var module = gameObject.AddComponent(type) as GolemBrainModule;
                        modules.Add(module);
                        initializedTypes.Add(type);
                        module?.Load(Golem.local);
                        onGolemBrainModuleLoaded?.Invoke(module);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load Golem Brain Module of type: {type}: {ex.Message}");
                }
        }
    }

    private void OnDefeat()
    {
        for (int i = 0; i < modules.Count; i++)
        {
            modules[i].Unload(Golem.local);
            onGolemBrainModuleUnloaded?.Invoke(modules[i]);
        }
        Destroy(this);
    }

    /// <summary>
    /// Used to add a module to the golem, they are unique, meaning you can't add more than one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public GolemBrainModule Add<T>()
    {
        bool flag = true;
        foreach (GolemBrainModule golemBrainModule in modules) 
            if (golemBrainModule.GetType() == typeof(T)) flag = false;
        if (flag)
        {
            var module = gameObject.AddComponent(typeof(T)) as GolemBrainModule;
            modules.Add(module);
            onGolemBrainModuleLoaded?.Invoke(module);
            module.Load(Golem.local);
            return module;
        }
        Debug.LogError($"Cannot add module of type: {typeof(T)}! Module already exists.");
        return null;
    }

    /// <summary>
    /// Used to remove brain modules safely.
    /// </summary>
    /// <param name="golemBrainModule"></param>
    public void Remove(GolemBrainModule golemBrainModule)
    {
        if (Golem.local == null) return;
        var module = modules.Find(m => m == golemBrainModule);
        if (module != null && modules.Contains(module))
        {
            modules.Remove(module);
            module.Unload(Golem.local);
            onGolemBrainModuleUnloaded?.Invoke(module);
            Destroy(module);
        }
    }

    /// <summary>
    /// Use this for accessing modules if you know it exists.
    /// </summary>
    /// <param name="addIfNotFound"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public GolemBrainModule GetModule<T>(bool addIfNotFound = true)
    { 
        GolemBrainModule module;
        module = modules.Find(m => m.GetType() == typeof(T));
        if (addIfNotFound && !module) module = Add<T>();
        return module;
    }

    /// <summary>
    /// If you don't know a module exists, this is best.
    /// </summary>
    /// <param name="brainModule"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGetModule<T>(out T brainModule) where T : GolemBrainModule
    {
        brainModule = modules.Find(module => module is T) as T;
        return brainModule != null;
    }

    /// <summary>
    /// Use this to access a part via bone name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public GolemPart GetPartByName(string name)
    {
        GolemPart part = parts.Values.First(p => p.name == name);
        return part;
    }
    
    /// <summary>
    /// Use this to access a part via the enum.
    /// </summary>
    /// <param name="part"></param>
    /// <param name="log"></param>
    /// <returns></returns>
    public GolemPart GetPart(Part part, bool log = false)
    {
        GolemPart part2 = null;
        if (Golem.local == null) return null;
        foreach (var parts in parts)
        {
            if (parts.Key == part) part2 = parts.Value;
            if (log) Debug.Log($"{parts.Key} == {part} | {parts.Key == part}");
        }

        return part2;
    }

    public void InitialiseParts()
    {
        var animator = Golem.local.animator;
        foreach (Part partType in Enum.GetValues(typeof(Part)))
        {
            var boneEnum = (HumanBodyBones)partType;
            var bone = animator.GetBoneTransform(boneEnum);
            if (bone == null) continue;
            GolemPart part = null;
            switch (partType)
            {
                case Part.Head:
                    part = bone.gameObject.AddComponent<HeadPart>();
                    break;
                default:
                    part = bone.gameObject.AddComponent<GolemPart>();
                    break;
            }

            part.part = partType;
            if (!parts.ContainsKey(partType)) parts.Add(partType, part);
        }

        if (parts.Count > 0) RefreshParts();
    }

    private IEnumerator EffectRoutine(float duration, EffectData upperArmLeftData, EffectData upperArmRightData, EffectData lowerArmLeftData, EffectData lowerArmRightData, EffectData torsoData, EffectData upperLegLeftData, EffectData upperLegRightData, EffectData lowerLegLeftData, EffectData lowerLegRightData, bool infinite)
    {
        effectsActive = true;
        var upperLeftArm = Golem.local.transform.GetChildByNameRecursive("LeftShoulder");
        var upperRightArm = GetPart(Part.RightUpperArm);
        var lowerLeftArm = GetPart(Part.LeftLowerArm);
        var lowerRightArm = GetPart(Part.RightLowerArm);
        var torso = GetPart(Part.Chest);
        var upperLeftLeg = GetPart(Part.LeftUpperLeg);
        var upperRightLeg = GetPart(Part.RightUpperLeg);
        var lowerLeftLeg = GetPart(Part.LeftLowerLeg);
        var lowerRightLeg = GetPart(Part.RightLowerLeg);
        if (upperArmLeftData != null)
        {
            var upperArmLeftGolemEffect = upperArmLeftData?.Spawn(upperLeftArm.transform);
            activeEffects.Add(upperArmLeftGolemEffect);
        }

        if (upperArmRightData != null)
        {
            var upperArmRightGolemEffect = upperArmRightData?.Spawn(upperRightArm.transform);
            activeEffects.Add(upperArmRightGolemEffect);
        }

        if (lowerArmLeftData != null)
        {
            var lowerArmLeftGolemEffect = lowerArmLeftData?.Spawn(lowerLeftArm.transform);
            activeEffects.Add(lowerArmLeftGolemEffect);
        }

        if (lowerArmRightData != null)
        {
            var lowerArmRightGolemEffect = lowerArmRightData?.Spawn(lowerRightArm.transform);
            activeEffects.Add(lowerArmRightGolemEffect);
        }

        if (torsoData != null)
        {
            var torsoGolemEffect = torsoData?.Spawn(torso.transform);
            activeEffects.Add(torsoGolemEffect);
        }

        if (upperLegLeftData != null)
        {
            var upperLegLeftGolemEffect = upperLegLeftData?.Spawn(upperLeftLeg.transform);
            activeEffects.Add(upperLegLeftGolemEffect);
        }

        if (upperLegRightData != null)
        {
            var upperLegRightGolemEffect = upperLegRightData?.Spawn(upperRightLeg.transform);
            activeEffects.Add(upperLegRightGolemEffect);
        }

        if (lowerLegLeftData != null)
        {
            var lowerLegLeftGolemEffect = lowerLegLeftData?.Spawn(lowerLeftLeg.transform);
            activeEffects.Add(lowerLegLeftGolemEffect);
        }

        if (lowerLegRightData != null)
        {
            var lowerLegRightGolemEffect = lowerLegRightData?.Spawn(lowerRightLeg.transform);
            activeEffects.Add(lowerLegRightGolemEffect);
        }

        if (activeEffects.Count > 0)
            for (var i = 0; i < activeEffects?.Count; i++)
            {
                var effectInstance = activeEffects[i];
                effectInstance.Play();
            }

        if (!infinite)
        {
            yield return new WaitForSeconds(duration);
            if (activeEffects.Count > 0)
                for (var i = 0; i < activeEffects?.Count; i++)
                {
                    var effectInstance = activeEffects[i];
                    effectInstance.End();
                    activeEffects?.RemoveAt(i);
                }

            effectsActive = false;
        }
        else
        {
            yield return null;
        }
    }

    public List<EffectInstance> PlayFullBodyEffect(float duration, EffectData upperArmLeftData = null, EffectData upperArmRightData = null, EffectData lowerArmLeftData = null, EffectData lowerArmRightData = null, EffectData torsoData = null, EffectData upperLegLeftData = null, EffectData upperLegRightData = null, EffectData lowerLegLeftData = null, EffectData lowerLegRightData = null, bool infinite = false)
    {
        if (!effectsActive) Golem.local.StartCoroutine(EffectRoutine(duration, upperArmLeftData, upperArmRightData, lowerArmLeftData, lowerArmRightData, torsoData, upperLegLeftData, upperLegRightData, lowerLegLeftData, lowerLegRightData, infinite));
        return activeEffects;
    }

    public void RefreshParts()
    {
        foreach (var parts in parts) parts.Value.UpdateChildParts();
    }
}

public static class GolemExtension
{
    /// <summary>
    /// Accesses the golem brain, with some sneaky, but messy trickery.
    /// </summary>
    /// <param name="golem"></param>
    /// <returns></returns>
    public static GolemBrain Brain(this GolemController golem)
    {
        return GolemBrain.Instance;
    }
}