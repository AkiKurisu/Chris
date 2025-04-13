using System;
using System.Linq;
using UnityEngine;

namespace Chris.Modules
{
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
                .Where(x =>
                {
#if UNITY_EDITOR
                    if (x.GetName().Name.Contains(".Editor"))
                    {
                        return false;
                    }
#endif

                    return x.GetReferencedAssemblies().Any(name => name.Name == nameof(Chris)) 
                           || x.GetName().Name == nameof(Chris);
                })
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