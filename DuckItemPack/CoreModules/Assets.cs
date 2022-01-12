using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DuckItemPack.CoreModules
{
    class Assets : CoreModule
    {
        public static List<ArtifactDef> artifactDefs = new List<ArtifactDef>();
        public static List<BuffDef> buffDefs = new List<BuffDef>();
        public static List<EffectDef> effectDefs = new List<EffectDef>();
        public static List<SkillFamily> skillFamilies = new List<SkillFamily>();
        public static List<SkillDef> skillDefs = new List<SkillDef>();
        public static List<GameObject> projectilePrefabs = new List<GameObject>();
        public static List<GameObject> networkedObjectPrefabs = new List<GameObject>();

        public static List<ItemDef> itemDefs = new List<ItemDef>();
        public static List<EquipmentDef> equipDefs = new List<EquipmentDef>();

        public override void Init()
        {
            AddExecutionDebuff();
            AddLuckBuff();
            IL.RoR2.HealthComponent.TakeDamage += AddExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues += DisplayExecutionThreshold;
            On.RoR2.CharacterBody.AddTimedBuff_BuffIndex_float += LuckBuffAdd;
            On.RoR2.CharacterBody.RemoveBuff_BuffIndex += LuckBuffRemove;
            On.RoR2.CharacterMaster.OnInventoryChanged += LuckCalculation;
        }

        public static void RecalculateLuck(CharacterMaster master)
        {
            float luck = 0;
            CharacterBody body = master.GetBody();
            if (body)
            {
                luck += body.GetBuffCount(luckBuffIndex);
            }
            luck += (float)master.inventory.GetItemCount(RoR2Content.Items.Clover);
            luck -= (float)master.inventory.GetItemCount(RoR2Content.Items.LunarBadLuck);

            master.luck = luck;
        }
        private void LuckCalculation(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            orig(self);
            RecalculateLuck(self);
        }
        private void LuckBuffRemove(On.RoR2.CharacterBody.orig_RemoveBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
        {
            orig(self, buffType);
            if (buffType == luckBuffIndex.buffIndex)
            {
                RecalculateLuck(self.master);
            }
        }
        private void LuckBuffAdd(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffIndex_float orig, CharacterBody self, BuffIndex buffType, float duration)
        {
            orig(self, buffType, duration);
            if (buffType == luckBuffIndex.buffIndex)
            {
                RecalculateLuck(self.master);
            }
        }

        public static BuffDef executionDebuffIndex;
        public static float newExecutionThresholdBase = 0.15f;
        public static float newExecutionThresholdStack = 0.10f;

        public static BuffDef luckBuffIndex;

        private void AddExecutionDebuff()
        {
            executionDebuffIndex = new BuffDef
            {
                buffColor = Color.white,
                canStack = true,
                isDebuff = false,
                name = "DipExecutionDebuffStackable",
                iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffNullifiedIcon")
            };
            buffDefs.Add(executionDebuffIndex);
        }

        private void AddLuckBuff()
        {
            luckBuffIndex = new BuffDef
            {
                buffColor = Color.green,
                canStack = true,
                isDebuff = false,
                name = "DipLuckBuffStackable",
                iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffNullifiedIcon")
            };
            buffDefs.Add(luckBuffIndex);
        }

        private void AddExecutionThreshold(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int thresholdPosition = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(float.NegativeInfinity),
                x => x.MatchStloc(out thresholdPosition)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_isInFrozenState")
                );

            c.Emit(OpCodes.Ldloc, thresholdPosition);
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((currentThreshold, hc) =>
            {
                float newThreshold = currentThreshold;

                newThreshold = GetExecutionThreshold(currentThreshold, hc);

                return newThreshold;
            });
            c.Emit(OpCodes.Stloc, thresholdPosition);
        }

        static float GetExecutionThreshold(float currentThreshold, HealthComponent healthComponent)
        {
            float newThreshold = currentThreshold;
            CharacterBody body = healthComponent.body;

            if (body != null)
            {
                if (!body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
                {
                    int executionBuffCount = body.GetBuffCount(executionDebuffIndex);
                    if (executionBuffCount > 0)
                    {
                        float threshold = newExecutionThresholdBase + newExecutionThresholdStack * executionBuffCount;
                        if (currentThreshold < threshold)
                        {
                            newThreshold = threshold;
                        }
                    }
                }
            }

            return newThreshold;
        }

        private HealthComponent.HealthBarValues DisplayExecutionThreshold(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues values = orig(self);

            values.cullFraction = Mathf.Clamp01(GetExecutionThreshold(values.cullFraction, self));

            return values;
        }
    }
}
