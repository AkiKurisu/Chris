
using System;
using System.Collections.Generic;
using System.Linq;
using Chris.Serialization;
using UnityEngine;

namespace Chris
{
    /// <summary>
    /// RuntimeModule is a base class for implementing logic before scene loaded in order to initialize earlier.
    /// </summary>
    public abstract class RuntimeModule
    {
        public abstract void Initialize(ModuleConfig config);
    }

    /// <summary>
    /// Config for <see cref="RuntimeModule"/>
    /// </summary>
    [PreferJsonConvert]
    public class ModuleConfig
    {
        private static ModuleConfig _config;

        /// <summary>
        /// Contains module additional data that can be changed during runtime
        /// </summary>
        public Dictionary<string, string> MetaData = new();
        
        public static ModuleConfig Get()
        {
            return _config ??= SaveUtility.LoadOrNew<ModuleConfig>();
        }

        public void Save()
        {
            SaveUtility.Save(this);
        }
    }

    public static class ModuleLoader
    {
        private static bool _isLoaded;

        /// <summary>
        /// Set enabled whether runtime modules should be loaded, default is true.
        /// Can be config before <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>
        /// </summary>
        public static bool Enable { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        public static void InitializeModules()
        {
            if (!Enable || _isLoaded) return;
            _isLoaded = true;
            var config = ModuleConfig.Get();
            var managerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(RuntimeModule).IsAssignableFrom(x) && !x.IsAbstract)
                .ToArray();

            foreach (var type in managerTypes)
            {
                var manager = (RuntimeModule)Activator.CreateInstance(type);
                manager.Initialize(config);
            }
        }
    }
}
