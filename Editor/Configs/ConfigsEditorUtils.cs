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
#if !UNITY_STANDALONE_WIN
            // For platforms can not access to streaming assets dir, make archive file
            var files = Directory.GetFiles(ConfigsModule.StreamingDirectory);
            if (files.Length > 0)
            {
                if (!ZipWrapper.Zip(files, ConfigsModule.StreamingDirectory + ".zip"))
                {
                    throw new IOException("[Chris] Archive configs failed");
                }
            }
            // Cleanup temporal directory
            Directory.Delete(ConfigsModule.StreamingDirectory, true);
            File.Delete(ConfigsModule.StreamingDirectory + ".meta");
#endif
        }
    }
}