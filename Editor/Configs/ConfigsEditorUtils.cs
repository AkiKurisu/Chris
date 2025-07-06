using System.IO;
using Chris.Serialization;

namespace Chris.Configs.Editor
{
    public static class ConfigsEditorUtils
    {
        private static readonly SaveLoadSerializer ConfigSerializer = new(ConfigsModule.EditorDirectory, ConfigsModule.Extension, TextSerializeFormatter.Instance);

        /// <summary>
        /// Get config serializer in editor, cache will be clean up before using.
        /// </summary>
        /// <returns></returns>
        public static SaveLoadSerializer GetConfigSerializer()
        {
            ConfigSystem.ClearCache();
            return ConfigSerializer;
        }
        
        private static void CopyDirectoryRecursively(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            
            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(filePath);
                var destFilePath = Path.Combine(destDir, fileName);
                File.Copy(filePath, destFilePath, true);
            }
            
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryRecursively(subDir, destSubDir);
            }
        }
        
        public static void ExportAndArchiveConfigs()
        {
            // Collect all configs and export to streaming assets
            CopyDirectoryRecursively(ConfigsModule.EditorDirectory, ConfigsModule.StreamingDirectory);
#if UNITY_ANDROID
            // If android, make archive file and delete configs folder
            if (Directory.GetFiles(ConfigsModule.StreamingDirectory).Length > 0)
            {
                if (!ZipWrapper.Zip(new[] { ConfigsModule.StreamingDirectory },
                        ConfigsModule.StreamingDirectory + ".zip"))
                {
                    throw new IOException("[Chris] Archive configs failed");
                }
            }
            Directory.Delete(ConfigsModule.StreamingDirectory, true);
            File.Delete(ConfigsModule.StreamingDirectory + ".meta");
#endif
        }
    }
}