﻿using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace DuckItemPack.Items
{
    class BigBattery : ItemBase
    {
        public static float shieldPercent = 0.04f;
        public static float rechargeRateIncrease = 0.1f;
        public static float aspdIncrease = 0.1f;

        public override string ItemName => "AAAAAAA Battery";

        public override string ItemLangTokenName => "BORBOBIGBATTERY";

        public override string ItemPickupDesc => "Increase shield recharge rate. While shields are active, gain an attack speed bonus.";

        public override string ItemFullDescription => $"Gain a <style=cIsHealing>shield</style> equal to " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(shieldPercent)}</style> of your maximum health. " +
            $"Increases <style=cIsHealing>shield recharge rate</style> " +
            $"by <style=cIsHealing>{Tools.ConvertDecimal(rechargeRateIncrease)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(rechargeRateIncrease)} per stack)</style>. " +
            $"While shields are active, increase <style=cIsDamage>attack speed</style> " +
            $"by <style=cIsDamage>{Tools.ConvertDecimal(rechargeRateIncrease)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(rechargeRateIncrease)} per stack)</style>.";

        public override string ItemLore => 
            "A scientist walked out in a rage, shouting 'Screw naming conventions!' " +
            "and placed what appeared to be a standard battery, shrunk, " +
            "with comically long dimensions onto the table. " +
            "\n \nIt is currently in storage while the scientist is under review.";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility , ItemTag.Damage };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += BatteryAspd;
            On.RoR2.CharacterBody.FixedUpdate += BatteryDelayReduction;
            IL.RoR2.HealthComponent.ServerFixedUpdate += BatteryRechargeIncrease;
        }

        private void BatteryRechargeIncrease(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<RoR2.HealthComponent>(nameof(HealthComponent.body)),
                x => x.MatchCallOrCallvirt<RoR2.CharacterBody>("get_maxShield"),
                x => x.MatchLdcR4(out _)
                );

            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((baseFraction, healthComponent) =>
            {
                float endFraction = baseFraction; // baseFraction is the portion of MAX SHIELDS that gets healed per second
                Inventory inv = healthComponent.body.inventory;

                if(inv != null)
                {
                    endFraction = endFraction;
                }

                return endFraction;
            });
        }

        private void BatteryDelayReduction(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            float outOfDangerStopwatch = self.outOfDangerStopwatch;

            int batteryCount = GetCount(self);
            if (batteryCount > 0)
            {
                float rateIncreaseTotal = Mathf.Min(6f, rechargeRateIncrease * batteryCount);
                self.outOfDangerStopwatch = outOfDangerStopwatch + rateIncreaseTotal * Time.fixedDeltaTime;
            }

            orig(self);
        }

        private void BatteryAspd(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                HealthComponent hc = sender.healthComponent;
                if (hc != null)
                {
                    args.baseShieldAdd += sender.maxHealth * shieldPercent;

                    if (Fuse.HasShield(hc))
                    {
                        args.attackSpeedMultAdd += aspdIncrease * itemCount;
                    }
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}