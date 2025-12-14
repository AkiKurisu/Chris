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
                .Select(t => (RuntimeModule)Activator.CreateInstance(t))
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