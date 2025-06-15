using System;
using System.Reflection;
using Chris.Serialization;

namespace Chris.Configs
{
    /// <summary>
    /// Base class for global configs
    /// </summary>
    public abstract class ConfigBase
    {
        protected static ulong NextConfigId;

        internal class ConfigLocation : IConfigLocation
        {
            public ConfigFileLocation FileLocation { get; }

            public string ConfigName { get; }

            public Type Type { get; }

            public bool PreferJsonConvert { get; }

            public ConfigLocation(string filePath, string configName, Type type, bool preferJsonConvert)
            {
                FileLocation = filePath;
                ConfigName = configName;
                Type = type;
                PreferJsonConvert = preferJsonConvert;
            }
        }
    }

    /// <summary>
    /// Generic global config
    /// </summary>
    /// <typeparam name="TConfig"></typeparam>
    public abstract class Config<TConfig> : ConfigBase
        where TConfig : Config<TConfig>, new()
    {
        /// <summary>
        /// Check if this config is a root config (has no parent)
        /// </summary>
        public static bool IsRoot => string.IsNullOrEmpty(ConfigPath) || !ConfigPath.Contains('.');

        /// <summary>
        /// Get parent config path if exists
        /// </summary>
        public static string ParentPath
        {
            get
            {
                if (IsRoot) return null;
                var parts = ConfigPath.Split('.');
                return parts[0];
            }
        }

        /// <summary>
        /// Get config name if exists
        /// </summary>
        public static string Name
        {
            get
            {
                if (string.IsNullOrEmpty(ConfigPath)) return null;
                if (!ConfigPath.Contains('.')) return ConfigPath;
                var parts = ConfigPath.Split('.');
                return string.Concat(parts[1..]);
            }
        }

        /// <summary>
        /// Get config location
        /// </summary>
        public static IConfigLocation Location { get; }

        private static readonly string ConfigPath;
        
        internal static readonly ulong TypeId;

        static Config()
        {
            TypeId = ++NextConfigId;
            var configAttribute = typeof(TConfig).GetCustomAttribute<ConfigPathAttribute>();
            if (configAttribute != null)
            {
                ConfigPath = configAttribute.Path;
            }
            ConfigPath = string.IsNullOrEmpty(ConfigPath) ? typeof(TConfig).Name : ConfigPath;
            Location = new ConfigLocation(ParentPath, Name, typeof(TConfig), SaveLoadSerializer.TypeCache<TConfig>.PreferJsonConvert);
        }

        public static TConfig Get()
        {
            return ConfigSystem.GetConfig<TConfig>();
        }

        /// <summary>
        /// Save config to persistent save path
        /// </summary>
        public void Save()
        {
            Save(ConfigsModule.PersistentSerializer);
        }

        /// <summary>
        /// Save config by providing serializer
        /// </summary>
        public void Save(SaveLoadSerializer serializer)
        {
            var configFile = ConfigSystem.GetConfigFile(Location.FileLocation);
            configFile.SetConfig(Location, this);
            serializer.Serialize(Location.FileLocation, configFile);
        }
    }
}
