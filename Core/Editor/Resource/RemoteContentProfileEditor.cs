using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Chris.Resource.Editor
{
    [CustomEditor(typeof(RemoteContentProfile), true)]
    public class RemoteContentProfileEditor : UnityEditor.Editor
    {
        private const int MaxPreviewEntries = 15;

        private RemoteContentProfile _profile;

        private SerializedProperty _packageNameProp;

        private SerializedProperty _versionProp;

        private SerializedProperty _outputRootProp;

        private SerializedProperty _zipOutputProp;

        private readonly HashSet<string> _expandedGroups = new();

        private AddressableAssetGroup _pendingRemoveGroup;

        private bool _hostedInWindow;

        private static class Styles
        {
            public static readonly GUIStyle HeaderStyle;

            public static readonly GUIStyle BoxStyle;

            public static readonly GUIStyle InfoLabelStyle;

            public static readonly GUIStyle CardHeaderStyle;

            public static readonly Color AddButtonColor = new(0.7f, 0.9f, 1f);

            public static readonly Color BuildButtonColor = new(253 / 255f, 163 / 255f, 255 / 255f);

            public static readonly Color RemoveButtonColor = new(1f, 0.5f, 0.5f);

            static Styles()
            {
                HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 10, 5)
                };

                BoxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                };

                InfoLabelStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                {
                    padding = new RectOffset(5, 5, 2, 2)
                };

                CardHeaderStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private void OnEnable()
        {
            _profile = (RemoteContentProfile)target;
            _packageNameProp = serializedObject.FindProperty("packageName");
            _versionProp = serializedObject.FindProperty("version");
            _outputRootProp = serializedObject.FindProperty("outputRoot");
            _zipOutputProp = serializedObject.FindProperty("zipOutput");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPackageInfo();
            DrawAssetGroups();

            serializedObject.ApplyModifiedProperties();

            DrawValidation(_profile);
            DrawExtraActions(_profile);
            if (!_hostedInWindow)
            {
                DrawBuildActions(_profile);
            }
        }

        protected virtual void DrawExtraActions(RemoteContentProfile profile)
        {
        }

        /// <summary>
        /// When hosted inside <see cref="RemoteContentProfileWindow"/>, the window owns the
        /// build actions, so the inspector-level Open Builder / Build row is suppressed.
        /// </summary>
        internal void SetHostedInWindow(bool hosted)
        {
            _hostedInWindow = hosted;
        }

        private void DrawPackageInfo()
        {
            GUILayout.Label("Package Info", Styles.HeaderStyle);

            EditorGUILayout.BeginVertical(Styles.BoxStyle);

            EditorGUILayout.PropertyField(_packageNameProp, new GUIContent("Package Name"));
            EditorGUILayout.PropertyField(_versionProp, new GUIContent("Version"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_outputRootProp, new GUIContent("Output Root"));
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                BrowseOutputRoot();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_zipOutputProp, new GUIContent("Zip Output"));

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Resolved Output", _profile.GetOutputRootFullPath(), Styles.InfoLabelStyle);

            EditorGUILayout.EndVertical();
        }

        private void BrowseOutputRoot()
        {
            string current = _profile.GetOutputRootFullPath();
            string startDir = Directory.Exists(current) ? current : Path.GetDirectoryName(Application.dataPath);
            string selected = EditorUtility.OpenFolderPanel("Select Output Root", startDir, string.Empty);
            if (string.IsNullOrEmpty(selected))
            {
                return;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string normalizedSelected = selected.Replace('\\', '/');
            string normalizedRoot = projectRoot!.Replace('\\', '/');
            if (normalizedSelected.StartsWith(normalizedRoot + "/"))
            {
                _outputRootProp.stringValue = normalizedSelected.Substring(normalizedRoot.Length + 1);
            }
            else
            {
                _outputRootProp.stringValue = normalizedSelected;
            }
        }

        private void DrawAssetGroups()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Asset Groups", Styles.HeaderStyle);
            GUILayout.FlexibleSpace();

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Styles.AddButtonColor;
            var addRect = GUILayoutUtility.GetRect(new GUIContent("Add Group  \u25be"), GUI.skin.button, GUILayout.Width(120), GUILayout.Height(20));
            if (GUI.Button(addRect, new GUIContent("Add Group  \u25be", "Pick an Addressable group to add")))
            {
                ShowAddGroupDropdown(addRect);
            }
            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(Styles.BoxStyle);

            var groups = _profile.AssetGroups;
            if (groups.Count == 0)
            {
                EditorGUILayout.LabelField("No Addressable groups added. Use \"Add Group\" to select one.", EditorStyles.wordWrappedLabel);
            }
            else
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    DrawGroupCard(groups[i]);
                }
            }

            if (_pendingRemoveGroup)
            {
                Undo.RecordObject(_profile, "Remove Group");
                _expandedGroups.Remove(_pendingRemoveGroup.Guid);
                _profile.RemoveGroup(_pendingRemoveGroup);
                _pendingRemoveGroup = null;
                EditorUtility.SetDirty(_profile);
                serializedObject.Update();
                Repaint();
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(groups.Count == 0))
            {
                if (GUILayout.Button("Remove Missing", GUILayout.Width(120)))
                {
                    Undo.RecordObject(_profile, "Remove Missing Groups");
                    _profile.RemoveMissingGroups();
                    EditorUtility.SetDirty(_profile);
                    serializedObject.Update();
                    Repaint();
                }

                if (GUILayout.Button("Clear All", GUILayout.Width(90)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Groups",
                            "Remove all Addressable groups from this profile?", "Clear", "Cancel"))
                    {
                        Undo.RecordObject(_profile, "Clear Groups");
                        _profile.ClearGroups();
                        _expandedGroups.Clear();
                        EditorUtility.SetDirty(_profile);
                        serializedObject.Update();
                        Repaint();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawGroupCard(AddressableAssetGroup group)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            if (!group)
            {
                EditorGUILayout.LabelField("<Missing Group>", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            string key = group.Guid;
            bool expanded = _expandedGroups.Contains(key);
            bool newExpanded = EditorGUILayout.Foldout(expanded, group.Name, true, Styles.CardHeaderStyle);
            if (newExpanded != expanded)
            {
                if (newExpanded)
                {
                    _expandedGroups.Add(key);
                }
                else
                {
                    _expandedGroups.Remove(key);
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                Selection.activeObject = group;
                EditorGUIUtility.PingObject(group);
            }

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Styles.RemoveButtonColor;
            if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(65)))
            {
                _pendingRemoveGroup = group;
            }
            GUI.backgroundColor = originalColor;

            EditorGUILayout.EndHorizontal();

            bool hasSchema = group.HasSchema<BundledAssetGroupSchema>();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Entries: {group.entries.Count}", Styles.InfoLabelStyle, GUILayout.Width(100));
            if (hasSchema)
            {
                EditorGUILayout.LabelField("Bundled Schema \u2713", Styles.InfoLabelStyle);
            }
            else
            {
                var warnStyle = new GUIStyle(Styles.InfoLabelStyle) { normal = { textColor = new Color(1f, 0.6f, 0.2f) } };
                EditorGUILayout.LabelField("Missing BundledAssetGroupSchema", warnStyle);
            }
            EditorGUILayout.EndHorizontal();

            if (_expandedGroups.Contains(key))
            {
                DrawGroupEntries(group);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawGroupEntries(AddressableAssetGroup group)
        {
            EditorGUI.indentLevel++;
            var entries = group.entries.ToList();
            if (entries.Count == 0)
            {
                EditorGUILayout.LabelField("No direct entries.", Styles.InfoLabelStyle);
            }
            else
            {
                int count = Mathf.Min(entries.Count, MaxPreviewEntries);
                for (int i = 0; i < count; i++)
                {
                    EditorGUILayout.LabelField($"\u2022 {entries[i].address}", Styles.InfoLabelStyle);
                }

                if (entries.Count > MaxPreviewEntries)
                {
                    EditorGUILayout.LabelField($"... and {entries.Count - MaxPreviewEntries} more", Styles.InfoLabelStyle);
                }
            }
            EditorGUI.indentLevel--;
        }

        private void ShowAddGroupDropdown(Rect rect)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                EditorUtility.DisplayDialog("Addressables Missing",
                    "AddressableAssetSettings is missing. Create it from the Addressables window first.", "OK");
                return;
            }

            var existing = new HashSet<string>(_profile.AssetGroups.Where(group => group).Select(group => group.Guid));
            var available = settings.groups
                .Where(group => group && !group.ReadOnly && !existing.Contains(group.Guid))
                .ToList();

            var dropdown = new RemoteContentGroupDropdown(available, OnGroupPicked);
            dropdown.Show(rect);
        }

        private void OnGroupPicked(AddressableAssetGroup group)
        {
            if (!group)
            {
                return;
            }

            Undo.RecordObject(_profile, "Add Group");
            _profile.AddGroups(new[] { group });
            EditorUtility.SetDirty(_profile);
            serializedObject.Update();
            Repaint();
        }

        private static void DrawValidation(RemoteContentProfile profile)
        {
            var validation = profile.ValidateProfile();
            foreach (string warning in validation.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            foreach (string error in validation.Errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            if (validation.IsValid && validation.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("Profile is valid and ready to build.", MessageType.Info);
            }
        }

        private static void DrawBuildActions(RemoteContentProfile profile)
        {
            EditorGUILayout.Space();

            bool isValid = profile.ValidateProfile().IsValid;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Builder", GUILayout.Height(30)))
                {
                    RemoteContentProfileWindow.Open(profile);
                }

                using (new EditorGUI.DisabledScope(!isValid))
                {
                    var originalColor = GUI.backgroundColor;
                    GUI.backgroundColor = Styles.BuildButtonColor;
                    if (GUILayout.Button("Build", GUILayout.Height(30)))
                    {
                        RunBuild(profile);
                    }
                    GUI.backgroundColor = originalColor;
                }
            }
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
