using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreProjects
{
    public static class Plugin
    {

        public static ConfigDirectories ConfigDirectories = new ConfigDirectories();

        public static ModConfig Config { get; private set; }

        public static Logger Logger = new Logger();

        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfig(IModContext context)
        {

            Directory.CreateDirectory(ConfigDirectories.ModPersistenceFolder);
            Config = ModConfig.LoadConfig(ConfigDirectories.ConfigPath);

            ChangeDefaults(Config.ProjectCountMultiplier);
            ChangeUpgrades(Config.ProjectCountMultiplier);


            new Harmony("NBKRedSpy_" + ConfigDirectories.ModAssemblyName).PatchAll();
        }


        /// <summary>
        /// Changes the modifiers of Magnum perks that target the max number of armor and weapon projects.
        /// </summary>
        /// <param name="projectsMultiplier"></param>
        private static void ChangeUpgrades(int projectsMultiplier)
        {
            //Get the modifier to all "more weapons" and "more armors" upgrades.
            var targetUpgrades = Data.MagnumPerks.Records
                .SelectMany(x => x.Modifiers, (perk, modifier) => new { Perk = perk, Modifier = modifier })
                .Where(x => x.Modifier.Parameter == MagnumParameter.WPSTUpgradeMoreWeapon || x.Modifier.Parameter == MagnumParameter.ARMSTUpgradeMoreArmors)
                .ToList();

            //The modifier is read only, but we can create a new object.
            //Preferring this method to manually modifying the incoming data text as the values are already parsed out.
            //Loop though the matches and replace the modifier with a new one.
            foreach (var item in targetUpgrades)
            {
                var oldModifier = item.Modifier;
                item.Perk.Modifiers.Remove(oldModifier);
                item.Perk.Modifiers.Add(new MagnumParameterModifier(oldModifier._spaceshipParameter, oldModifier.OperationType, oldModifier._modifier * projectsMultiplier));
            }
        }

        /// <summary>
        /// Change the default max number of projects that the armor and weapon stations can have.
        /// </summary>
        /// <param name="projectsMultiplier"></param>
        private static void ChangeDefaults(int projectsMultiplier)
        {
            float defaultValue;

            if (Data.MagnumDefaultValues.TryGetValue(MagnumParameter.WPSTUpgradeMoreWeapon, out defaultValue))
            {
                Data.MagnumDefaultValues[MagnumParameter.WPSTUpgradeMoreWeapon] = defaultValue * projectsMultiplier;
            }

            if (Data.MagnumDefaultValues.TryGetValue(MagnumParameter.ARMSTUpgradeMoreArmors, out defaultValue))
            {
                Data.MagnumDefaultValues[MagnumParameter.ARMSTUpgradeMoreArmors] = defaultValue * projectsMultiplier;
            }
        }

    }
}
