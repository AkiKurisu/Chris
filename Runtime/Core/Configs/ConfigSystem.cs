using System.Collections.Generic;

namespace Chris.Configs
{
    public static class ConfigSystem
    {
        private readonly struct ConfigProviderStructure
        {
            public readonly IConfigProvider Provider;
            
            public readonly int Priority;

            public ConfigProviderStructure(IConfigProvider provider, int priority)
            {
                Provider = provider;
                Priority = priority;
            }
        }
        
        
        private static readonly List<ConfigProviderStructure> ConfigProviders = new();

        private static readonly Dictionary<ulong, Config> GlobalConfigs = new();

        static ConfigSystem()
        {
            RegisterConfigProvider(new DefaultConfigProvider(), 100);
        }
        
        /// <summary>
        /// Get global config
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        public static TConfig GetConfig<TConfig>() where TConfig : Config<TConfig>, new()
        {
            ulong id = Config<TConfig>.ConfigTypeId;
            if (GlobalConfigs.TryGetValue(id, out var config))
            {
                return (TConfig)config;
            }
            var address = Config<TConfig>.ConfigLocation;
            var globalConfig = GetConfig<TConfig>(address);
            GlobalConfigs.Add(id, globalConfig);
            return globalConfig;
        }
        
        /// <summary>
        /// Get config from <see cref="IConfigLocation"/>
        /// </summary>
        /// <param name="location"></param>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        public static TConfig GetConfig<TConfig>(IConfigLocation location) where TConfig : Config<TConfig>, new()
        {
            foreach (var provider in ConfigProviders)
            {
                if (provider.Provider.TryGetConfig(location, out var config))
                {
                    if (config is TConfig tConfig) return tConfig;
                }
            }
            // Return class default object
            return new TConfig();
        }

        public static void RegisterConfigProvider(IConfigProvider provider, int priority = 0)
        {
            ConfigProviders.Add(new ConfigProviderStructure(provider, priority));
            ConfigProviders.Sort(static (config1, config2) => config2.Priority.CompareTo(config1.Priority));
        }
    }
}