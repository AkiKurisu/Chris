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
        private readonly SavSerializer _savSerializer = new(ConfigsModule.ConfigStreamingDirectory);
#endif
        
        public bool TryGetConfig(IConfigLocation location, out Config config)
        {
            config = null;
            if (location is not Config.Location globalLocation) return false;
            if (_savSerializer.Exists(globalLocation.Name))
            {
                config = _savSerializer.Load(globalLocation.Name, globalLocation.Type, globalLocation.PreferJsonConvert) as Config;
            }
            return config != null;
        }
    }
}