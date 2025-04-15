using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using UnityEngine;

namespace Chris.Serialization
{
    /// <summary>
    /// Serializer for read and write saving files
    /// </summary>
    public class SaveLoadSerializer
    {
        public static class TypeCache<T>
        {
            public static readonly bool PreferJsonConvert;

            static TypeCache()
            {
                PreferJsonConvert = typeof(T).GetCustomAttribute<PreferJsonConvertAttribute>() != null;
            }
        }
        
        private static readonly BinaryFormatter Formatter = new();

        private readonly string _path;
        
        private readonly string _extension;

        private const string DefaultExtension = "sav";

        public SaveLoadSerializer(string path, string extension = DefaultExtension)
        {
            _path = path;
            _extension = extension;
        }
        
        public void Save<T>(string key, T data)
        {
            string jsonData;
            if (TypeCache<T>.PreferJsonConvert)
                jsonData = JsonConvert.SerializeObject(data);
            else
                jsonData = JsonUtility.ToJson(data);
            if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);
            using var file = File.Create($"{_path}/{key}.{_extension}");
            Formatter.Serialize(file, jsonData);
        }
        
        public void Delete(string key)
        {
            if (!Directory.Exists(_path)) return;
            var path = $"{_path}/{key}.{_extension}";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        
        public void DeleteAll()
        {
            if (Directory.Exists(_path)) Directory.Delete(_path, true);
        }
        
        public void SaveJson(string key, string jsonData)
        {
            if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);
            using var file = File.Create($"{_path}/{key}.{_extension}");
            Formatter.Serialize(file, jsonData);
        }
        
        public bool Exists(string key)
        {
            return File.Exists($"{_path}/{key}.{_extension}");
        }
        
        public bool TryLoadJson(string key, out string jsonData)
        {
            var path = $"{_path}/{key}.{_extension}";
            if (File.Exists(path))
            {
                using var file = File.Open(path, FileMode.Open);
                jsonData = (string)Formatter.Deserialize(file);
                return true;
            }
            jsonData = null;
            return false;
        }

        public bool Overwrite<T>(string key, T data)
        {
            var path = $"{_path}/{key}.{_extension}";
            if (File.Exists(path))
            {
                using var file = File.Open(path, FileMode.Open);
                if (TypeCache<T>.PreferJsonConvert)
                    JsonConvert.PopulateObject((string)Formatter.Deserialize(file), data);
                else
                    JsonUtility.FromJsonOverwrite((string)Formatter.Deserialize(file), data);
                return true;
            }
            return false;
        }
        
        public T LoadOrNew<T>(string key) where T : class, new()
        {
            T data = null;
            var path = $"{_path}/{key}.{_extension}";
            if (File.Exists(path))
            {
                using var file = File.Open(path, FileMode.Open);
                if (TypeCache<T>.PreferJsonConvert)
                    data = JsonConvert.DeserializeObject<T>((string)Formatter.Deserialize(file));
                else
                    data = JsonUtility.FromJson<T>((string)Formatter.Deserialize(file));
            }
            data ??= new T();
            return data;
        }
        
        public object Load(string key, Type type, bool preferJsonConvert)
        {
            var path = $"{_path}/{key}.{_extension}";
            using var file = File.Open(path, FileMode.Open);
            if (preferJsonConvert)
                return JsonConvert.DeserializeObject((string)Formatter.Deserialize(file), type);
            return JsonUtility.FromJson((string)Formatter.Deserialize(file), type);
        }
    }
    
    public static class SaveUtility
    {
        static SaveUtility()
        {
            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
        }
        
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        public static readonly string SavePath = Path.Combine(Application.dataPath, "../Saved");
#else
        public static readonly string SavePath = Path.Combine(Application.persistentDataPath, "Saved");
#endif
        
        private static readonly SaveLoadSerializer Serializer = new(SavePath);
        
        /// <summary>
        /// Save data to saving
        /// </summary>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        public static void Save<T>(T data)
        {
            Serializer.Save(typeof(T).Name, data);
        }
        
        /// <summary>
        /// Delete saving
        /// </summary>
        /// <param name="key"></param>
        public static void Delete(string key)
        {
            Serializer.Delete(key);
        }
        
        /// <summary>
        /// Delete all savings
        /// </summary>
        public static void DeleteAll()
        {
            Serializer.DeleteAll();
        }

        /// <summary>
        /// Save json to saving
        /// </summary>
        /// <param name="key"></param>
        /// <param name="jsonData"></param>
        public static void SaveJson(string key, string jsonData)
        {
            Serializer.SaveJson(key, jsonData);
        }
        
        /// <summary>
        /// Whether saving with name exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Exists(string key)
        {
            return Serializer.Exists(key);
        }

        /// <summary>
        /// Load json from saving
        /// </summary>
        /// <param name="key"></param>
        /// <param name="jsonData"></param>
        public static bool TryLoadJson(string key, out string jsonData)
        {
            return Serializer.TryLoadJson(key, out jsonData);
        }
        
        /// <summary>
        /// Load json from saving and overwrite object
        /// </summary>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool Overwrite<T>(T data)
        {
            return Serializer.Overwrite(typeof(T).Name, data);
        }
        
        /// <summary>
        /// Load json from saving and parse to <see cref="T"/> object, if it not exists, allocate new one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T LoadOrNew<T>() where T : class, new()
        {
            return Serializer.LoadOrNew<T>(typeof(T).Name);
        }
    }
}
