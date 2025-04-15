using System.IO;
using Chris.Modules;
using Chris.Serialization;
using UnityEngine;
using System;
using UnityEngine.Networking;

namespace Chris.Configs
{
    internal class ConfigsModule: RuntimeModule
    {
        public static readonly string ConfigStreamingDirectory = Path.Combine(Application.streamingAssetsPath, "Configs");
        
        public static readonly string ConfigPersistentDirectory = Path.Combine(SaveUtility.SavePath, "Configs");

        public const string ConfigExtension = "cfg";

#if UNITY_EDITOR
        // In editor, we can just overwrite streaming config
        public static readonly SaveLoadSerializer PersistentSerializer = new(ConfigStreamingDirectory, ConfigExtension);
#else
        public static readonly SavSerializer PersistentSerializer = new(ConfigPersistentDirectory, ConfigExtension);
#endif
        
        
        public override void Initialize(ModuleConfig config)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Transfer streaming configs archive to persistent configs directory
            ExtractStreamingConfigs();
#endif
        }
        
        // ReSharper disable once UnusedMember.Local
        private static void ExtractStreamingConfigs()
        {
            using var request = UnityWebRequest.Get(new Uri(ConfigStreamingDirectory + ".zip").AbsoluteUri);
            request.downloadHandler = new DownloadHandlerFile(ConfigPersistentDirectory);
            request.SendWebRequest();
            while (request.isDone)
            {
                // Block main thread
            }
            ZipWrapper.UnzipFile($"{ConfigPersistentDirectory}/Configs.zip", ConfigPersistentDirectory);
        }
    }
}