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

        // Type mapping for console variables
        private static readonly Dictionary<Type, Type> TypeToConsoleVariableMap = new()
        {
            { typeof(int), typeof(IntConsoleVariable<>) },
            { typeof(float), typeof(FloatConsoleVariable<>) },
            { typeof(bool), typeof(BoolConsoleVariable<>) },
            { typeof(string), typeof(StringConsoleVariable<>) }
        };

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
            // Process fields
            ProcessMembers(configType, GetFieldsWithAttribute(configType), CreateFieldAccessor);

            // Process properties
            ProcessMembers(configType, GetPropertiesWithAttribute(configType), CreatePropertyAccessor);
        }

        private static FieldInfo[] GetFieldsWithAttribute(Type configType)
        {
            return configType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.GetCustomAttribute<ConsoleVariableAttribute>() != null)
                .ToArray();
        }

        private static PropertyInfo[] GetPropertiesWithAttribute(Type configType)
        {
            return configType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttribute<ConsoleVariableAttribute>() != null)
                .Where(prop => prop.CanRead && prop.CanWrite)
                .ToArray();
        }

        private void ProcessMembers<T>(Type configType, T[] members, Func<T, MemberAccessor> accessorFactory) where T : MemberInfo
        {
            foreach (var member in members)
            {
                var attribute = member.GetCustomAttribute<ConsoleVariableAttribute>();
                if (attribute == null) continue;

                var memberAccessor = accessorFactory(member);
                if (memberAccessor == null) continue;

                CreateConsoleVariable(configType, memberAccessor, attribute);
            }
        }

        private static MemberAccessor CreateFieldAccessor(FieldInfo field)
        {
            return new FieldAccessor(field);
        }

        private static MemberAccessor CreatePropertyAccessor(PropertyInfo property)
        {
            if (!property.CanRead || !property.CanWrite)
            {
                Debug.LogWarning($"[Chris] Property {property.Name} on {property.DeclaringType?.Name} must have both getter and setter to be used as console variable");
                return null;
            }

            return new PropertyAccessor(property);
        }

        private void CreateConsoleVariable(Type configType, MemberAccessor memberAccessor, ConsoleVariableAttribute attribute)
        {
            var variableName = attribute.Name;
            var memberType = memberAccessor.MemberType;

            try
            {
                var variable = CreateConsoleVariable_Internal(configType, memberAccessor, memberType, variableName);

                if (variable == null)
                {
                    Debug.LogWarning($"[Chris] Unsupported member type {memberType} for console variable {variableName}");
                    return;
                }

                if (!_variables.TryAdd(variableName, variable))
                {
                    Debug.LogWarning($"[Chris] A config variable named {variableName} already exists!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Chris] Failed to create console variable {variableName}: {ex.Message}");
            }
        }

        private static ConsoleVariable CreateConsoleVariable_Internal(Type configType, MemberAccessor memberAccessor, 
            Type memberType,string variableName)
        {
            // Redirect enum to int
            if (memberType.IsEnum)
            {
                memberType = typeof(int);
            }

            var variable = TypeToConsoleVariableMap.TryGetValue(memberType, out var consoleVariableType)
                ? CreateTypedConsoleVariable(configType, memberAccessor, variableName, consoleVariableType)
                : null;
            return variable;
        }

        private static ConsoleVariable CreateTypedConsoleVariable(Type configType, MemberAccessor memberAccessor, string variableName, Type consoleVariableGenericType)
        {
            // Create the generic type with the config type
            var consoleVariableType = consoleVariableGenericType.MakeGenericType(configType);

            // Create instance using constructor
            var constructor = consoleVariableType.GetConstructor(new[] { typeof(string), typeof(MemberAccessor) });
            if (constructor == null)
            {
                Debug.LogError($"Constructor not found for console variable type {consoleVariableType}");
                return null;
            }

            return (ConsoleVariable)constructor.Invoke(new object[] { variableName, memberAccessor });
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
