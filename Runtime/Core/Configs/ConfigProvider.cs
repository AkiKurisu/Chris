using Chris.Serialization;

namespace Chris.Configs
{
    public interface IConfigFileProvider
    {
        bool TryGetConfigFile(ConfigFileLocation location, out IConfigFile config);
    }
    
    [PreferJsonConvert]
    public interface IConfigFile
    {
        bool TryGetConfig(IConfigLocation location, out ConfigBase config);

        void SetConfig(IConfigLocation location, ConfigBase config);
    }

    internal class SaveLoadConfigFileProvider : IConfigFileProvider
    {
        private readonly SaveLoadSerializer _serializer;

        protected SaveLoadConfigFileProvider(SaveLoadSerializer serializer)
        {
            _serializer = serializer;
        }
        
        public bool TryGetConfigFile(ConfigFileLocation location, out IConfigFile config)
        {
            ConfigFile configFile = null;
            if (_serializer.Exists(location.Path))
            {
                configFile = _serializer.Deserialize<ConfigFile>(location.Path);
            }

            if (configFile != null)
            {
                configFile.Location = location;
                config = configFile;
                return true;
            }

            config = null;
            return false;
        }
    }
    
    internal class StreamingConfigFileProvider : SaveLoadConfigFileProvider
    {
        // Use binary format
        public StreamingConfigFileProvider() : 
            base(new SaveLoadSerializer(ConfigsModule.ConfigStreamingDirectory, ConfigsModule.ConfigExtension))
        {
        }
    }
    
    internal class PersistentConfigFileProvider : SaveLoadConfigFileProvider
    {
        public PersistentConfigFileProvider() : base(ConfigsModule.PersistentSerializer)
        {
        }
    }
}