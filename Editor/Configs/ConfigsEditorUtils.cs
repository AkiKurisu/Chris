using System.IO;

namespace Chris.Configs.Editor
{
    public static class ConfigsEditorUtils
    {
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
            CopyDirectoryRecursively(ConfigsModule.ConfigPersistentDirectory, ConfigsModule.ConfigStreamingDirectory);
#if UNITY_ANDROID
            // If android, make archive file and delete configs folder
            if (Directory.GetFiles(ConfigsModule.ConfigStreamingDirectory).Length > 0)
            {
                if (!ZipWrapper.Zip(new[] { ConfigsModule.ConfigStreamingDirectory },
                        ConfigsModule.ConfigStreamingDirectory + ".zip"))
                {
                    throw new IOException("[Chris] Archive configs failed");
                }
            }
            Directory.Delete(ConfigsModule.ConfigStreamingDirectory, true);
            File.Delete(ConfigsModule.ConfigStreamingDirectory + ".meta");
#endif
        }
    }
}