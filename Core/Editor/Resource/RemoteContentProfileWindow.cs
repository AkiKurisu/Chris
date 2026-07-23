using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Chris.Resource.Editor
{
    public sealed class RemoteContentProfileWindow : EditorWindow
    {
        private const int MaxPreviewEntries = 15;

        private const float MinAssetGroupsHeight = 180f;

        private const float DefaultFooterHeight = 100f;

        private RemoteContentProfile _profile;

        private RemoteContentProfileEditor _profileEditor;

        private SerializedObject _serializedProfile;

        private SerializedProperty _packageNameProp;

        private SerializedProperty _versionProp;

        private SerializedProperty _outputRootProp;

        private SerializedProperty _zipOutputProp;

        private readonly AdvancedDropdownState _groupDropdownState = new();

        private readonly HashSet<string> _expandedGroups = new();

        private RemoteContentGroupDropdown _groupDropdown;

        private AddressableAssetGroup _pendingRemoveGroup;

        private Vector2 _assetGroupsScroll;

        private float _assetGroupsTop = 290f;

        private float _footerHeight = DefaultFooterHeight;

        private static string ProfileGuidKey => Application.productName + "_RemoteContentProfileGUID";

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

        [MenuItem("Tools/Chris/Resource/Remote Content Builder")]
        public static void Open()
        {
            var window = GetWindow<RemoteContentProfileWindow>("Remote Content");
            window.minSize = new Vector2(560, 640);
        }

        public static void Open(RemoteContentProfile profile)
        {
            var window = GetWindow<RemoteContentProfileWindow>("Remote Content");
            window.minSize = new Vector2(560, 640);
            window.SetProfile(profile);
        }

        private void OnEnable()
        {
            if (!_profile)
            {
                _profile = LoadRememberedProfile();
                if (_profile)
                {
                    CreateProfileState();
                }
            }
        }

        private void OnDisable()
        {
            DestroyProfileEditor();
            _serializedProfile = null;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0f, 0f, position.width, position.height));
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            DrawHeader();
            DrawProfileSelector();

            if (!_profile)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Create Profile", GUILayout.Height(30)))
                {
                    CreateProfile();
                }

                EditorGUILayout.EndVertical();
                GUILayout.EndArea();
                return;
            }

            EnsureProfileState();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
            _serializedProfile.Update();
            DrawPackageInfo();
            if (Event.current.type == EventType.Repaint)
            {
                _assetGroupsTop = GUILayoutUtility.GetLastRect().yMax + 5f;
            }

            float assetGroupsHeight = Mathf.Max(MinAssetGroupsHeight, position.height - _assetGroupsTop - _footerHeight);
            DrawAssetGroups(assetGroupsHeight);
            if (_serializedProfile.ApplyModifiedProperties())
            {
                SaveProfileAsset();
            }

            float footerTop = GUILayoutUtility.GetLastRect().yMax;
            DrawValidation(_profile);
            DrawProfileExtraActions();
            EditorGUILayout.EndVertical();

            DrawActionButtons();
            if (Event.current.type == EventType.Repaint)
            {
                _footerHeight = Mathf.Max(DefaultFooterHeight, GUILayoutUtility.GetLastRect().yMax - footerTop + 5f);
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private static void DrawHeader()
        {
            EditorGUILayout.HelpBox($"Remote Content Builder\nCurrent Platform: {EditorUserBuildSettings.activeBuildTarget}", MessageType.Info);
            EditorGUILayout.Space(5);
        }

        private void DrawProfileSelector()
        {
            EditorGUILayout.BeginHorizontal();

            var selected = (RemoteContentProfile)EditorGUILayout.ObjectField("Profile", _profile, typeof(RemoteContentProfile), false);
            if (selected != _profile)
            {
                SetProfile(selected);
            }

            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("Select Remote Content Profile", Application.dataPath, "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    string dataPath = Application.dataPath.Replace('\\', '/');
                    string picked = path.Replace('\\', '/');
                    if (picked.StartsWith(dataPath))
                    {
                        string assetPath = "Assets" + picked.Substring(dataPath.Length);
                        var profile = AssetDatabase.LoadAssetAtPath<RemoteContentProfile>(assetPath);
                        if (profile)
                        {
                            SetProfile(profile);
                        }
                        else
                        {
                            ShowNotification(new GUIContent($"Invalid profile: {assetPath}"));
                        }
                    }
                    else
                    {
                        ShowNotification(new GUIContent("Profile must be inside the project's Assets folder."));
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
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

        private void DrawAssetGroups(float height)
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(height), GUILayout.ExpandWidth(true));

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Asset Groups", Styles.HeaderStyle);
            GUILayout.FlexibleSpace();

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Styles.AddButtonColor;
            var addRect = GUILayoutUtility.GetRect(new GUIContent("Add Group  \u25be"), GUI.skin.button, GUILayout.Width(120), GUILayout.Height(20));
            bool addGroupClicked = EditorGUI.DropdownButton(addRect, new GUIContent("Add Group", "Pick an Addressable group to add"), FocusType.Passive, GUI.skin.button);
            GUI.backgroundColor = originalColor;
            if (addGroupClicked)
            {
                ShowAddGroupDropdown(addRect);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            float boxHeight = Mathf.Max(120f, height - 30f);
            EditorGUILayout.BeginVertical(Styles.BoxStyle, GUILayout.Height(boxHeight), GUILayout.ExpandWidth(true));

            var groups = _profile.AssetGroups;
            float scrollHeight = Mathf.Max(70f, boxHeight - 36f);
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_assetGroupsScroll, GUILayout.Height(scrollHeight)))
            {
                _assetGroupsScroll = scrollScope.scrollPosition;
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
            }

            if (_pendingRemoveGroup)
            {
                Undo.RecordObject(_profile, "Remove Group");
                _expandedGroups.Remove(_pendingRemoveGroup.Guid);
                _profile.RemoveGroup(_pendingRemoveGroup);
                _pendingRemoveGroup = null;
                SaveProfileAsset();
                _serializedProfile.Update();
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
                    SaveProfileAsset();
                    _serializedProfile.Update();
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
                        SaveProfileAsset();
                        _serializedProfile.Update();
                        Repaint();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
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
            if (available.Count == 0)
            {
                ShowNotification(new GUIContent("No available Addressable groups."));
                return;
            }

            _groupDropdown = new RemoteContentGroupDropdown(_groupDropdownState, available, AddGroupToProfile);
            _groupDropdown.Show(rect);
        }

        private void AddGroupToProfile(AddressableAssetGroup group)
        {
            if (!group || !_profile)
            {
                return;
            }

            Undo.RecordObject(_profile, "Add Group");
            _profile.AddGroups(new[] { group });
            _expandedGroups.Add(group.Guid);
            SaveProfileAsset();
            _serializedProfile.Update();
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

        private void DrawProfileExtraActions()
        {
            if (!_profileEditor)
            {
                CreateProfileEditor();
            }

            if (!_profileEditor)
            {
                return;
            }

            _profileEditor.DrawExtraActionsInWindow(_profile);
            if (EditorUtility.IsDirty(_profile))
            {
                AssetDatabase.SaveAssetIfDirty(_profile);
                _serializedProfile.Update();
                Repaint();
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space();

            bool isValid = _profile && _profile.ValidateProfile().IsValid;
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!isValid))
            {
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = Styles.BuildButtonColor;
                if (GUILayout.Button(new GUIContent("Build", "Build remote content for the active platform"), GUILayout.Height(35)))
                {
                    RemoteContentProfileEditor.RunBuild(_profile);
                }
                GUI.backgroundColor = originalColor;
            }

            using (new EditorGUI.DisabledScope(!_profile))
            {
                if (GUILayout.Button(new GUIContent("Open Output Folder", "Open the resolved output directory"),
                        GUILayout.Height(35), GUILayout.Width(160)))
                {
                    OpenOutputFolder();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OpenOutputFolder()
        {
            string path = _profile.GetOutputRootFullPath();
            Directory.CreateDirectory(path);
            Process.Start(path);
        }

        private void SetProfile(RemoteContentProfile profile)
        {
            _profile = profile;
            _assetGroupsScroll = Vector2.zero;
            _expandedGroups.Clear();
            DestroyProfileEditor();
            _serializedProfile = null;

            if (_profile)
            {
                CreateProfileState();
                EditorPrefs.SetString(ProfileGuidKey, GetGuid(_profile));
            }

            Repaint();
        }

        private void EnsureProfileState()
        {
            if (_serializedProfile == null || _serializedProfile.targetObject != _profile)
            {
                CreateProfileState();
            }
        }

        private void CreateProfileState()
        {
            if (!_profile)
            {
                return;
            }

            _serializedProfile = new SerializedObject(_profile);
            _packageNameProp = _serializedProfile.FindProperty("packageName");
            _versionProp = _serializedProfile.FindProperty("version");
            _outputRootProp = _serializedProfile.FindProperty("outputRoot");
            _zipOutputProp = _serializedProfile.FindProperty("zipOutput");
            CreateProfileEditor();
        }

        private void CreateProfileEditor()
        {
            DestroyProfileEditor();
            if (!_profile)
            {
                return;
            }

            _profileEditor = UnityEditor.Editor.CreateEditor(_profile) as RemoteContentProfileEditor;
        }

        private void DestroyProfileEditor()
        {
            if (_profileEditor)
            {
                DestroyImmediate(_profileEditor);
            }

            _profileEditor = null;
        }

        private void SaveProfileAsset()
        {
            if (!_profile)
            {
                return;
            }

            EditorUtility.SetDirty(_profile);
            AssetDatabase.SaveAssetIfDirty(_profile);
        }

        private void CreateProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Remote Content Profile",
                "RemoteContentProfile",
                "asset",
                "Choose where to save the remote content profile.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var profile = CreateInstance<RemoteContentProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            SetProfile(profile);
        }

        private static RemoteContentProfile LoadRememberedProfile()
        {
            string guid = EditorPrefs.GetString(ProfileGuidKey, null);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<RemoteContentProfile>(path);
        }

        private static string GetGuid(Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
    }
}
