using Chris.Configs;
using Chris.Configs.Editor;
using Chris.DataDriven;
using Chris.DataDriven.Editor;
using Chris.Schedulers;
using Chris.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chris.Editor
{
    [FilePath("ProjectSettings/ChrisSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ChrisSettings : ScriptableSingleton<ChrisSettings>
    {
        public bool schedulerStackTrace = true;
        
        public bool initializeDataTableManagerOnLoad;
        
        public bool validateDataTableBeforeLoad = true;
        
        public SerializedType<IDataTableEditorSerializer> dataTableEditorSerializer = SerializedType<IDataTableEditorSerializer>.FromType(typeof(DataTableEditorJsonSerializer));
        
        public bool inlineRowReadOnly;

        internal static void SaveSettings()
        {
            instance.Save(true);
            
            var serializer = ConfigsEditorUtils.GetConfigSerializer();
            ConfigFileLocation location = "Chris";
            var configFile = ConfigSystem.GetConfigFile(location);
            
            var schedulerSettings = SchedulerConfig.Get();
            schedulerSettings.enableStackTrace = instance.schedulerStackTrace;
            configFile.SetConfig(SchedulerConfig.Location, schedulerSettings);
            
            var dataDrivenSettings = DataDrivenConfig.Get();
            dataDrivenSettings.initializeDataTableManagerOnLoad = instance.initializeDataTableManagerOnLoad;
            dataDrivenSettings.validateDataTableBeforeLoad = instance.validateDataTableBeforeLoad;
            configFile.SetConfig(DataDrivenConfig.Location, dataDrivenSettings);
            
            serializer.Serialize(location, configFile);
        }
    }

    internal class ChrisSettingsProvider : SettingsProvider
    {
        private SerializedObject _settingsObject;
        
        private class Styles
        {
            public static readonly GUIContent StackTraceSchedulerLabel = new("Stack Trace", "Allow trace scheduled task in editor.");
            
            public static readonly GUIContent DataTableSerializerLabel = new("Editor Serializer", "Set DataTable Editor serializer type.");
            
            public static readonly GUIContent InitializeDataTableManagerOnLoadLabel = new("Initialize Managers", "Initialize all DataTableManager instances before scene loaded.");
            
            public static readonly GUIContent ValidateDataTableBeforeLoadLabel = new("Validate Before Load", "Verify that the DataTable exists before loading it.");

            public static readonly GUIContent InlineRowReadOnlyLabel = new("Inline Row ReadOnly", "Enable to let DataTableRow in inspector list view readonly.");
        }
        
        public ChrisSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            if(!ChrisSettings.instance.dataTableEditorSerializer.IsValid())
            {
                ChrisSettings.instance.dataTableEditorSerializer =
                    SerializedType<IDataTableEditorSerializer>.FromType(typeof(DataTableEditorJsonSerializer));
                ChrisSettings.SaveSettings();
            }  
            _settingsObject = new SerializedObject(ChrisSettings.instance);
        }
        
        public override void OnGUI(string searchContext)
        {
            DrawSchedulerSettings();
            DrawDataTableSettings();
        }
        
        private void DrawSchedulerSettings()
        {
            GUILayout.BeginVertical("Scheduler Settings", GUI.skin.box);
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.schedulerStackTrace)), Styles.StackTraceSchedulerLabel);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                ChrisSettings.SaveSettings();
            }
            GUILayout.EndVertical();
        }
        
        private void DrawDataTableSettings()
        {
            GUILayout.BeginVertical("DataTable Settings", GUI.skin.box);
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.initializeDataTableManagerOnLoad)), Styles.InitializeDataTableManagerOnLoadLabel);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.validateDataTableBeforeLoad)), Styles.ValidateDataTableBeforeLoadLabel);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.dataTableEditorSerializer)), Styles.DataTableSerializerLabel);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.inlineRowReadOnly)), Styles.InlineRowReadOnlyLabel);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                if (!ChrisSettings.instance.dataTableEditorSerializer.IsValid())
                {
                    ChrisSettings.instance.dataTableEditorSerializer = SerializedType<IDataTableEditorSerializer>.FromType(typeof(DataTableEditorJsonSerializer));
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
