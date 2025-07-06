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
    public class ConfigsModule: RuntimeModule
    {
#if UNITY_ANDROID
        public static readonly string ActualStreamingDirectory = Path.Combine(Application.persistentDataPath, "Configs");
#else
        public static readonly string ActualStreamingDirectory = Path.Combine(Application.streamingAssetsPath, "Configs");
#endif
        
        public static readonly string StreamingDirectory = Path.Combine(Application.streamingAssetsPath, "Configs");
        
        public static readonly string PersistentDirectory = Path.Combine(SaveUtility.SavePath, "Configs");
        
        internal static readonly string EditorDirectory = Path.Combine(Application.dataPath, "../Configs");
        
        public const string Extension = "cfg";

        /// <summary>
        /// Get runtime config serializer
        /// </summary>
        public static readonly SaveLoadSerializer PersistentSerializer = new(PersistentDirectory, Extension, TextSerializeFormatter.Instance);
        
        public override void Initialize(ModuleConfig config)
        {
#if UNITY_ANDROID || UNITY_EDITOR
            // Transfer streaming configs archive to actual streaming configs directory
            ExtractStreamingConfigs();
#endif
        }
        
        // ReSharper disable once UnusedMember.Local
        private static void ExtractStreamingConfigs()
        {
            try
            {
                using var request = UnityWebRequest.Get(new Uri(StreamingDirectory + ".zip").AbsoluteUri);
                request.downloadHandler = new DownloadHandlerFile(PersistentDirectory);
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

            // Remove invalid file when zip is nil
            if (File.Exists(PersistentDirectory))
            {
                File.Delete(PersistentDirectory);
            }
            
            var zipPath = $"{PersistentDirectory}/Configs.zip";
            if (File.Exists(zipPath))
            {
                ZipWrapper.UnzipFile(zipPath, ActualStreamingDirectory);
                File.Delete(zipPath);
            }
        }
    }
}