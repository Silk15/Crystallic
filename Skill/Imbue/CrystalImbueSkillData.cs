using ThunderRoad.Skill;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace Crystallic.Skill.Imbue;

public class CrystalImbueSkillData : SpellSkillData
{
    public static List<ThunderRoad.Imbue> imbuesToRevert = new();
    public static Dictionary<Item, string> previousImbues = new();
   
    public EffectData imbueCollisionEffectData;
    public EffectData imbueEffectData;
    public Color colorModifier;
    public string spellId;
    public string imbueHitEffectId;
    public string imbueEffectId;
    public string typeAddress;
    public Type type;
    public bool crystalliseOnHit = true;
    
    public static void SaveImbues()
    {
        foreach (ThunderRoad.Imbue imbue in ThunderRoad.Imbue.all) 
            if (imbue.GetComponents<ImbueBehaviour>().Any(i => i.enabled)) 
                imbuesToRevert.Add(imbue);
    }
    
    public override void OnCatalogRefresh()
    {
        base.OnCatalogRefresh();
        previousImbues.Clear();
        imbueCollisionEffectData = Catalog.GetData<EffectData>(imbueHitEffectId);
        imbueEffectData = Catalog.GetData<EffectData>(imbueEffectId);
        type = Type.GetType(typeAddress);
        
        if (type == null || !type.IsSubclassOf(typeof(ImbueBehaviour)))
            Debug.LogError($"[Crystallic] ImbueBehaviour Type {typeAddress} is not a subclass of ImbueBehaviour!");
    }

    public override void OnImbueLoad(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueLoad(spell, imbue);
        var item = imbue.colliderGroup.collisionHandler.item;

        if (imbuesToRevert.Contains(imbue))
        {
            imbuesToRevert.Remove(imbue);
            LoadImbue(imbue);
            return;
        }
        
        if (spell.id != spellId || previousImbues.IsNullOrEmpty() || !previousImbues.ContainsKey(item) || (previousImbues.TryGetValue(item, out var id) && id != "Crystallic"))
            return;
        
        LoadImbue(imbue);
    }

    public override void OnImbueUnload(SpellData spell, ThunderRoad.Imbue imbue)
    {
        base.OnImbueUnload(spell, imbue);
        UnloadImbue(imbue);
    }

    public void LoadImbue(ThunderRoad.Imbue imbue)
    {
        var behaviour = (ImbueBehaviour)imbue.gameObject.AddComponent(type);
        behaviour.Load(this, imbue);
    }

    public void UnloadImbue(ThunderRoad.Imbue imbue)
    {
        var components = imbue.gameObject.GetComponents<ImbueBehaviour>();
        if (!components.IsNullOrEmpty()) foreach (var imbueBehavior in components) imbueBehavior?.Unload(imbue);
        previousImbues[imbue.colliderGroup.collisionHandler.item] = imbue.spellCastBase.id;
    }
}