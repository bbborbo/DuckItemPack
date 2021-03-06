using BepInEx.Configuration;
using DuckItemPack.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace DuckItemPack.Items
{
    class Fuse : ItemBase
    {
        public static GameObject fuseNovaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static BuffDef fuseRecharge;
        public static float fuseRechargeTime = 1;

        public static float baseShield = 25;
        public static float radiusBase = 18;
        public static float radiusStack = 2;
        public override string ItemName => "Volatile Fuse";

        public override string ItemLangTokenName => "BORBOFUSE";

        public override string ItemPickupDesc => "Creates a Shocking nova when your shields break.";

        public override string ItemFullDescription => $"Gain <style=cIsHealing>{baseShield} shield</style> <style=cStack>(+{baseShield} per stack)</style>. " +
            $"<style=cIsUtility>Breaking your shields</style> creates a nova that " +
            $"<style=cIsUtility>Shocks</style> enemies within <style=cIsUtility>{radiusBase}m</style> " +
            $"<style=cStack>(+{radiusStack} per stack)</style>. " +
            $"<style=cIsDamage>Shock duration scales with shield health</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += FuseTakeDamage;
            GetStatCoefficients += FuseShieldBonus;
        }

        private void FuseShieldBonus(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                args.baseShieldAdd += baseShield * itemCount;
            }
        }

        private void FuseTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            bool hadShieldBefore = HasShield(self);
            CharacterBody body = self.body;
            int fuseItemCount = GetCount(body);

            orig(self, damageInfo);

            if (hadShieldBefore && !HasShield(self) && self.alive)
            {
                if (fuseItemCount > 0 && !body.HasBuff(fuseRecharge))
                {
                    float maxShield = self.body.maxShield;
                    float maxHealth = self.body.maxHealth;
                    float shieldHealthFraction = maxShield / (maxHealth + maxShield);

                    float currentRadius = radiusBase + radiusStack * (fuseItemCount - 1);

                    EffectManager.SpawnEffect(fuseNovaEffectPrefab, new EffectData
                    {
                        origin = self.transform.position,
                        scale = currentRadius
                    }, true);
                    BlastAttack fuseNova = new BlastAttack()
                    {
                        baseDamage = self.body.damage,
                        radius = currentRadius,
                        procCoefficient = Mathf.Min(shieldHealthFraction + 0.1f, 1),
                        position = self.transform.position,
                        attacker = self.gameObject,
                        crit = Util.CheckRoll(self.body.crit, self.body.master),
                        falloffModel = BlastAttack.FalloffModel.None,
                        damageType = DamageType.Shock5s,
                        teamIndex = TeamComponent.GetObjectTeam(self.gameObject)
                    };
                    fuseNova.Fire();

                    self.body.AddTimedBuffAuthority(fuseRecharge.buffIndex, fuseRechargeTime);
                }
            }
        }

        public static bool HasShield(HealthComponent hc)
        {
            return hc.shield > 1;
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        private void CreateBuff()
        {
            fuseRecharge = ScriptableObject.CreateInstance<BuffDef>();
            {
                fuseRecharge.name = "FuseRechargeDebuff";
                fuseRecharge.buffColor = Color.cyan;
                fuseRecharge.canStack = false;
                fuseRecharge.isDebuff = true;
                fuseRecharge.iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffTeslaIcon");
            };
            Assets.buffDefs.Add(fuseRecharge);
        }
    }
}
