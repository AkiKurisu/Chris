using Chris.Serialization;
using UnityEngine;

namespace Chris.Configs
{
    public interface IConfigFileProvider
    {
        bool TryGetConfigFile(ConfigFileLocation location, out IConfigFile config);
    }
    
    [PreferJsonConvert]
    public interface IConfigFile
    {
        (string configName, string configData)[] GetAllConfigData();
        
        bool TryGetConfig(IConfigLocation location, out ConfigBase config);

        void SetConfig(IConfigLocation location, ConfigBase config);
        
        void MergeConfigFile(IConfigFile targetFile);
    }

    internal class SaveLoadConfigFileProvider : IConfigFileProvider
    {
        private readonly SaveLoadSerializer _serializer;

        protected SaveLoadConfigFileProvider(SaveLoadSerializer serializer)
        {
            _serializer = serializer;
        }
        
        public virtual bool TryGetConfigFile(ConfigFileLocation location, out IConfigFile config)
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
        public StreamingConfigFileProvider() : 
            base(
#if UNITY_EDITOR
                new SaveLoadSerializer(ConfigsModule.ConfigEditorDirectory, 
#else
                new SaveLoadSerializer(ConfigsModule.ConfigStreamingDirectory, 
#endif
                ConfigsModule.ConfigExtension, TextSerializeFormatter.Instance))
        {
        }
    }
    
    internal class PersistentConfigFileProvider : SaveLoadConfigFileProvider
    {
        public PersistentConfigFileProvider() : base(ConfigsModule.PersistentSerializer)
        {
        }

        public override bool TryGetConfigFile(ConfigFileLocation location, out IConfigFile config)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                config = null;
                return false;
            }
#endif
            return base.TryGetConfigFile(location, out config);
        }
    }
}