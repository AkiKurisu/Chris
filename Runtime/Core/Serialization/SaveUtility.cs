using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using UnityEngine;
namespace Chris.Serialization
{
    public static class SaveUtility
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private static readonly string SavePath = Path.Combine(Application.dataPath, "../Saved");
#else
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "Saved");
#endif
        
        private static readonly BinaryFormatter Formatter = new();
        
        /// <summary>
        /// Save object data to saving
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        public static void Save(string key, object data)
        {
            string jsonData;
            if (data.GetType().GetCustomAttribute<PreferJsonConvertAttribute>() == null)
                jsonData = JsonUtility.ToJson(data);
            else
                jsonData = JsonConvert.SerializeObject(data);
            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
            using FileStream file = File.Create($"{SavePath}/{key}.sav");
            Formatter.Serialize(file, jsonData);
        }
        
        /// <summary>
        /// Save data to saving
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        public static void Save<T>(string key, T data)
        {
            string jsonData;
            if (typeof(T).GetCustomAttribute<PreferJsonConvertAttribute>() == null)
                jsonData = JsonUtility.ToJson(data);
            else
                jsonData = JsonConvert.SerializeObject(data);
            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
            using FileStream file = File.Create($"{SavePath}/{key}.sav");
            Formatter.Serialize(file, jsonData);
        }
        
        /// <summary>
        /// Save data to saving
        /// </summary>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        public static void Save<T>(T data)
        {
            Save(typeof(T).Name, data);
        }
        
        /// <summary>
        /// Delete saving
        /// </summary>
        /// <param name="key"></param>
        public static void Delete(string key)
        {
            if (!Directory.Exists(SavePath)) return;
            string path = $"{SavePath}/{key}.sav";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        
        /// <summary>
        /// Delete all savings
        /// </summary>
        public static void DeleteAll()
        {
            if (Directory.Exists(SavePath)) Directory.Delete(SavePath, true);
        }

        /// <summary>
        /// Save json to saving
        /// </summary>
        /// <param name="key"></param>
        /// <param name="jsonData"></param>
        public static void SaveJson(string key, string jsonData)
        {
            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
            using FileStream file = File.Create($"{SavePath}/{key}.sav");
            Formatter.Serialize(file, jsonData);
        }
        
        public static bool SavingExists(string key)
        {
            return File.Exists($"{SavePath}/{key}.sav");
        }
        
        /// <summary>
        /// Load json from saving
        /// </summary>
        /// <param name="key"></param>
        public static bool TryLoadJson(string key, out string jsonData)
        {
            string path = $"{SavePath}/{key}.sav";
            if (File.Exists(path))
            {
                using FileStream file = File.Open(path, FileMode.Open);
                jsonData = (string)Formatter.Deserialize(file);
                return true;
            }
            jsonData = null;
            return false;
        }
        
        /// <summary>
        /// Load json from saving and overwrite object
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public static bool Overwrite(string key, object data)
        {
            string path = $"{SavePath}/{key}.sav";
            if (File.Exists(path))
            {
                using FileStream file = File.Open(path, FileMode.Open);
                if (data.GetType().GetCustomAttribute<PreferJsonConvertAttribute>() == null)
                    JsonUtility.FromJsonOverwrite((string)Formatter.Deserialize(file), data);
                else
                    JsonConvert.PopulateObject((string)Formatter.Deserialize(file), data);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Load json from saving and overwrite object
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool Overwrite<T>(string key, T data)
        {
            string path = $"{SavePath}/{key}.sav";
            if (File.Exists(path))
            {
                using FileStream file = File.Open(path, FileMode.Open);
                if (typeof(T).GetCustomAttribute<PreferJsonConvertAttribute>() == null)
                    JsonUtility.FromJsonOverwrite((string)Formatter.Deserialize(file), data);
                else
                    JsonConvert.PopulateObject((string)Formatter.Deserialize(file), data);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Load json from saving and overwrite object
        /// </summary>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool Overwrite<T>(T data)
        {
            return Overwrite(typeof(T).Name, data);
        }
        
        /// <summary>
        /// Load json from saving and parse to <see cref="T"/> object, if has no saving allocate new one
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LoadOrNew<T>(string key) where T : class, new()
        {
            T data = null;
            string path = $"{SavePath}/{key}.sav";
            if (File.Exists(path))
            {
                using FileStream file = File.Open(path, FileMode.Open);
                if (typeof(T).GetCustomAttribute<PreferJsonConvertAttribute>() == null)
                    data = JsonUtility.FromJson<T>((string)Formatter.Deserialize(file));
                else
                    data = JsonConvert.DeserializeObject<T>((string)Formatter.Deserialize(file));
            }
            data ??= new T();
            return data;
        }
        
        /// <summary>
        /// Load json from saving and parse to <see cref="T"/> object, if has no saving allocate new one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T LoadOrNew<T>() where T : class, new()
        {
            return LoadOrNew<T>(typeof(T).Name);
        }
    }
}