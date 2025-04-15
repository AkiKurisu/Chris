using Chris.Serialization;

namespace Chris.Configs
{
    public interface IConfigProvider
    {
        bool TryGetConfig(IConfigLocation location, out Config config);
    }
    
    internal class StreamingConfigProvider : IConfigProvider
    {
        private readonly SaveLoadSerializer _saveLoadSerializer = new(ConfigsModule.ConfigStreamingDirectory, ConfigsModule.ConfigExtension);
        
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
    
    internal class PersistentConfigProvider : IConfigProvider
    {
        private readonly SaveLoadSerializer _saveLoadSerializer = ConfigsModule.PersistentSerializer;
        
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