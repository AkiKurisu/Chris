using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Chris.Editor
{
    /// <summary>
    ///   <para>Generic class for storing Editor Config.</para>
    /// </summary>
    public class ConfigSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    CreateAndLoad();
                return _instance;
            }
        }

        protected ConfigSingleton()
        {
            if (_instance != null)
            {
                Debug.LogError("ConfigSingleton already exists. Did you query the singleton in a constructor?");
            }
            else
            {
                _instance = (object)this as T;
            }
        }

        private static void CreateAndLoad()
        {
            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
                InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            if (!(_instance == null))
                return;
            CreateInstance<T>().hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
        }

        protected virtual void Save(bool saveAsText)
        {
            if (_instance == null)
            {
                Debug.LogError("Cannot save ScriptableSingleton: no instance!");
            }
            else
            {
                string filePath = GetFilePath();
                if (!string.IsNullOrEmpty(filePath))
                {
                    string directoryName = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directoryName))
                        Directory.CreateDirectory(directoryName!);
                    InternalEditorUtility.SaveToSerializedFileAndForget(new Object[]
                    {
                        _instance
                    }, filePath, saveAsText);
                }
                else
                {
                    Debug.LogWarning($"Saving has no effect. Your class '{GetType()}' is missing the ConfigPathAttribute. " +
                                     $"Use this attribute to specify where to save your ConfigSingleton.\n" +
                                     $"Only call Save() and use this attribute if you want your state to survive between sessions of Unity.");
                }
            }
        }

        private static string _filePath;
        
        private static string GetFilePath()
        {
            return _filePath ??= $"ProjectSettings/{EditorUserBuildSettings.activeBuildTarget.ToString()}/{typeof(T).Name}.asset";
        }
    }
}