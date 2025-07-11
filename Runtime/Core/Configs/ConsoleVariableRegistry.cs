using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chris.Configs;
using UnityEngine;

namespace Chris
{
    public class ConsoleVariableRegistry
    {
        private const string ChrisAssemblyName = "Chris";

        private readonly Dictionary<string, ConsoleVariable> _variables = new();

        private static ConsoleVariableRegistry _instance;

        private ConsoleVariableRegistry()
        {
            InitializeVariables();
        }

        public static ConsoleVariableRegistry Get()
        {
            return _instance ??= new ConsoleVariableRegistry();
        }

        private void InitializeVariables()
        {
            var configTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetReferencedAssemblies()
                    .Any(a => a.Name == ChrisAssemblyName) || assembly.GetName().Name == ChrisAssemblyName)
                .SelectMany(assembly => assembly.GetTypes()
                    .Where(IsConfigType))
                .ToArray();

            foreach (var configType in configTypes)
            {
                ProcessConfigType(configType);
            }
        }

        private void ProcessConfigType(Type configType)
        {
            var fields = configType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.GetCustomAttribute<ConsoleVariableAttribute>() != null)
                .ToArray();

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<ConsoleVariableAttribute>();
                CreateConsoleVariable(configType, field, attribute);
            }
        }

        private void CreateConsoleVariable(Type configType, FieldInfo fieldInfo, ConsoleVariableAttribute attribute)
        {
            var variableName = attribute.Name;
            var fieldType = fieldInfo.FieldType;

            try
            {
                ConsoleVariable variable;

                // Create appropriate console variable based on field type
                if (fieldType == typeof(int))
                {
                    variable = CreateTypedConsoleVariable(configType, fieldInfo, variableName, typeof(IntConsoleVariable<>));
                }
                else if (fieldType == typeof(float))
                {
                    variable = CreateTypedConsoleVariable(configType, fieldInfo, variableName, typeof(FloatConsoleVariable<>));
                }
                else if (fieldType == typeof(bool))
                {
                    variable = CreateTypedConsoleVariable(configType, fieldInfo, variableName, typeof(BoolConsoleVariable<>));
                }
                else if (fieldType == typeof(string))
                {
                    variable = CreateTypedConsoleVariable(configType, fieldInfo, variableName, typeof(StringConsoleVariable<>));
                }
                else
                {
                    Debug.LogWarning($"[Chris] Unsupported field type {fieldType} for console variable {variableName}");
                    return;
                }

                if (variable != null)
                {
                    if (!_variables.TryAdd(variableName, variable))
                    {
                        Debug.LogWarning($"[Chris] A config variable named {variableName} already exists!");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Chris] Failed to create console variable {variableName}: {ex.Message}");
            }
        }

        private static ConsoleVariable CreateTypedConsoleVariable(Type configType, FieldInfo fieldInfo, string variableName, Type consoleVariableGenericType)
        {
            // Create the generic type with the config type
            var consoleVariableType = consoleVariableGenericType.MakeGenericType(configType);

            // Create instance using constructor
            var constructor = consoleVariableType.GetConstructor(new[] { typeof(string), typeof(FieldInfo) });
            if (constructor == null)
            {
                Debug.LogError($"Constructor not found for console variable type {consoleVariableType}");
                return null;
            }

            return (ConsoleVariable)constructor.Invoke(new object[] { variableName, fieldInfo });
        }

        /// <summary>
        /// Get console variable by name
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <returns>Console variable if found, null otherwise</returns>
        public ConsoleVariable GetVariable(string name)
        {
            _variables.TryGetValue(name, out var variable);
            return variable;
        }

        /// <summary>
        /// Try to get console variable by name
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <param name="variable">The found variable</param>
        /// <returns>True if variable exists, false otherwise</returns>
        public bool TryGetVariable(string name, out ConsoleVariable variable)
        {
            variable = null;

            if (_variables.TryGetValue(name, out variable))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Load a config if it's not already loaded
        /// </summary>
        /// <param name="configType">Config type to load</param>
        /// <returns>True if config was loaded or already loaded, false otherwise</returns>
        public bool LoadConfig(Type configType)
        {
            // Use reflection to call ConfigSystem.GetConfig<T>() where T is the config type
            var getConfigMethod = typeof(ConfigSystem).GetMethod("GetConfig");
            if (getConfigMethod == null)
                return false;

            var genericMethod = getConfigMethod.MakeGenericMethod(configType);

            try
            {
                var config = genericMethod.Invoke(null, null);
                return config != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get all registered console variables
        /// </summary>
        /// <returns>Dictionary of all variables</returns>
        public ConsoleVariable[] GetAllVariables()
        {
            return _variables.Values.ToArray();
        }

        /// <summary>
        /// Check if a variable exists
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <returns>True if variable exists, false otherwise</returns>
        public bool HasVariable(string name)
        {
            return _variables.ContainsKey(name);
        }

        private static bool IsConfigType(Type t)
        {
            if (t.IsAbstract || t.IsGenericType) return false;
            return t.IsInheritedFromGenericDefinition(typeof(Config<>), out _);
        }
    }
}
