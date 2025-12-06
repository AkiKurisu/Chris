using System.Collections.Generic;
using Newtonsoft.Json;
using R3.Chris;

namespace Chris.Configs
{
    public static class ConfigSystem
    {
        private readonly struct ConfigFileProviderStructure
        {
            public readonly IConfigFileProvider FileProvider;

            public readonly int Priority;

            public ConfigFileProviderStructure(IConfigFileProvider fileProvider, int priority)
            {
                FileProvider = fileProvider;
                Priority = priority;
            }
        }

        private static readonly List<ConfigFileProviderStructure> ConfigFileProviders = new();

        private static readonly Dictionary<ulong, ConfigBase> ConfigCache = new();

        private static readonly Dictionary<string, IConfigFile> ConfigFileCache = new();
        
        private static readonly JsonSerializerSettings SerializerSettings;
        
        static ConfigSystem()
        {
            // Register ReactivePropertyConverter used in Config
            if (JsonConvert.DefaultSettings == null)
            {
                SerializerSettings = new JsonSerializerSettings();
                JsonConvert.DefaultSettings = GetDefaultSettings;
            }
            else
            {
                SerializerSettings = JsonConvert.DefaultSettings();
            }
            SerializerSettings.Converters.Add(new ReactivePropertyConverter());
            
            // Register framework-level config providers first
#if UNITY_EDITOR
            RegisterConfigFileProvider(new EditorConfigFileProvider(), 200);        // Platform specific
            RegisterConfigFileProvider(new EditorBaseConfigFileProvider(), 300);    // Project Base
#else
            RegisterConfigFileProvider(new StreamingConfigFileProvider(), 200);
#endif

            // Pre-load ConfigsConfig to initialize serializer configuration
            _ = ConfigsConfig.GetConfigSerializer();
            ClearCache();

            // Register user-level config provider with configurable serializer
            RegisterConfigFileProvider(new PersistentConfigFileProvider(), 100);
        }

        internal static void ClearCache()
        {
            ConfigFileCache.Clear();
            ConfigCache.Clear();
        }
        
        private static JsonSerializerSettings GetDefaultSettings()
        {
            return SerializerSettings;
        }

        /// <summary>
        /// Get global config
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        public static TConfig GetConfig<TConfig>() where TConfig : Config<TConfig>, new()
        {
            // Try get from config cache
            ulong id = Config<TConfig>.TypeId;
            if (ConfigCache.TryGetValue(id, out var config))
            {
                return (TConfig)config;
            }
            var location = Config<TConfig>.Location;
            var globalConfig = GetConfigFromFileCache<TConfig>(location);
            ConfigCache.Add(id, globalConfig);
            return globalConfig;
        }

        /// <summary>
        /// Get config from cache by <see cref="IConfigLocation"/>
        /// </summary>
        /// <param name="location"></param>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        private static TConfig GetConfigFromFileCache<TConfig>(IConfigLocation location) where TConfig : Config<TConfig>, new()
        {
            var configFile = GetConfigFile(location.FileLocation);

            // Try to get config
            if (configFile.TryGetConfig(location, out var config))
            {
                return (TConfig)config;
            }

            // Return class default object
            return new TConfig();
        }

        /// <summary>
        /// Get <see cref="IConfigFile"/> from <see cref="ConfigFileLocation"/>
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static IConfigFile GetConfigFile(ConfigFileLocation location)
        {
            if (ConfigFileCache.TryGetValue(location.Path, out var configFile))
            {
                return configFile;
            }

            foreach (var provider in ConfigFileProviders)
            {
                // Try to get config file
                if (provider.FileProvider.TryGetConfigFile(location, out var newConfigFile))
                {
                    if (configFile == null)
                    {
                        configFile = newConfigFile;
                        continue;
                    }

                    configFile.MergeConfigFile(newConfigFile);
                }
            }

            configFile ??= new ConfigFile(location);
            ConfigFileCache.Add(location.Path, configFile);
            return configFile;
        }

        public static void RegisterConfigFileProvider(IConfigFileProvider fileProvider, int priority = 0)
        {
            ConfigFileProviders.Add(new ConfigFileProviderStructure(fileProvider, priority));
            // Higher priority, lower prio.
            ConfigFileProviders.Sort(static (config1, config2) => config2.Priority.CompareTo(config1.Priority));
        }
    }
}