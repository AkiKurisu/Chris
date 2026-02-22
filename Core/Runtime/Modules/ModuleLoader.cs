using System;
using System.Linq;
using Chris.Configs;
using UnityEngine;

namespace Chris.Modules
{
    internal static class ModuleLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeModules()
        {
            ConfigsModule.InitializeInternal();
            var modules = AppDomain.CurrentDomain.GetAssemblies()
    #if UNITY_EDITOR
                    .Where(assembly => !assembly.GetName().Name.Contains(".Editor"))
#endif
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => typeof(RuntimeModule).IsAssignableFrom(type) && !type.IsAbstract)
                    .Select(type => (RuntimeModule)Activator.CreateInstance(type))
                    .OrderBy(module => module.Order)
                    .ToArray();

            foreach (var module in modules)
            {
                module.Initialize();
            }
        }
    }
}