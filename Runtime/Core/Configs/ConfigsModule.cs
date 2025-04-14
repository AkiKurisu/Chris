using System.IO;
using Chris.Modules;
using Chris.Serialization;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
#endif

namespace Chris.Configs
{
    internal class ConfigsModule: RuntimeModule
    {
        public static readonly string ConfigStreamingDirectory = Path.Combine(Application.streamingAssetsPath, "Configs");
        
        public static readonly string ConfigPersistentDirectory = Path.Combine(SaveUtility.SavePath, "Configs");

#if UNITY_EDITOR
        // In editor, we can just overwrite streaming config
        public static readonly SavSerializer PersistentSerializer = new(ConfigStreamingDirectory);
#else
        public static readonly SavSerializer PersistentSerializer = new(ConfigPersistentDirectory);
#endif
        
        
        public override void Initialize(ModuleConfig config)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Transfer streaming configs archive to persistent configs directory
            ExtractStreamingConfigs().Forget();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static async UniTask ExtractStreamingConfigs()
        {
            using UnityWebRequest request = UnityWebRequest.Get(new Uri(ConfigStreamingDirectory + ".zip").AbsoluteUri);
            request.downloadHandler = new DownloadHandlerFile(ConfigPersistentDirectory);
            await request.SendWebRequest().ToUniTask();
            ZipWrapper.UnzipFile($"{ConfigPersistentDirectory}/Configs.zip", ConfigPersistentDirectory);
        }
#endif
    }
}