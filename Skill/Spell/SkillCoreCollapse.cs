using System;
using Crystallic.Skill.SpellMerge;
using ThunderRoad;
using ThunderRoad.Skill;
using TriInspector;
using UnityEngine;

namespace Crystallic.Skill.Spell
{
    public class SkillCoreCollapse : SpellSkillData
    {
        #if !SDK
        [ModOption("Collapse Enemy Damage", "Controls the amount of damage dealt to enemies."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Core Collapse", 21)]
        public static float enemyDamage = 10f;

        [ModOption("Collapse Radius", "Controls the radius of the detonation."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Core Collapse", 21)]
        public static float radius = 5f;

        [ModOption("Collapse Force", "Controls the force applied to enemies and items."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Core Collapse", 21)]
        public static float force = 80f;

        [ModOption("Collapse Breakable Damage", "Controls the amount of damage dealt to breakable items like crates and ceramics."), ModOptionFloatValues(0f, 100f, 1f), ModOptionSlider, ModOptionCategory("Core Collapse", 21)]
        public static float breakForce = 40f;
        #endif

        [NonSerialized]
        public EffectData collapseEffectData;

        [Dropdown(nameof(GetAllEffectID))]
        public string collapseEffectId;

        #if !SDK
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            collapseEffectData = Catalog.GetData<EffectData>(collapseEffectId);
        }

        public override void OnSpellLoad(SpellData spell, SpellCaster caster = null)
        {
            base.OnSpellLoad(spell, caster);
            if (spell is not SpellMergeCrystallic spellMergeCrystallic) return;
            spellMergeCrystallic.onCoreCollapsed -= OnCoreCollapsed;
            spellMergeCrystallic.onCoreCollapsed += OnCoreCollapsed;
        }

        public override void OnSpellUnload(SpellData spell, SpellCaster caster = null)
        {
            base.OnSpellUnload(spell, caster);
            if (spell is not SpellMergeCrystallic spellMergeCrystallic) return;
            spellMergeCrystallic.onCoreCollapsed -= OnCoreCollapsed;
        }

        private void OnCoreCollapsed(SpellMergeCrystallic spellMergeCrystallic, ItemMagicProjectile itemMagicProjectile)
        {
            collapseEffectData.Spawn(itemMagicProjectile.transform.position, Quaternion.identity).Play();
            foreach ((ThunderEntity, Vector3) closestPoint in ThunderEntity.InRadiusClosestPoint(itemMagicProjectile.transform.position, radius))
            {
                switch (closestPoint.Item1)
                {
                    case Creature creature when !creature.isPlayer && creature.IsEnemy(spellMergeCrystallic.mana.creature):
                        var brainModuleSpeak = creature.brain.instance.GetModule<BrainModuleSpeak>();

                        if (!creature.isKilled)
                            brainModuleSpeak.Play(BrainModuleSpeak.hashFalling, false);

                        creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                        creature.AddExplosionForce(force, itemMagicProjectile.transform.position, radius, 0.5f, ForceMode.Impulse);
                        creature.Damage(enemyDamage);
                        break;

                    case Item item when !item.TryGetComponent(out ItemMagicProjectile _):
                        item.AddExplosionForce(force, itemMagicProjectile.transform.position, radius, 0.5f, ForceMode.Impulse);

                        Breakable breakable = item.breakable;
                        if (breakable != null && !breakable.contactBreakOnly)
                            item.breakable.Explode(breakForce, itemMagicProjectile.transform.position, radius, 0.0f, ForceMode.Impulse);
                        break;
                }
            }
        }
        #endif
    }
}