using System.IO;
using Chris.Editor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Chris.Configs.Editor
{
    internal class ConfigsBuildProcessor : BuildProcessorWithReport
    {
        private bool _isStreamingAssetsPathEmpty;
        
        protected override void PreprocessBuild(BuildReport report)
        {
            _isStreamingAssetsPathEmpty = false;
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                _isStreamingAssetsPathEmpty = true;
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            ConfigsEditorUtils.ExportAndArchiveConfigs();
        }
        
        protected override void PostprocessBuild(BuildReport report)
        {
            if (_isStreamingAssetsPathEmpty && Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.Delete(Application.streamingAssetsPath, true);
                File.Delete(Application.streamingAssetsPath + ".meta");
            }
        }
    }
}
