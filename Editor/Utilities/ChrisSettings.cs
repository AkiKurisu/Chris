using Chris.DataDriven.Editor;
using Chris.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Chris.Editor
{
    [FilePath("ProjectSettings/ChrisSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ChrisSettings : ScriptableSingleton<ChrisSettings>
    {
        public bool SchdulerStackTrace = true;
        
        public SerializedType<IDataTableJsonSerializer> DataTableJsonSerializer = SerializedType<IDataTableJsonSerializer>.FromType(typeof(DataTableJsonSerializer));
        
        public bool InitializeDataTableManagerOnLoad = false;
        
        public bool InlineRowReadOnly = false;

        public static void SaveSettings()
        {
            instance.Save(true);
        }
    }

    internal class ChrisSettingsProvider : SettingsProvider
    {
        private SerializedObject _settingsObject;
        private class Styles
        {
            public static GUIContent s_StackTraceScheduler = new("Stack Trace", "Allow trace scheduled task in editor");
            
            public static GUIContent s_DataTableJsonSerializer = new("Json Serializer", "Set DataTable json serializer type");
            
            public static GUIContent s_InitializeDataTableManagerOnLoad = new("Initialize Managers", "Initialize all DataManager instances before scene loaded");
            
            public static GUIContent s_InlineRowReadOnly = new("Inline Row ReadOnly", "Enable to let DataTableRow in inspector list view readonly");
        }
        public ChrisSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }
        
        private const string StackTraceSchedulerDisableSymbol = "AF_SCHEDULER_STACK_TRACE_DISABLE";
        
        private const string InitializeDataTableManagerOnLoadSymbol = "AF_INITIALIZE_DATATABLE_MANAGER_ON_LOAD";
        
        private ChrisSettings _settings;
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            if(!ChrisSettings.instance.DataTableJsonSerializer.IsValid())
            {
                ChrisSettings.instance.DataTableJsonSerializer =
                    SerializedType<IDataTableJsonSerializer>.FromType(typeof(DataTableJsonSerializer));
                ChrisSettings.SaveSettings();
            }  
            _settingsObject = new SerializedObject(_settings = ChrisSettings.instance);
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
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.SchdulerStackTrace)), Styles.s_StackTraceScheduler);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                if (_settings.SchdulerStackTrace)
                    ScriptingSymbol.RemoveScriptingSymbol(StackTraceSchedulerDisableSymbol);
                else
                    ScriptingSymbol.AddScriptingSymbol(StackTraceSchedulerDisableSymbol);
                ChrisSettings.SaveSettings();
            }
            GUILayout.EndVertical();
        }
        private void DrawDataTableSettings()
        {
            GUILayout.BeginVertical("DataTable Settings", GUI.skin.box);
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.DataTableJsonSerializer)), Styles.s_DataTableJsonSerializer);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.InitializeDataTableManagerOnLoad)), Styles.s_InitializeDataTableManagerOnLoad);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisSettings.InlineRowReadOnly)), Styles.s_InlineRowReadOnly);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                if (!ChrisSettings.instance.DataTableJsonSerializer.IsValid())
                {
                    ChrisSettings.instance.DataTableJsonSerializer = SerializedType<IDataTableJsonSerializer>.FromType(typeof(DataTableJsonSerializer));
                }
                if (_settings.InitializeDataTableManagerOnLoad)
                    ScriptingSymbol.AddScriptingSymbol(InitializeDataTableManagerOnLoadSymbol);
                else
                    ScriptingSymbol.RemoveScriptingSymbol(InitializeDataTableManagerOnLoadSymbol);
                ChrisSettings.SaveSettings();
            }
            GUILayout.EndVertical();
        }
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new ChrisSettingsProvider("Project/Chris Settings", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<Styles>()
            };
            return provider;
        }
    }
}
