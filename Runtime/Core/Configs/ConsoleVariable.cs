using System;
using System.Reflection;
using UnityEngine;

namespace Chris.Configs
{
    /// <summary>
    /// Abstraction for accessing both fields and properties
    /// </summary>
    public abstract class MemberAccessor
    {
        public abstract Type MemberType { get; }
        
        public abstract string Name { get; }
        
        public abstract object GetValue(object target);
        
        public abstract void SetValue(object target, object value);
    }

    /// <summary>
    /// Field accessor implementation
    /// </summary>
    public class FieldAccessor : MemberAccessor
    {
        private readonly FieldInfo _fieldInfo;

        public FieldAccessor(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        public override Type MemberType => _fieldInfo.FieldType;
        
        public override string Name => _fieldInfo.Name;

        public override object GetValue(object target)
        {
            return _fieldInfo.GetValue(target);
        }

        public override void SetValue(object target, object value)
        {
            _fieldInfo.SetValue(target, value);
        }
    }

    /// <summary>
    /// Property accessor implementation
    /// </summary>
    public class PropertyAccessor : MemberAccessor
    {
        private readonly PropertyInfo _propertyInfo;

        public PropertyAccessor(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public override Type MemberType => _propertyInfo.PropertyType;
        
        public override string Name => _propertyInfo.Name;

        public override object GetValue(object target)
        {
            return _propertyInfo.GetValue(target);
        }

        public override void SetValue(object target, object value)
        {
            _propertyInfo.SetValue(target, value);
        }
    }

    /// <summary>
    /// Base class for config variables that can be modified through console
    /// </summary>
    public abstract class ConsoleVariable
    {
        public string Name { get; protected set; }

        public Type ConfigType { get; protected set; }

        public MemberAccessor MemberAccessor { get; protected set; }

        protected ConsoleVariable(string name, Type configType, MemberAccessor memberAccessor)
        {
            Name = name;
            ConfigType = configType;
            MemberAccessor = memberAccessor;
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
            return MemberAccessor.MemberType;
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
        protected ConsoleVariable(string name, MemberAccessor memberAccessor) : base(name, typeof(TConfig), memberAccessor)
        {
        }

        private static TConfig GetConfig()
        {
            return ConfigSystem.GetConfig<TConfig>();
        }

        public override object GetValue()
        {
            var config = GetConfig();
            return MemberAccessor.GetValue(config);
        }

        public override void SetValue(object value)
        {
            var config = GetConfig();
            if (value is TValue typedValue)
            {
                MemberAccessor.SetValue(config, typedValue);
                config.Save(); // Save the config after modification
            }
            else
            {
                // Try to convert the value
                try
                {
                    var convertedValue = Convert.ChangeType(value, typeof(TValue));
                    MemberAccessor.SetValue(config, convertedValue);
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
        public IntConsoleVariable(string name, MemberAccessor memberAccessor) : base(name, memberAccessor)
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
        public FloatConsoleVariable(string name, MemberAccessor memberAccessor) : base(name, memberAccessor)
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
        public BoolConsoleVariable(string name, MemberAccessor memberAccessor) : base(name, memberAccessor)
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
        public StringConsoleVariable(string name, MemberAccessor memberAccessor) : base(name, memberAccessor)
        {
        }
    }
}