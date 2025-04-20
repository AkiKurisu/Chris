using System;
using System.Linq;
using System.Reflection;
using Chris.Serialization;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_ANDROID
using System.IO;
#endif

namespace Chris.Configs.Editor
{
    /// <summary>
    /// Interface for receive build config callback
    /// </summary>
    public interface IConfigBuilder
    {
        /// <summary>
        /// Write config to persistent config
        /// </summary>
        void BuildConfig(SaveLoadSerializer serializer);
    }
    
    internal class ConfigsBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Collect all configs and export to streaming assets
            var serializer = new SaveLoadSerializer(ConfigsModule.ConfigStreamingDirectory, ConfigsModule.ConfigExtension);
            var baseGenericType = typeof(ScriptableSingleton<>);
            const string assemblyName = "Chris.Editor";
            
            // Get editor types implement ScriptableSingleton<T>
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetReferencedAssemblies()
                    .Any(a => a.Name == assemblyName) || assembly.GetName().Name == assemblyName)
                .SelectMany(assembly => assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsGenericType));

            // Get generic argument
            foreach (var type in allTypes)
            {
                var current = type.BaseType;
                while (current != null)
                {
                    if (current.IsGenericType 
                        && current.GetGenericTypeDefinition() == baseGenericType)
                    {
                        var instanceProp = type.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                        if (instanceProp != null)
                        {
                            var instance = instanceProp.GetValue(null);
                            if (instance is IConfigBuilder configBuilder)
                            {
                                configBuilder.BuildConfig(serializer);
                            }
                        }
                        break;
                    }

                    current = current.BaseType;
                }
            }
            
#if UNITY_ANDROID
            // If android, make archive file and delete configs folder
            if (Directory.GetFiles(ConfigsModule.ConfigStreamingDirectory).Length > 0)
            {
                if (!ZipWrapper.Zip(new[] { ConfigsModule.ConfigStreamingDirectory },
                        ConfigsModule.ConfigStreamingDirectory + ".zip"))
                {
                    throw new Exception("Archive configs failed");
                }
            }
            Directory.Delete(ConfigsModule.ConfigStreamingDirectory, true);
#endif
        }
    }
}
