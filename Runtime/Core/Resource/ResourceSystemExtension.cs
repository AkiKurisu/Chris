using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace Chris.Resource
{
    public static class ResourceSystemExtension
    {
        public static UniTask<T>.Awaiter GetAwaiter<T>(this ResourceHandle<T> handle)
        {
            return handle.InternalHandle.GetAwaiter();
        }
        
        public static UniTask.Awaiter GetAwaiter(this ResourceHandle handle)
        {
            return handle.InternalHandle.GetAwaiter();
        }
        
        public static UniTask<T> ToUniTask<T>(this ResourceHandle<T> handle)
        {
            return handle.InternalHandle.ToUniTask();
        }
        
        public static UniTask ToUniTask(this ResourceHandle handle)
        {
            return handle.InternalHandle.ToUniTask();
        }
        
        public static UniTask<T> WithCancellation<T>(this ResourceHandle<T> handle, CancellationToken cancellationToken, bool cancelImmediately = false, bool autoReleaseWhenCanceled = false)
        {
            return handle.InternalHandle.WithCancellation(cancellationToken, cancelImmediately, autoReleaseWhenCanceled);
        }
        
        public static UniTask WithCancellation(this ResourceHandle handle, CancellationToken cancellationToken, bool cancelImmediately = false, bool autoReleaseWhenCanceled = false)
        {
            return handle.InternalHandle.WithCancellation(cancellationToken, cancelImmediately, autoReleaseWhenCanceled);
        }
        
        /// <summary>
        /// Whether internal operation is valid
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool IsValid(this ResourceHandle handle)
        {
            return ResourceSystem.IsValid(handle.Version, handle.Index);
        }
        
        /// <summary>
        /// Whether internal operation is valid
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool IsValid<T>(this ResourceHandle<T> handle)
        {
            return ResourceSystem.IsValid(handle.Version, handle.Index);
        }
        
        /// <summary>
        /// Whether internal operation is done
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool IsDone(this ResourceHandle handle)
        {
            return ResourceSystem.IsValid(handle.Version, handle.Index) && handle.InternalHandle.IsDone;
        }
        
        /// <summary>
        /// Whether internal operation is done
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool IsDone<T>(this ResourceHandle<T> handle)
        {
            return ResourceSystem.IsValid(handle.Version, handle.Index) && handle.InternalHandle.IsDone;
        }
        
        /// <summary>
        /// Load asset async by <see cref="AssetReferenceT{T}"/> and convert to <see cref="ResourceHandle{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetReferenceT"></param>
        /// <returns></returns>
        public static ResourceHandle<T> ToResourceHandle<T>(this AssetReferenceT<T> assetReferenceT) where T : Object
        {
            return ResourceSystem.CreateHandle(assetReferenceT.LoadAssetAsync(), ResourceSystem.AssetLoadOperation);
        }
    }
}
