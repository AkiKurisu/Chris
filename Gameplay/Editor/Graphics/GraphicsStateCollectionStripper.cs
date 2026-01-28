// Reference: URP17 Samples
#if UNITY_6000_3_OR_NEWER
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Chris.Gameplay.Graphics.Editor
{
    // This script is used to make sure build contains only the GraphicsStateCollections files that matches with build target platform,
    // by moving the unwanted collections to a temp folder before build and restoring them after build.
    // i.e. A Windows build will not contain any OSX GraphicsStateCollections etc.
    internal class GraphicsStateCollectionStripper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private struct StrippedFile
        {
            public string OriginalPath;
            public string TempPath;
        }

        private readonly List<StrippedFile> m_StrippedFiles = new();

        private const string k_TmpFolderPath = "Temp/";

        public int callbackOrder => 0;

        // Maps BuildTarget and RuntimePlatform enums.
        private bool IsPlatformMatching(BuildTarget buildTarget, RuntimePlatform runtimePlatform)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return runtimePlatform == RuntimePlatform.WindowsPlayer;
                case BuildTarget.StandaloneOSX:
                    return runtimePlatform == RuntimePlatform.OSXPlayer;
                case BuildTarget.iOS:
                    return runtimePlatform == RuntimePlatform.IPhonePlayer;
                case BuildTarget.Android:
                    return runtimePlatform == RuntimePlatform.Android;

                default: return false;
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Get all GraphicsStateCollection files in the project.
            string[] collectionGUIDs = AssetDatabase.FindAssets("t:GraphicsStateCollection",
                new[] { "Assets/" + GraphicsStateCollectionManager.CollectionFolderPath });
            GraphicsStateCollection[] collections = new GraphicsStateCollection[collectionGUIDs.Length];
            for (int i = 0; i < collections.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(collectionGUIDs[i]);
                collections[i] = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(path);
            }

            // Make a list of collections files to be stripped.
            m_StrippedFiles.Clear();
            for (int i = 0; i < collections.Length; i++)
            {
                // If the collection file's platform does not match with the current build target.
                if (!IsPlatformMatching(report.summary.platform, collections[i].runtimePlatform))
                {
                    // Add the file's original and new paths to the list.
                    StrippedFile sc = new()
                    {
                        OriginalPath = AssetDatabase.GetAssetPath(collections[i])
                    };
                    string assetFileName = Path.GetFileName(sc.OriginalPath);
                    sc.TempPath = k_TmpFolderPath + assetFileName;
                    m_StrippedFiles.Add(sc);

                    // Also add it's meta file paths to the list.
                    StrippedFile scMeta = new()
                    {
                        OriginalPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(sc.OriginalPath)
                    };
                    string metaFileName = Path.GetFileName(scMeta.OriginalPath);
                    scMeta.TempPath = k_TmpFolderPath + metaFileName;
                    m_StrippedFiles.Add(scMeta);
                }
            }

            // Move the stripped collection files to the Temp folder.
            foreach (StrippedFile sc in m_StrippedFiles)
            {
                File.Move(sc.OriginalPath, sc.TempPath);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Move the stripped collection files back to their original paths.
            foreach (StrippedFile sc in m_StrippedFiles)
            {
                File.Move(sc.TempPath, sc.OriginalPath);
            }
        }
    }
}
#endif