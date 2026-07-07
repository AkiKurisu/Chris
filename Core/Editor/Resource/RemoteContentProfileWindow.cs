using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Chris.Resource.Editor
{
    public sealed class RemoteContentProfileWindow : EditorWindow
    {
        private RemoteContentProfile _profile;

        private UnityEditor.Editor _profileEditor;

        private Vector2 _scroll;

        private static string ProfileGuidKey => Application.productName + "_RemoteContentProfileGUID";

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
                    CreateProfileEditor();
                }
            }
        }

        private void OnDisable()
        {
            if (_profileEditor)
            {
                DestroyImmediate(_profileEditor);
            }

            _profileEditor = null;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawProfileSelector();

            if (!_profile)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Create Profile", GUILayout.Height(30)))
                {
                    CreateProfile();
                }

                return;
            }

            EditorGUILayout.Space();
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scrollScope.scrollPosition;
                if (!_profileEditor)
                {
                    CreateProfileEditor();
                }
                EditorGUILayout.BeginVertical(GUI.skin.box);
                _profileEditor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
            }

            DrawActionButtons();
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

        private void DrawActionButtons()
        {
            EditorGUILayout.Space();

            bool isValid = _profile && _profile.ValidateProfile().IsValid;
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!isValid))
            {
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(253 / 255f, 163 / 255f, 255 / 255f);
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
            if (_profileEditor)
            {
                DestroyImmediate(_profileEditor);
                _profileEditor = null;
            }

            if (_profile)
            {
                CreateProfileEditor();
                EditorPrefs.SetString(ProfileGuidKey, GetGuid(_profile));
            }

            Repaint();
        }

        private void CreateProfileEditor()
        {
            if (!_profile)
            {
                return;
            }

            _profileEditor = UnityEditor.Editor.CreateEditor(_profile);
            if (_profileEditor is RemoteContentProfileEditor profileEditor)
            {
                profileEditor.SetHostedInWindow(true);
            }
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
