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
    
#if UNITY_EDITOR
    internal class EditorBaseConfigFileProvider : SaveLoadConfigFileProvider
    {
        public EditorBaseConfigFileProvider() : 
            base(new SaveLoadSerializer(ConfigsModule.EditorBaseDirectory, ConfigsModule.Extension, TextSerializeFormatter.Instance))
        {
        }
    }
    
    internal class EditorConfigFileProvider : SaveLoadConfigFileProvider
    {
        public EditorConfigFileProvider() : 
            base(new SaveLoadSerializer(ConfigsModule.EditorPlatformDirectory, ConfigsModule.Extension, TextSerializeFormatter.Instance))
        {
        }
    }
#else
    internal class StreamingConfigFileProvider : SaveLoadConfigFileProvider
    {
        public StreamingConfigFileProvider() : 
            base(new SaveLoadSerializer(ConfigsModule.ActualStreamingDirectory, ConfigsModule.Extension, TextSerializeFormatter.Instance))
        {
        }
    }
#endif
    
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