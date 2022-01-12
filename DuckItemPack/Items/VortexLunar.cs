using BepInEx.Configuration;
using DuckItemPack.CoreModules;
using HG;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DuckItemPack.Items
{
    class VortexLunar : ItemBase
    {
        public static BuffDef vortexCooldownDebuff;
        public static float baseCooldown = 7;
        public static float stackCooldown = -1;
        public static float minDamageCoefficient = 3;

        public static float damageCoefficient = 0.5f;
        public static float procCoefficient = 1;
        public static float baseRadius = 15;
        public static float stackRadius = 5;

        public override string ItemName => "Ascended Vortex";

        public override string ItemLangTokenName => "LUNARMEATHOOK";

        public override string ItemPickupDesc => "High damage hits pull in ALL nearby enemies AND allies. Recharges over time.";

        public override string ItemFullDescription => $"On hits that deal <style=cIsDamage>more than {Tools.ConvertDecimal(minDamageCoefficient)} damage</style>, " +
            $"create an otherworldly vortex that <style=cIsHealth>pulls in nearby enemies AND allies</style> " +
            $"within {baseRadius}m <style=cStack>(+{stackRadius} per stack)</style>. " +
            $"Recharges every <style=cIsUtility>{baseCooldown}</style> seconds <style=cStack>(-{0 - stackCooldown} per stack)</style>.";

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
            On.RoR2.GlobalEventManager.OnHitEnemy += AscendedVortexOnHit;
        }

        private void AscendedVortexOnHit(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
        {
            if(!damageInfo.rejected && damageInfo.procCoefficient != 0 && damageInfo.attacker != null)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();

                if(attackerBody != null && !damageInfo.procChainMask.HasProc(ProcType.BounceNearby) && !attackerBody.HasBuff(vortexCooldownDebuff))
                {
                    int vortexItemCount = GetCount(attackerBody);
                    float damageCoefficient2 = damageInfo.damage / (attackerBody.damage * minDamageCoefficient);
                    if (vortexItemCount > 0 && damageCoefficient2 >= 1)
                    {
                        float vortexRadius = baseRadius + stackRadius * (vortexItemCount - 1);
                        float vortexDamage = attackerBody.damage * damageCoefficient;
                        float vortexCooldown = Mathf.Max(0.5f, baseCooldown + stackCooldown * (vortexItemCount - 1));
                        attackerBody.AddTimedBuffAuthority(vortexCooldownDebuff.buffIndex, vortexCooldown);

                        #region hurtbox search
                        List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
                        BullseyeSearch search = new BullseyeSearch();
                        List<HealthComponent> list2 = CollectionPool<HealthComponent, List<HealthComponent>>.RentCollection();

                        // whitelist victim and attacker
                        if (true)
                        {
                            if (attackerBody.healthComponent && false)
                            {
                                list2.Add(attackerBody.healthComponent);
                            }
                            if (victimBody && victimBody.healthComponent)
                            {
                                list2.Add(victimBody.healthComponent);
                            }
                        }

                        BounceOrb.SearchForTargets(search, TeamIndex.None, damageInfo.position, vortexRadius, 100, list, list2);
                        CollectionPool<HealthComponent, List<HealthComponent>>.ReturnCollection(list2);
                        List<HealthComponent> bouncedObjects = new List<HealthComponent>
                                {
                                    victim.GetComponent<HealthComponent>()
                                };
                        #endregion


                        EffectManager.SpawnEffect(Fuse.fuseNovaEffectPrefab, new EffectData
                        {
                            origin = damageInfo.position,
                            scale = vortexRadius
                        }, true);

                        for (int i = 0; i < list.Count; i++)
                        {
                            HurtBox hurtBox3 = list[i];
                            if (hurtBox3)
                            {
                                /*BounceOrb bounceOrb = new BounceOrb();
                                bounceOrb.origin = damageInfo.position;
                                bounceOrb.damageValue = vortexDamage;
                                bounceOrb.isCrit = damageInfo.crit;
                                bounceOrb.teamIndex = TeamIndex.Neutral;
                                bounceOrb.attacker = damageInfo.attacker;
                                bounceOrb.procChainMask = damageInfo.procChainMask;
                                bounceOrb.procChainMask.AddProc(ProcType.BounceNearby);
                                bounceOrb.procCoefficient = procCoefficient;
                                bounceOrb.damageColorIndex = DamageColorIndex.Item;
                                bounceOrb.bouncedObjects = bouncedObjects;
                                bounceOrb.target = hurtBox3;
                                OrbManager.instance.AddOrb(bounceOrb);*/

                                HealthComponent healthComponent = hurtBox3.healthComponent;
                                if (healthComponent)
                                {
                                    float forceMultiplier = 1;
                                    if(healthComponent.body.characterMotor != null)
                                    {
                                        forceMultiplier = healthComponent.body.characterMotor.mass;
                                    }
                                    else if(healthComponent.body.rigidbody != null)
                                    {
                                        forceMultiplier = healthComponent.body.rigidbody.mass;
                                    }

                                    Vector3 position = hurtBox3.transform.position;
                                    GameObject gameObject = healthComponent.gameObject;
                                    DamageInfo di = new DamageInfo()
                                    {
                                        damage = vortexDamage,
                                        attacker = damageInfo.attacker,
                                        inflictor = null,
                                        force = (position - damageInfo.position).normalized * -15f * forceMultiplier * Mathf.Min(damageCoefficient2, 3),
                                        crit = damageInfo.crit,
                                        procChainMask = damageInfo.procChainMask,
                                        procCoefficient = procCoefficient,
                                        position = position,
                                        damageColorIndex = DamageColorIndex.Item
                                    };
                                    healthComponent.TakeDamage(di);
                                    GlobalEventManager.instance.OnHitEnemy(di, gameObject);
                                    GlobalEventManager.instance.OnHitAll(di, gameObject);
                                }
                            }
                        }

                        //return the collection, not sure what this does
                        CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
                    }
                }
            }
            orig(self, damageInfo, victim);
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
            vortexCooldownDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                vortexCooldownDebuff.name = "VortexCooldown";
                vortexCooldownDebuff.buffColor = Color.magenta;
                vortexCooldownDebuff.canStack = false;
                vortexCooldownDebuff.isDebuff = true;
                vortexCooldownDebuff.iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffMercExposeIcon");
            };
            Assets.buffDefs.Add(vortexCooldownDebuff);
        }
    }
}
