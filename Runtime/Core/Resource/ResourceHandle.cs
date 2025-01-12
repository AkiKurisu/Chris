using System;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using UObject = UnityEngine.Object;
namespace Chris.Resource
{
    /// <summary>
    /// A lightweight encapsulation of <see cref="AsyncOperationHandle"/>
    /// </summary>
    public readonly struct ResourceHandle : IEquatable<ResourceHandle>, IDisposable
    {
        internal readonly uint Version;
        
        internal readonly int Index;
        
        internal readonly byte OperationType;
        
        internal AsyncOperationHandle InternalHandle => ResourceSystem.CastOperationHandle(Version, Index);
        
        public object Result => InternalHandle.Result;
        
        public UniTask Task => InternalHandle.ToUniTask();
        
        public ResourceHandle(uint version, int index, byte operationType)
        {
            Version = version;
            Index = index;
            OperationType = operationType;
        }
        
        /// <summary>
        /// Register completed result callback, no need to unregister since delegate list is clear after fire event
        /// </summary>
        /// <param name="callBack"></param>
        public void RegisterCallBack(Action<UObject> callBack)
        {
            InternalHandle.Completed += h => callBack?.Invoke(h.Result as UObject);
        }
        
        /// <summary>
        /// Register completed result callback, no need to unregister since delegate list is clear after fire event
        /// </summary>
        /// <returns></returns>
        public UObject WaitForCompletion()
        {
            return InternalHandle.WaitForCompletion() as UObject;
        }
        
        /// <summary>
        /// Converts handle to be typed.
        /// To convert back to non-typed, implicit conversion is available.
        /// </summary>
        /// <typeparam name="T">The type of the handle.</typeparam>
        /// <returns>A new handle that is typed.</returns>
        public ResourceHandle<T> Convert<T>()
        {
            return this;
        }
        public bool Equals(ResourceHandle other)
        {
            return other.Index == Index && other.InternalHandle.Equals(InternalHandle);
        }
        /// <summary>
        /// Implement of <see cref="IDisposable"/> to release resource
        /// </summary>
        public void Dispose()
        {
            ResourceSystem.Release(this);
        }
    }
    /// <summary>
    /// A lightweight replacement of <see cref="AsyncOperationHandle{T}"/>
    /// </summary>
    public readonly struct ResourceHandle<T> : IEquatable<ResourceHandle<T>>, IDisposable
    {
        internal readonly uint Version;
        
        internal readonly int Index;
        
        internal readonly byte OperationType;
        
        internal AsyncOperationHandle<T> InternalHandle => ResourceSystem.CastOperationHandle<T>(Version, Index);
        
        public T Result => InternalHandle.Result;
        
        public UniTask<T> Task => InternalHandle.ToUniTask();
        
        public ResourceHandle(uint version, int index, byte operationType)
        {
            Version = version;
            Index = index;
            OperationType = operationType;
        }
        
        public static implicit operator ResourceHandle(ResourceHandle<T> obj)
        {
            return new ResourceHandle(obj.Version, obj.Index, obj.OperationType);
        }
        
        public static implicit operator ResourceHandle<T>(ResourceHandle obj)
        {
            return new ResourceHandle<T>(obj.Version, obj.Index, obj.OperationType);
        }
        
        /// <summary>
        /// Register completed result callback, no need to unregister since delegate list is clear after fire event
        /// </summary>
        /// <param name="callBack"></param>
        public void RegisterCallBack(Action<T> callBack)
        {
            InternalHandle.Completed += h => callBack?.Invoke(h.Result);
        }
        
        /// <summary>
        /// Register completed result callback, no need to unregister since delegate list is clear after fire event
        /// </summary>
        /// <param name="callBack"></param>
        public void RegisterCallBack(Action callBack)
        {
            InternalHandle.Completed += (h) => callBack?.Invoke();
        }
        
        public T WaitForCompletion()
        {
            return InternalHandle.WaitForCompletion();
        }

        public bool Equals(ResourceHandle<T> other)
        {
            return other.Index == Index && other.InternalHandle.Equals(InternalHandle);
        }
        
        /// <summary>
        /// Implement of <see cref="IDisposable"/> to release resource
        /// </summary>
        public void Dispose()
        {
            ResourceSystem.Release(this);
        }
    }
}
