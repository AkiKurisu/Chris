using Chris.Serialization;

namespace Chris.Configs
{
    public interface IConfigProvider
    {
        bool TryGetConfig(IConfigLocation location, out Config config);
    }
    
    internal class DefaultConfigProvider : IConfigProvider
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly SavSerializer _savSerializer = new(ConfigsModule.ConfigPersistentDirectory);
#else
        private readonly SaveLoadSerializer _saveLoadSerializer = new(ConfigsModule.ConfigStreamingDirectory);
#endif
        
        public bool TryGetConfig(IConfigLocation location, out Config config)
        {
            config = null;
            if (location is not Config.Location configLocation) return false;
            if (_saveLoadSerializer.Exists(configLocation.Name))
            {
                config = _saveLoadSerializer.Load(configLocation.Name, configLocation.Type, configLocation.PreferJsonConvert) as Config;
            }
            return config != null;
        }
    }
}