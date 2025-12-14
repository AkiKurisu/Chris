using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.AddressableAssets.Settings;

namespace Chris.Resource.Editor
{
    /// <summary>
    /// Resource exporter for building remote contents.
    /// </summary>
    public sealed class ResourceExporter
    {
        private static readonly LazyDirectory ExportDirectory = new(Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Export"));

        /// <summary>
        /// Export destination directory.
        /// </summary>
        public static string ExportPath => ExportDirectory.GetPath();
        
        private readonly List<IResourceBuilder> _builders = new();

        private readonly ResourceExportContext _context;

        private ResourceExporter(ResourceExportContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Create resource exporter from custom context and builders
        /// </summary>
        /// <param name="context"></param>
        /// <param name="builders"></param>
        /// <returns></returns>
        public static ResourceExporter CreateFromContext(ResourceExportContext context, IResourceBuilder[] builders)
        {
            var exporter = new ResourceExporter(context);
            exporter._builders.AddRange(builders);
            return exporter;
        }
        
        private static string CreateBuildPath(string modName)
        {
            var targetPath = Path.Combine(ExportPath, EditorUserBuildSettings.activeBuildTarget.ToString());
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            var buildPath = Path.Combine(targetPath, modName.Replace(" ", string.Empty));
            if (Directory.Exists(buildPath)) FileUtil.DeleteFileOrDirectory(buildPath);
            Directory.CreateDirectory(buildPath);
            return buildPath;
        }
        
        public bool Export()
        {
            _context.BuildPath = CreateBuildPath(_context.Name);
            BuildPipeline();
            if (BuildContent())
            {
                string achievePath = _context.BuildPath + ".zip";
                if (!ZipTogether(_context.BuildPath, achievePath))
                {
                    LogError("Zip failed!");
                    return false;
                }
                Directory.Delete(_context.BuildPath, true);
                Log($"Export succeed, export path: {achievePath}");
                return true;
            }

            LogError("Build pipeline failed!");
            return false;
        }
        
        private static void LogError(string message)
        {
            Debug.LogError($"<color=#ff2f2f>Resource Exporter</color>: {message}");
        }
        
        private static void Log(string message)
        {
            Debug.Log($"<color=#3aff48>Resource Exporter</color>: {message}");
        }
        
        private static bool ZipTogether(string buildPath, string zipPath)
        {
            return ZipWrapper.Zip(new[] { buildPath }, zipPath);
        }
        
        private bool BuildContent()
        {
            AddressableAssetSettings.BuildPlayerContent(out var result);
            CleanupPipeline();
            return string.IsNullOrEmpty(result.Error);
        }
        
        private void BuildPipeline()
        {
            foreach (var builder in _builders)
            {
                builder.Build(_context);
            }
        }
        
        private void CleanupPipeline()
        {
            foreach (var builder in _builders)
            {
                builder.Cleanup(_context);
            }
        }

        private class LazyDirectory
        {
            private readonly string _path;
        
            private bool _initialized;
        
            public LazyDirectory(string path)
            {
                _path = path;
            }
        
            public string GetPath()
            {
                if (!_initialized) return _path;
            
                Directory.CreateDirectory(_path);
                _initialized = true;
                return _path;
            }
        }
    }
}
