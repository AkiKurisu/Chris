using UnityEditor;
using UnityEngine;

namespace Chris.Resource.Editor
{
    [CustomEditor(typeof(RemoteContentProfile), true)]
    public class RemoteContentProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Edit Profile", GUILayout.Height(30)))
            {
                RemoteContentProfileWindow.Open((RemoteContentProfile)target);
            }
        }

        protected virtual void DrawExtraActions(RemoteContentProfile profile)
        {
        }

        internal void DrawExtraActionsInWindow(RemoteContentProfile profile)
        {
            DrawExtraActions(profile);
        }

        internal static void RunBuild(RemoteContentProfile profile)
        {
            var result = ResourceExporter.Export(profile);
            if (result.Succeeded)
            {
                EditorUtility.RevealInFinder(string.IsNullOrEmpty(result.ZipPath) ? result.BuildPath : result.ZipPath);
            }
            else
            {
                Debug.LogError($"Remote content build failed: {result.Error}");
            }
        }
    }
}
