using HarmonyLib;
using ModConfigMenu.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MoreProjects.Mcm
{


    /// <summary>
    /// The MCM configuration base class which provides methods to work with ConfigValues.
    /// </summary>
    /// <param name="config"></param>
    internal abstract class McmConfigurationBase<TConfig>(TConfig config, Logger logger) where TConfig : IMcmConfigTarget, new()
    {
        public Logger Logger { get; set; } = logger;

        public TConfig Config { get; set; } = config;

        /// <summary>
        /// The defaults for the config.  It is expected that any defaults are set on 
        /// TConfig's construction
        /// </summary>
        private TConfig Defaults { get; set; } = new TConfig();

        /// <summary>
        /// Used to make the keys for read only entries unique.  This prevents the MCM's dictionary
        /// from colliding due to entries that are simply notes or read only.
        /// </summary>
        private static int UniqueId = 0;

        /// <summary>
        /// Attempts to configure the MCM, but logs an error and continues if it fails.
        /// </summary>
        public bool TryConfigure()
        {
            try
            {
                Configure();
                return true;
            }
            catch (FileNotFoundException)
            {
                Logger.Log("Bypassing MCM. The 'Mod Configuration Menu' mod is not loaded. ");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred when configuring MCM");
            }

            return false;
        }

        
        /// <summary>
        /// The T specific configuration.  Use the Create* and OnSave helper functions.
        /// </summary>
        public abstract void Configure();


        protected ConfigValue CreateRestartMessage()
        {
            return new ConfigValue("__Restart", "The game must be restarted for any changes to take effect.", "Restart");

        }

        /// <summary>
        /// Creates a setting that is only available in the config file due to lack of MCM support.
        /// Creates a unique ID for the key to avoid the Save from picking it up.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        protected ConfigValue CreateReadOnly(string propertyName, string header = "Only available in config file")
        {
            int key = UniqueId++;



            object value = AccessTools.Property(typeof(TConfig), propertyName)?.GetValue(Config);

            if(value == null)
            {
                //Try field
                value = AccessTools.Field(typeof(TConfig), propertyName)?.GetValue(Config);
            }

            string formattedValue;

            if (value == null)
            {
                value = "Null";
            }
            if (value is IEnumerable enumList)
            {
                List<string> list = new();

                foreach (var item in enumList)
                {
                    list.Add(item.ToString());
                }

                formattedValue = string.Join(",", list);
            }
            else
            {
                formattedValue = value.ToString();
            }

            string formattedPropertyName = FormatUpperCaseSpaces(propertyName);

            return new ConfigValue(key.ToString(), $@"{formattedPropertyName}: <color=#FFFEC1>{formattedValue}</color>", header);

        }

        /// <summary>
        /// Formats a string with no spaces to having spaces before each uppercase letter.
        /// Used to make a property name easier to read.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private static string FormatUpperCaseSpaces(string propertyName)
        {
            //Since the UI uppercases the text, add spaces to make it easier to read.
            Regex regex = new Regex(@"([A-Z0-9])");
            string formattedPropertyName = regex.Replace(propertyName.ToString(), " $1").TrimStart();
            return formattedPropertyName;
        }

        /// <summary>
        /// Creates a numeric config entry. Limited to MCM's support of int and float.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="tooltip"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="label">If not set, will use the property name, adding spaced before each capital letter.</param>
        /// <param name="header"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        protected ConfigValue CreateConfigProperty<T>(string propertyName,
            string tooltip, T min, T max, string label = "", string header = "General") where T: struct
        {
            object defaultValue = AccessTools.Property(typeof(TConfig), propertyName).GetValue(Defaults);
            object propertyValue = AccessTools.Property(typeof(TConfig), propertyName).GetValue(Config);

            string formattedLabel = label == "" ? FormatUpperCaseSpaces(propertyName) : label;

            switch (typeof(T))
            {
                case Type floatType when floatType == typeof(float):

                    return new ConfigValue(propertyName, propertyValue, header, defaultValue, 
                        tooltip, formattedLabel, Convert.ToSingle(min), Convert.ToSingle(max));
                case Type intType when intType == typeof(int):
                    return new ConfigValue(propertyName, propertyValue, header, defaultValue,
                        tooltip, formattedLabel, Convert.ToInt32(min), Convert.ToInt32(max));
                default:
                    throw new ApplicationException($"Unexpected numeric type '{typeof(T).Name}'");
            }
        }

        /// <summary>
        /// Creates a configuration value.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="tooltip"></param>
        /// <param name="label">If not set, will use the property name, adding spaced before each capital letter.</param>
        /// <param name="header"></param>
        /// <returns></returns>
        protected ConfigValue CreateConfigProperty(string propertyName,
            string tooltip, string label = "", string header = "General")
        {
            object defaultValue = AccessTools.Property(typeof(TConfig), propertyName).GetValue(Defaults);
            object propertyValue = AccessTools.Property(typeof(TConfig), propertyName).GetValue(Config);

            string formattedLabel = label == "" ? FormatUpperCaseSpaces(propertyName) : label;

            return new ConfigValue(propertyName, propertyValue, header, defaultValue, tooltip, formattedLabel);
        }

        /// <summary>
        /// Sets the T's property that matches the ConfigValue key.
        /// Returns false if the property could not be found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        protected bool SetPropertyValue(string key, object value)
        {
            MethodInfo propertyMethod = AccessTools.Property(typeof(TConfig), key)?.GetSetMethod();


            //Try property
            if (propertyMethod != null)
            {
                propertyMethod.Invoke(Config, new object[] { value });
                return true;
            }

            //Try field.
            if (propertyMethod == null)
            {
                //Try field

                var field = AccessTools.Field(typeof(TConfig), key);
                if(field == null)
                {
                    return false;

                }

                field.SetValue(Config, value);
                return true;
            }

            return false;
        }

        protected virtual bool OnSave(Dictionary<string, object> currentConfig, out string feedbackMessage)
        {
            feedbackMessage = "";

            foreach (var entry in currentConfig)
            {
                SetPropertyValue(entry.Key, entry.Value);
            }

            Config.Save();

            return true;
        }
    }
}
