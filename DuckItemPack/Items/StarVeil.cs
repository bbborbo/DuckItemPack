using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DuckItemPack.Items
{
    class StarVeil : ItemBase
    {
        static float iframeDurationBase = 0.34f;
        static float iframeDurationStack = 0.33f;

        static float stormDurationMultiplier = 3;
        static float stormDamageCoefficient = 4f;

        static float baseDuration = (iframeDurationBase + iframeDurationStack);

        public override string ItemName => "Star Veil";

        public override string ItemLangTokenName => "STARVEIL";

        public override string ItemPickupDesc => "Taking damage causes you to become invincible... " +
            "<style=cIsHealth>BUT meteors will fall nearby, hurting both enemies and allies.</style>";

        public override string ItemFullDescription => $"After getting hit, become <style=cIsUtility>invincible to all incoming damage</style> " +
            $"for {baseDuration} <style=cStack>(+{iframeDurationStack} per stack)</style> seconds. " +
            $"Then, cause a <style=cIsDamage>rain of meteors</style> to fall from the sky " +
            $"for {Mathf.RoundToInt(baseDuration * stormDurationMultiplier)} " +
            $"<style=cStack>(+{Mathf.RoundToInt(iframeDurationStack * stormDurationMultiplier)} per stack)</style> seconds, " +
            $"<style=cIsHealth>damaging ALL characters</style> for <style=cIsDamage>{Tools.ConvertDecimal(stormDamageCoefficient)} damage per blast.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += StarVeilTakeDamage;
        }

        private void StarVeilTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            if (!damageInfo.rejected && damageInfo.procCoefficient > 0)
            {
                int itemCount = GetCount(self.body);
                if (itemCount > 0 && !self.body.HasBuff(RoR2Content.Buffs.Immune) && !damageInfo.damageType.HasFlag(DamageType.Silent))
                {
                    float iframes = iframeDurationBase + iframeDurationStack * itemCount;
                    self.body.AddTimedBuffAuthority(RoR2Content.Buffs.Immune.buffIndex, iframes);

                    MeteorStormController stormController =
                        UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm"),
                        self.body.corePosition, Quaternion.identity).GetComponent<MeteorStormController>();
                    stormController.owner = self.gameObject;
                    stormController.ownerDamage = self.body.damage;
                    stormController.isCrit = Util.CheckRoll(self.body.crit, self.body.master);
                    stormController.waveCount = Mathf.CeilToInt(iframes * stormDurationMultiplier);
                    stormController.impactDelay = 2;// Mathf.Min(iframes, 2);
                    stormController.blastRadius = 8f;
                    stormController.waveMinInterval = 0.25f;
                    stormController.waveMaxInterval = 0.75f;
                    stormController.blastDamageCoefficient = stormDamageCoefficient;
                    NetworkServer.Spawn(stormController.gameObject);
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
