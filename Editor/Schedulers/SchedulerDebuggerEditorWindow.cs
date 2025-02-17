using UnityEngine;
using UnityEditor;
using System.Linq;
using Chris.Editor;
using Unity.CodeEditor;
namespace Chris.Schedulers.Editor
{
    // EditorWindow is modified from R3.Unity
    public class SchedulerDebuggerEditorWindow : EditorWindow
    {
        private SchedulerRunner Manager
        {
            get
            {
                if (Application.isPlaying)
                    return SchedulerRunner.Get();
                else
                    return null;
            }
        }
        private int ManagedScheduledCount => Manager == null ? 0 : Manager.ScheduledItems.Count;
        private int ManagedScheduledCapacity => Manager == null ? 0 : Manager.ScheduledItems.InternalCapacity;
        private static SchedulerDebuggerEditorWindow window;

        [MenuItem("Tools/Chris/Scheduler Debugger")]
        public static void OpenWindow()
        {
            if (window != null)
            {
                window.Close();
            }

            // will called OnEnable(singleton instance will be set).
            GetWindow<SchedulerDebuggerEditorWindow>("Scheduler Debugger").Show();
        }

        private static readonly GUILayoutOption[] EmptyLayoutOption = new GUILayoutOption[0];

        private SchedulerDebuggerTreeView treeView;
        private object splitterState;

        private void OnEnable()
        {
            window = this; // set singleton.
            splitterState = SplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);
            treeView = new SchedulerDebuggerTreeView();
        }
        private void Update()
        {
            treeView.ReloadAndSort();
            Repaint();
        }
        private void OnGUI()
        {
            // Head
            RenderHeadPanel();

            // Splittable
            SplitterGUILayout.BeginVerticalSplit(splitterState, EmptyLayoutOption);
            {
                // Column Tabble
                RenderTable();

                // StackTrace details
                RenderDetailsPanel();
            }
            SplitterGUILayout.EndVerticalSplit();
        }

        #region HeadPanel

        private static readonly GUIContent CancelAllHeadContent = EditorGUIUtility.TrTextContent("Cancel All", "Cancel all scheduled tasks", (Texture)null);

        private void RenderHeadPanel()
        {
            EditorGUILayout.BeginVertical(EmptyLayoutOption);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, EmptyLayoutOption);

            GUILayout.Label($"Managed scheduled task count: {ManagedScheduledCount} capacity: {ManagedScheduledCapacity}");
            GUILayout.FlexibleSpace();

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button(CancelAllHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption))
            {
                Manager.CancelAll();
                treeView.ReloadAndSort();
                Repaint();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TableColumn

        private Vector2 tableScroll;
        private GUIStyle tableListStyle;

        private void RenderTable()
        {
            if (tableListStyle == null)
            {
                tableListStyle = new GUIStyle("CN Box");
                tableListStyle.margin.top = 0;
                tableListStyle.padding.left = 3;
            }

            EditorGUILayout.BeginVertical(tableListStyle, EmptyLayoutOption);

            tableScroll = EditorGUILayout.BeginScrollView(tableScroll, new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.MaxWidth(2000f)
            });
            var controlRect = EditorGUILayout.GetControlRect(new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            });


            treeView?.OnGUI(controlRect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }



        #endregion

        #region Details

        private static GUIStyle detailsStyle;
        private static GUIStyle stackTraceButtonStyle;
        private Vector2 detailsScroll;
        private void RenderDetailsPanel()
        {
            if (detailsStyle == null)
            {
                detailsStyle = new GUIStyle("CN Message")
                {
                    wordWrap = false,
                    stretchHeight = true
                };
                detailsStyle.margin.right = 15;
            }

            stackTraceButtonStyle ??= new(GUI.skin.button)
            {
                wordWrap = true,
                fontSize = 12
            };

            SchedulerDebuggerTreeView.ViewItem viewItem = null;
            var selected = treeView.state.selectedIDs;
            if (selected.Count > 0)
            {
                var first = selected[0];
                if (treeView.CurrentBindingItems.FirstOrDefault(x => x.id == first) is SchedulerDebuggerTreeView.ViewItem item)
                {
                    viewItem = item;
                }
            }
            detailsScroll = EditorGUILayout.BeginScrollView(detailsScroll, EmptyLayoutOption);
            if (viewItem != null)
            {
                if (SchedulerRegistry.TryGetListener(viewItem.ScheduledItem.Value, out var listener))
                {
                    GUILayout.Label($"{listener.fileName} {listener.lineNumber}", detailsStyle);
                    if (GUILayout.Button($"Open in Code Editor", stackTraceButtonStyle))
                    {
                        CodeEditor.Editor.CurrentCodeEditor.OpenProject(listener.fileName, listener.lineNumber);
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Resume", stackTraceButtonStyle))
                    {
                        Manager.Resume(viewItem.ScheduledItem.Value.Handle);
                    }
                    if (GUILayout.Button("Pause", stackTraceButtonStyle))
                    {
                        Manager.Pause(viewItem.ScheduledItem.Value.Handle);
                    }
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("Cancel", stackTraceButtonStyle))
                    {
                        Manager.Cancel(viewItem.ScheduledItem.Value.Handle);
                    }
                }
                else
                {
                    GUILayout.Label($"Enable Stack Trace in ChrisSettings to track all scheduled tasks.");
                }
            }
            EditorGUILayout.EndScrollView();
        }
        #endregion
    }
}

