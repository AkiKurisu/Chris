using System;
using System.Reflection;
using UnityEngine;

namespace Chris.Configs
{
    /// <summary>
    /// Base class for config variables that can be modified through console
    /// </summary>
    public abstract class ConsoleVariable
    {
        public string Name { get; protected set; }
        
        public Type ConfigType { get; protected set; }
        
        public FieldInfo FieldInfo { get; protected set; }

        protected ConsoleVariable(string name, Type configType, FieldInfo fieldInfo)
        {
            Name = name;
            ConfigType = configType;
            FieldInfo = fieldInfo;
        }

        /// <summary>
        /// Get the current value of the variable
        /// </summary>
        /// <returns>Current value</returns>
        public abstract object GetValue();

        /// <summary>
        /// Set the value of the variable
        /// </summary>
        /// <param name="value">New value</param>
        public abstract void SetValue(object value);

        /// <summary>
        /// Get the type of the variable
        /// </summary>
        /// <returns>Variable type</returns>
        public virtual Type GetValueType()
        {
            return FieldInfo.FieldType;
        }
    }

    /// <summary>
    /// Generic typed console variable
    /// </summary>
    /// <typeparam name="TConfig">Config type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    public abstract class ConsoleVariable<TConfig, TValue> : ConsoleVariable
        where TConfig : Config<TConfig>, new()
    {
        protected ConsoleVariable(string name, FieldInfo fieldInfo) : base(name, typeof(TConfig), fieldInfo)
        {
        }

        private static TConfig GetConfig()
        {
            return ConfigSystem.GetConfig<TConfig>();
        }

        public override object GetValue()
        {
            var config = GetConfig();
            return FieldInfo.GetValue(config);
        }

        public override void SetValue(object value)
        {
            var config = GetConfig();
            if (value is TValue typedValue)
            {
                FieldInfo.SetValue(config, typedValue);
                config.Save(); // Save the config after modification
            }
            else
            {
                // Try to convert the value
                try
                {
                    var convertedValue = Convert.ChangeType(value, typeof(TValue));
                    FieldInfo.SetValue(config, convertedValue);
                    config.Save();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Chris] Failed to convert value {value} to type {typeof(TValue)}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Integer console variable
    /// </summary>
    /// <typeparam name="TConfig">Config type</typeparam>
    public class IntConsoleVariable<TConfig> : ConsoleVariable<TConfig, int>
        where TConfig : Config<TConfig>, new()
    {
        public IntConsoleVariable(string name, FieldInfo fieldInfo) : base(name, fieldInfo)
        {
        }
    }

    /// <summary>
    /// Float console variable
    /// </summary>
    /// <typeparam name="TConfig">Config type</typeparam>
    public class FloatConsoleVariable<TConfig> : ConsoleVariable<TConfig, float>
        where TConfig : Config<TConfig>, new()
    {
        public FloatConsoleVariable(string name, FieldInfo fieldInfo) : base(name, fieldInfo)
        {
        }
    }

    /// <summary>
    /// Boolean console variable
    /// </summary>
    /// <typeparam name="TConfig">Config type</typeparam>
    public class BoolConsoleVariable<TConfig> : ConsoleVariable<TConfig, bool>
        where TConfig : Config<TConfig>, new()
    {
        public BoolConsoleVariable(string name, FieldInfo fieldInfo) : base(name, fieldInfo)
        {
        }
    }

    /// <summary>
    /// String console variable
    /// </summary>
    /// <typeparam name="TConfig">Config type</typeparam>
    public class StringConsoleVariable<TConfig> : ConsoleVariable<TConfig, string>
        where TConfig : Config<TConfig>, new()
    {
        public StringConsoleVariable(string name, FieldInfo fieldInfo) : base(name, fieldInfo)
        {
        }
    }
}