using System;
using System.IO;
using Chris.Editor;
using Chris.Serialization;
using Chris.Serialization.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UEditor = UnityEditor.Editor;

namespace Chris.DataDriven.Editor
{
    [CustomEditor(typeof(DataTable))]
    public class DataTableEditor : UEditor
    {
        public DataTable Table => target as DataTable;
        
        private DataTableRowView _dataTableRowView;
        
        private const string NullType = "Null";
        
        /// <summary>
        /// Subscribe to add custom left toolbar
        /// </summary>
        public DrawToolBarDelegate OnDrawLeftTooBar;
        
        /// <summary>
        /// Subscribe to add custom right toolbar
        /// </summary>
        public DrawToolBarDelegate OnDrawRightTooBar;
        
        public DataTableRowView GetDataTableRowView()
        {
            _dataTableRowView ??= CreateDataTableRowView(Table);
            return _dataTableRowView;
        }
        
        /// <summary>
        /// Implement to use customized <see cref="DataTableRowView"/>  
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected virtual DataTableRowView CreateDataTableRowView(DataTable table)
        {
            var rowView = new DataTableRowView(table);
            if (ChrisSettings.instance.inlineRowReadOnly)
            {
                rowView.ReadOnly = true;
            }
            return rowView;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultTitle();
            DrawToolBar();
            GUILayout.Space(10);
            DrawRowView();
        }
        
        protected void DrawDefaultTitle()
        {
            GUILayout.Label("DataTable", new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            });
        }
        
        protected virtual void DrawRowView()
        {
            _dataTableRowView = GetDataTableRowView();
            DrawRowTypeSelector();
            GUILayout.Space(5);
            _dataTableRowView.DrawGUI(serializedObject);
        }

        private void DrawRowTypeSelector()
        {
            var typeProp = serializedObject.FindProperty("m_rowType");
            var reference = typeProp.FindPropertyRelative("serializedTypeString");
            Type type;
            try
            {
                type = SerializedType.FromString(reference.stringValue);
            }
            catch
            {
                type = null;
            }
            string id = type != null ? type.Name : NullType;
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Row Type", "Set DataTable Row Struct Type"), GUILayout.Width(80));
            if (EditorGUILayout.DropdownButton(new GUIContent(id), FocusType.Keyboard))
            {
                var provider = CreateInstance<TypeSearchWindow>();
                provider.Initialize(typeof(IDataTableRow), selectType =>
                {
                    reference.stringValue = selectType != null ? SerializedType.ToString(selectType) : NullType;
                    serializedObject.ApplyModifiedProperties();
                    RequestDataTableUpdate();
                });
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
            }
            GUILayout.EndHorizontal();
        }
        
        #region Cleanup

        // DataTableEditor use global object manager to cache wrapper.
        // However, the soft object handle is not persistent.
        // We should ensure not to conflict with other modules.


        protected virtual void OnEnable()
        {
            GlobalObjectManager.Cleanup();
            Undo.undoRedoEvent += OnUndo;
        }

        protected virtual void OnDisable()
        {
            GlobalObjectManager.Cleanup();
            Undo.undoRedoEvent -= OnUndo;
            Table.Cleanup();
            /* Trigger save assets to force cleanup editor cache */
            EditorUtility.SetDirty(Table);
            /* Auto register table if it has AddressableDataTableAttribute */
            DataTableEditorUtils.RegisterTableToAssetGroup(Table);
        }

        #endregion
        
        /// <summary>
        /// Draw editor toolbar
        /// </summary>
        protected virtual void DrawToolBar()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export to Json", DataTableEditorUtils.ToolBarButtonStyle))
            {
                var jsonData = DataTableEditorUtils.ExportJson(Table);
                string path = EditorUtility.SaveFilePanel("Select json file export path", Application.dataPath, Table.name, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, jsonData);
                    Debug.Log($"<color=#3aff48>DataTable</color>: Save to json file succeed!");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            OnDrawLeftTooBar?.Invoke(this);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Import from Json", DataTableEditorUtils.ToolBarButtonStyle))
            {
                string path = EditorUtility.OpenFilePanel("Select json file to import", Application.dataPath, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    var data = File.ReadAllText(path);
                    DataTableEditorUtils.ImportJson(Table, data);
                    RequestDataTableUpdate();
                    EditorUtility.SetDirty(Table);
                    AssetDatabase.SaveAssets();
                    GUIUtility.ExitGUI();
                }
            }
            OnDrawRightTooBar?.Invoke(this);
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Request change DataTable in editor
        /// </summary>
        protected void RequestDataTableUpdate()
        {
            DataTableEditorUtils.OnDataTablePreUpdate?.Invoke(Table);
            RebuildEditorView();
            DataTableEditorUtils.OnDataTablePostUpdate?.Invoke(Table);
        }
        
        /// <summary>
        /// Rebuild editor gui view, called on DataTable changed
        /// </summary>
        protected virtual void RebuildEditorView()
        {
            _dataTableRowView?.Rebuild();
            serializedObject.Update();
        }
        
        protected virtual void OnUndo(in UndoRedoInfo undo)
        {
            // Manually rebuild row view after undo
            RebuildEditorView();
        }
    }
}