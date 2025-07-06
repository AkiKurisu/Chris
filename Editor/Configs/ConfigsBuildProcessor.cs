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
        
        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            _isStreamingAssetsPathEmpty = false;
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                _isStreamingAssetsPathEmpty = true;
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            ConfigsEditorUtils.ExportAndArchiveConfigs();
        }
        
        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            if (_isStreamingAssetsPathEmpty)
            {
                Directory.Delete(Application.streamingAssetsPath, true);
                File.Delete(Application.streamingAssetsPath + ".meta");
            }
        }
    }
}
