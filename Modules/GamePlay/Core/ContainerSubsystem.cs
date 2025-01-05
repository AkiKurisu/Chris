using System;
using System.Collections.Generic;
using Chris.Collections;
using UnityEngine.Assertions;
namespace Chris.Gameplay
{
    /// <summary>
    /// World lifetime scope IOC subsystem
    /// </summary>
    [InitializeOnWorldCreate]
    public class ContainerSubsystem : WorldSubsystem
    {
        private readonly IOCContainer _container = new();
        
        private readonly Dictionary<Type, Action<object>> _typeCallbackMap = new();

        public static ContainerSubsystem Get()
        {
            return GameWorld.Get()?.GetSubsystem<ContainerSubsystem>();
        }
        
        /// <summary>
        /// Register a callback when target type instance is registered
        /// </summary>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void RegisterCallback<T>(Action<T> callback)
        {
            Assert.IsNotNull(callback, "[ContainerSubsystem] Instance callback is null, which is not expected.");
            var type = typeof(T);
            if (!_typeCallbackMap.ContainsKey(type))
            {
                _typeCallbackMap[type] = obj => callback((T)obj);
            }
            else
            {
                _typeCallbackMap[type] += obj => callback((T)obj);
            }
        }
        
        /// <summary>
        /// Register target type instance
        /// </summary>
        /// <param name="instance"></param>
        /// <typeparam name="T"></typeparam>
        public void Register<T>(T instance)
        {
            _container.Register(instance);
            var type = typeof(T);
            if (_typeCallbackMap.TryGetValue(type, out Action<object> callBack))
            {
                callBack?.Invoke(instance);
            }
        }
        
        /// <summary>
        /// Unregister target type instance
        /// </summary>
        /// <param name="instance"></param>
        /// <typeparam name="T"></typeparam>
        public void Unregister<T>(T instance)
        {
            _container.Unregister(instance);
        }
        
        /// <summary>
        /// Get target type instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T Resolve<T>() where T : class
        {
            return _container.Resolve<T>();
        }
        
        protected override void Release()
        {
            _container.Clear();
            _typeCallbackMap.Clear();
        }
    }
}