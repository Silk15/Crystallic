using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

[Serializable]
public class SaveData
{
    #if !SDK
    public static SaveData instance;
    
    public Dictionary<string, bool> savedCategoryValues = new();
    public List<ModRequirement> savedModRequirements = new();
    
    [NonSerialized, JsonIgnore]
    public List<ModRequirement> modRequirements = new()
    {
        new ModRequirement()
        {
            modName = "Arcana",
            message = "You have \"Arcana\" and \"Crystallic\" installed without the compatability mod \"Crystallic Arcana\", Crystallic's crystal imbues will not function properly without this requirement! \n\n Please ensure you have \"Crystallic Arcana\" installed, a link can be found under the \"Requirements\" tab of either mod.",
            requirementName = "CrystallicArcana"
        }
    };

    public static void LoadAsync(Action onComplete = null) => GameManager.local.StartCoroutine(LoadCoroutine(onComplete));

    public static void SaveAsync(Action onComplete = null) => GameManager.local.StartCoroutine(SaveCoroutine(onComplete));
    
    public static void DeleteAsync(Action onComplete = null) => GameManager.local.StartCoroutine(DeleteCoroutine(onComplete));
    
    private static IEnumerator SaveCoroutine(Action onComplete)
    {
        yield return GameManager.platform.WriteSaveCoroutine(new PlatformBase.Save("Crystallic", "sav", JsonConvert.SerializeObject(instance, Catalog.GetJsonNetSerializerSettings())));
        onComplete?.Invoke();
    }

    private static IEnumerator DeleteCoroutine(Action onComplete)
    {
        yield return GameManager.platform.DeleteSaveCoroutine(new PlatformBase.Save("Crystallic", "sav"));
        onComplete?.Invoke();
    }

    private static IEnumerator LoadCoroutine(Action onComplete)
    {
        PlatformBase.Save save = null;
        yield return GameManager.platform.ReadSaveCoroutine("Crystallic", "sav", value => save = value);

        bool createdNew = false;

        if (save != null && !string.IsNullOrEmpty(save.data))
        {
            try
            {
                instance = JsonConvert.DeserializeObject<SaveData>(save.data, Catalog.GetJsonNetSerializerSettings());
                if (instance == null)
                {
                    Debug.LogWarning("[Crystallic] Deserialized save data was null, creating new one.");
                    instance = new SaveData();
                    createdNew = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Crystallic] Failed to deserialize save data: " + ex.Message);
                instance = new SaveData();
                createdNew = true;
            }
        }
        else
        {
            Debug.Log("[Crystallic] No save data file found, creating new!");
            instance = new SaveData();
            createdNew = true;
        }

        if (instance.savedModRequirements.IsNullOrEmpty() || instance.savedModRequirements.Count < instance.modRequirements.Count)
        {
            instance.savedModRequirements.Clear();
            instance.savedModRequirements = instance.modRequirements.ToList();
        }

        if (createdNew)
            yield return SaveCoroutine(onComplete);
        else
            onComplete?.Invoke();
    }

    [Serializable]
    public class ModRequirement
    {
        public string modName;
        public string message;
        public string requirementName;
        public bool messageSeen = false;
    }
    #endif
}