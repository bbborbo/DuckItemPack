using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static DuckItemPack.CoreModules.StatHooks;

namespace DuckItemPack.Items
{
    class ManaFlower : ItemBase
    {
        public static float cdrAmt = 0.08f;
        public override string ItemName => "Nature's Gift";

        public override string ItemLangTokenName => "BORBOMANAFLOWER";

        public override string ItemPickupDesc => "Reduces cooldowns for your primary and secondary skills.";

        public override string ItemFullDescription => $"Reduce <style=cIsUtility>Primary and Secondary skill cooldowns</style> " +
            $"by <style=cIsUtility>{Tools.ConvertDecimal(cdrAmt)}</style> <style=cStack>(+{Tools.ConvertDecimal(cdrAmt)} per stack)</style>.";

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
            BorboGetStatCoefficients += ManaFlowerCdr;
        }

        private void ManaFlowerCdr(CharacterBody sender, BorboStatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                for(int i = 0; i < itemCount; i++)
                {
                    args.primaryCooldownMultiplier *= (1 - cdrAmt);
                    args.secondaryCooldownMultiplier *= (1 - cdrAmt);
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
