using Chris.Configs;
using Chris.DataDriven;
using Chris.DataDriven.Editor;
using Chris.Schedulers;
using Chris.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chris.Editor
{
    [BaseConfig]
    public class ChrisSettings : ConfigSingleton<ChrisSettings>
    {
        public bool schedulerStackTrace = true;

        public bool initializeDataTableManagerOnLoad;

        public bool validateDataTableBeforeLoad = true;

        public SerializedType<IDataTableEditorSerializer> dataTableEditorSerializer = SerializedType<IDataTableEditorSerializer>.FromType(typeof(DataTableEditorJsonSerializer));

        public bool inlineRowReadOnly;

        public SerializedType<ISerializeFormatter> configSerializer = SerializedType<ISerializeFormatter>.FromType(typeof(TextSerializeFormatter));

        public string password;
        
        internal static void SaveSettings()
        {
            Instance.Save(true);

            ConfigFileLocation location = "Chris";
            var configFile = ConfigSystem.GetProjectConfigFile(location);

            var schedulerSettings = SchedulerConfig.Get();
            schedulerSettings.enableStackTrace = Instance.schedulerStackTrace;
            configFile.SetConfig(SchedulerConfig.Location, schedulerSettings);

            var dataDrivenSettings = DataDrivenConfig.Get();
            dataDrivenSettings.initializeDataTableManagerOnLoad = Instance.initializeDataTableManagerOnLoad;
            dataDrivenSettings.validateDataTableBeforeLoad = Instance.validateDataTableBeforeLoad;
            configFile.SetConfig(DataDrivenConfig.Location, dataDrivenSettings);

            var configsSettings = ConfigsConfig.Get();
            configsSettings.configSerializer = Instance.configSerializer;
            configsSettings.password = Instance.password;
            configFile.SetConfig(ConfigsConfig.Location, configsSettings);

            Serialize(location, configFile);
        }
    }

    internal class ChrisSettingsProvider : SettingsProvider
    {
        private SerializedObject _settingsObject;
        
        private class Styles
        {
            public static readonly GUIContent StackTraceSchedulerLabel = new("Stack Trace",
                "Allow trace scheduled task in editor.");

            public static readonly GUIContent DataTableSerializerLabel = new("Editor Serializer",
                "Set the serializer type in DataTable Editor.");

            public static readonly GUIContent InitializeDataTableManagerOnLoadLabel = new("Initialize Managers",
                "Initialize all DataTableManager instances before the scene loads.");

            public static readonly GUIContent ValidateDataTableBeforeLoadLabel = new("Validate Before Load",
                "Verify the existence of a DataTable before loading it." +
                "Disabling this feature may cause exceptions to be thrown on load, resulting in unexpected behavior. " +
                "Disable only after checking that all DataTables exist.");

            public static readonly GUIContent InlineRowReadOnlyLabel = new("Inline Row ReadOnly",
                "Enable to make the DataTableRow in the inspector list view read-only.");

            public static readonly GUIContent ConfigSerializerLabel = new("Config Serializer",
                "Set the serializer type for user data config files.");
            
            public static readonly GUIContent PasswordLabel = new("Encrypt Password",
                "Set the user data serializer encrypt password.");
        }

        private ChrisSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            if (!ChrisSettings.Instance.dataTableEditorSerializer.IsValid())
            {
                ChrisSettings.Instance.dataTableEditorSerializer =
                    SerializedType<IDataTableEditorSerializer>.FromType(typeof(DataTableEditorJsonSerializer));
                ChrisSettings.SaveSettings();
            }
            if (!ChrisSettings.Instance.configSerializer.IsValid())
            {
                ChrisSettings.Instance.configSerializer = SerializedType<ISerializeFormatter>.FromType(typeof(TextSerializeFormatter));
                ChrisSettings.SaveSettings();
            }
            _settingsObject = new SerializedObject(ChrisSettings.Instance);
        }

        public override void OnDeactivate()
        {
            ChrisSettings.SaveSettings();
        }

        public override void OnGUI(string searchContext)
        {
            DrawSchedulerSettings();
            DrawDataTableSettings();
            DrawConfigSettings();
        }

        private void DrawSchedulerSettings()
        {
            var titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label("Scheduler Settings", titleStyle);
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.schedulerStackTrace)), Styles.StackTraceSchedulerLabel);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                ChrisSettings.SaveSettings();
            }
            GUILayout.EndVertical();
        }

        private void DrawDataTableSettings()
        {
            var titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label("DataTable Settings", titleStyle);
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.initializeDataTableManagerOnLoad)), Styles.InitializeDataTableManagerOnLoadLabel);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.validateDataTableBeforeLoad)), Styles.ValidateDataTableBeforeLoadLabel);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.dataTableEditorSerializer)), Styles.DataTableSerializerLabel);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.inlineRowReadOnly)), Styles.InlineRowReadOnlyLabel);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                if (!ChrisSettings.Instance.dataTableEditorSerializer.IsValid())
                {
                    ChrisSettings.Instance.dataTableEditorSerializer = SerializedType<IDataTableEditorSerializer>.FromType(typeof(DataTableEditorJsonSerializer));
                }
                ChrisSettings.SaveSettings();
            }
            GUILayout.EndVertical();
        }

        private void DrawConfigSettings()
        {
            var titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label("Config Settings", titleStyle);
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.configSerializer)), Styles.ConfigSerializerLabel);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.password)), Styles.PasswordLabel);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                if (!ChrisSettings.Instance.configSerializer.IsValid())
                {
                    ChrisSettings.Instance.configSerializer = SerializedType<ISerializeFormatter>.FromType(typeof(TextSerializeFormatter));
                }
                ChrisSettings.SaveSettings();
            }
            GUILayout.EndVertical();
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new ChrisSettingsProvider("Project/Chris", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<Styles>()
            };
            return provider;
        }
    }
}
