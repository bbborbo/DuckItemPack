using R2API.Utils;
using System;
using RoR2;
using UnityEngine;
using RoR2.Projectile;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using BepInEx;
using R2API;
using BepInEx.Bootstrap;
using MonoMod.Cil;
using Mono.Cecil.Cil;


using System.Security;
using System.Security.Permissions;
using DuckItemPack.Items;
using DuckItemPack.Equipment;
using BepInEx.Configuration;
using DuckItemPack.CoreModules;


#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace DuckItemPack
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin("com.Borbo.DuckItemPack", "DuckItempack", "0.1.4")]
    [R2APISubmoduleDependency(nameof(LoadoutAPI), nameof(LanguageAPI), nameof(BuffAPI), nameof(PrefabAPI), nameof(EffectAPI), nameof(ResourcesAPI), nameof(ItemAPI), nameof(RecalculateStatsAPI))]

    internal partial class Main : BaseUnityPlugin
    {
        public static AssetBundle assetBundle = Tools.LoadAssetBundle(Properties.Resources.itempackbundle);
        public static string modelsPath = "Assets/Models/Prefabs/";
        public static string iconsPath = "Assets/Textures/Icons/";
        private static ConfigFile CustomConfigFile { get;  set; }
        public static ConfigEntry<bool> EnableConfig { get; set; }

        public List<ItemBase> Items = new List<ItemBase>();
        public static Dictionary<ItemBase, bool> ItemStatusDictionary = new Dictionary<ItemBase, bool>();

        public List<EquipmentBase> Equipments = new List<EquipmentBase>();
        public static Dictionary<EquipmentBase, bool> EquipmentStatusDictionary = new Dictionary<EquipmentBase, bool>();

        bool IsConfigEnabled()
        {
            return EnableConfig.Value;
        }

        void Awake()
        {
            InitializeConfig();
            InitializeItems();
            InitializeEquipment();

            InitializeCoreModules();

            new ContentPacks().Initialize();
        }

        private void InitializeConfig()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\DuckItemPack.cfg", true);

            EnableConfig = CustomConfigFile.Bind<bool>("Allow Config Options", "Enable Config", false,
                "Set this to true to enable config options. Please keep in mind that it was not within my design intentions to play this way. " +
                "This is primarily meant for modpack users with tons of mods installed. " +
                "If you have any issues or feedback on my mod balance, please feel free to send in feedback with the contact info in the README or Thunderstore description.");
        }

        void InitializeCoreModules()
        {
            var CoreModuleTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(CoreModule)));

            foreach (var coreModuleType in CoreModuleTypes)
            {
                CoreModule coreModule = (CoreModule)Activator.CreateInstance(coreModuleType);

                coreModule.Init();

                Debug.Log("Core Module: " + coreModule + " Initialized!");
            }
        }

        #region items
        void InitializeItems()
        {
            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

            foreach (var itemType in ItemTypes)
            {
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                if (item.IsHidden)
                    return;

                if (ValidateItem(item, Items))
                {
                    item.Init(CustomConfigFile);
                }
            }
        }

        bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            bool itemEnabled = true;
            if (IsConfigEnabled())
            {
                itemEnabled = CustomConfigFile.Bind<bool>("Items", "Enable Item: " + item.ItemName, true, "Should this item appear in runs?").Value;
            }

            ItemStatusDictionary.Add(item, itemEnabled);

            if (itemEnabled)
            {
                itemList.Add(item);
            }
            return itemEnabled;
        }
        #endregion

        #region equips
        void InitializeEquipment()
        {
            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EquipmentBase)));

            foreach (var equipmentType in EquipmentTypes)
            {
                EquipmentBase equipment = (EquipmentBase)System.Activator.CreateInstance(equipmentType);
                if (equipment.IsHidden)
                    return;

                if (ValidateEquipment(equipment, Equipments))
                {
                    equipment.Init(Config);
                }
            }
        }
        public bool ValidateEquipment(EquipmentBase equipment, List<EquipmentBase> equipmentList)
        {
            bool itemEnabled = true;
            if (IsConfigEnabled())
            {
                itemEnabled = CustomConfigFile.Bind<bool>("Equipment", "Enable Equipment: " + equipment.EquipmentName, true, "Should this item appear in runs?").Value;
            }

            EquipmentStatusDictionary.Add(equipment, itemEnabled);

            if (itemEnabled)
            {
                equipmentList.Add(equipment);
            }
            return itemEnabled;
        }
        #endregion
    }
}
