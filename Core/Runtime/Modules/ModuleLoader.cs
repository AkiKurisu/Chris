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
            var config = ModuleConfig.Get();
            var modules = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly =>
                {
#if UNITY_EDITOR
                    if (assembly.GetName().Name.Contains(".Editor"))
                    {
                        return false;
                    }
#endif

                    return assembly.GetReferencedAssemblies().Any(name => name.Name == nameof(Chris)) 
                           || assembly.GetName().Name == nameof(Chris);
                })
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(RuntimeModule).IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => (RuntimeModule)Activator.CreateInstance(type))
                .OrderBy(module => module.Order)
                .ToArray();

            foreach (var module in modules)
            {
                module.Initialize(config);
            }
            config.Save();
        }
    }
}