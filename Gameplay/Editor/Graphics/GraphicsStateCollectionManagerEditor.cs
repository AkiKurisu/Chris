#if UNITY_6000_3_OR_NEWER
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UEditor = UnityEditor.Editor;

namespace Chris.Gameplay.Graphics.Editor
{
    [CustomEditor(typeof(GraphicsStateCollectionManager))]
    public class GraphicsStateCollectionManagerEditor : UEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var manager = (GraphicsStateCollectionManager)target;
            var buttonContent = new GUIContent(" Refresh Collection List", EditorGUIUtility.IconContent("Refresh").image);
            var buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = 30,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 5, 5)
            };
            
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            
            if (GUILayout.Button(buttonContent, buttonStyle))
            {
                UpdateCollectionList(manager);
                int count = manager.collections?.Length ?? 0;
                EditorUtility.DisplayDialog("Collection List Updated", $"Found {count} GraphicsStateCollection(s) in the collection folder.", "OK");
            }
            
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Click the button above to refresh and update the collection list from the project folder.", MessageType.Info);
        }

        private static void UpdateCollectionList(GraphicsStateCollectionManager manager)
        {
            string[] collectionGUIDs = AssetDatabase.FindAssets("t:GraphicsStateCollection",
                new[] { "Assets/" + GraphicsStateCollectionManager.CollectionFolderPath });
            manager.collections = new GraphicsStateCollection[collectionGUIDs.Length];
            for (int i = 0; i < manager.collections.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(collectionGUIDs[i]);
                manager.collections[i] = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(path);
            }

            EditorUtility.SetDirty(manager);
        }
    }
}
#endif