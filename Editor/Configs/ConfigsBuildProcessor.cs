using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Chris.Configs.Editor
{
    internal class ConfigsBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }

        private bool _isStreamingAssetsPathEmpty;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            _isStreamingAssetsPathEmpty = false;
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                _isStreamingAssetsPathEmpty = true;
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            ConfigsEditorUtils.ExportAndArchiveConfigs();
        }
        
        public void OnPostprocessBuild(BuildReport report)
        {
            if (_isStreamingAssetsPathEmpty)
            {
                Directory.Delete(Application.streamingAssetsPath, true);
                File.Delete(Application.streamingAssetsPath + ".meta");
            }
        }
    }
}
