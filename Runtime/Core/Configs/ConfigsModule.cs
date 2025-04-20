using System.IO;
using Chris.Modules;
using Chris.Serialization;
using UnityEngine;
using System;
using UnityEngine.Networking;
using UnityEngine.Scripting;

namespace Chris.Configs
{
    [Preserve]
    internal class ConfigsModule: RuntimeModule
    {
        public static readonly string ConfigStreamingDirectory = Path.Combine(Application.streamingAssetsPath, "Configs");
        
        public static readonly string ConfigPersistentDirectory = Path.Combine(SaveUtility.SavePath, "Configs");

        public const string ConfigExtension = "cfg";

        public static readonly SaveLoadSerializer PersistentSerializer = new(ConfigPersistentDirectory, ConfigExtension);
        
        public override void Initialize(ModuleConfig config)
        {
#if UNITY_ANDROID || UNITY_EDITOR
            // Transfer streaming configs archive to persistent configs directory
            ExtractStreamingConfigs();
#endif
        }
        
        // ReSharper disable once UnusedMember.Local
        private static void ExtractStreamingConfigs()
        {
            try
            {
                using var request = UnityWebRequest.Get(new Uri(ConfigStreamingDirectory + ".zip").AbsoluteUri);
                request.downloadHandler = new DownloadHandlerFile(ConfigPersistentDirectory);
                request.SendWebRequest();
                while (request.isDone)
                {
                    // Block main thread
                }
            }
            // Do not exist, skip
            catch
            {
                // ignored
            }

            // Remove invalid file
            if (File.Exists(ConfigPersistentDirectory))
            {
                File.Delete(ConfigPersistentDirectory);
            }
            
            var zipPath = $"{ConfigPersistentDirectory}/Configs.zip";
            if (File.Exists(zipPath))
            {
                ZipWrapper.UnzipFile(zipPath, ConfigPersistentDirectory);
            }
        }
    }
}