using System;
using System.Collections;
using System.Collections.Generic;
using Chris.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
namespace Chris.Resource
{
    /// <summary>
    /// Exception thrown when request resource address is invalid
    /// </summary>
    public class InvalidResourceRequestException : Exception
    {
        public string InvalidAddress { get; }
        
        public InvalidResourceRequestException(string address, string message) : base(message) { InvalidAddress = address; }
    }
    
    /// <summary>
    /// Resource system that loads resource by address and label based on Addressables.
    /// </summary>
    public static class ResourceSystem
    {
        /// <summary>
        /// Options for merging the results of requests.
        /// If keys (A, B) mapped to results ([1,2,4],[3,4,5])...
        ///  - UseFirst (or None) takes the results from the first key
        ///  -- [1,2,4]
        ///  - Union takes results of each key and collects items that matched any key.
        ///  -- [1,2,3,4,5]
        ///  - Intersection takes results of each key, and collects items that matched every key.
        ///  -- [4]
        /// </summary>
        /// <remarks>
        /// Aligned with <see cref="Addressables.MergeMode"/>
        /// </remarks>
        public enum MergeMode
        {
            /// <summary>
            /// Use to indicate that no merge should occur. The first set of results will be used.
            /// </summary>
            None = 0,

            /// <summary>
            /// Use to indicate that the merge should take the first set of results.
            /// </summary>
            UseFirst = 0,

            /// <summary>
            /// Use to indicate that the merge should take the union of the results.
            /// </summary>
            Union,

            /// <summary>
            /// Use to indicate that the merge should take the intersection of the results.
            /// </summary>
            Intersection
        }
        
        internal const byte AssetLoadOperation = 0;
        
        internal const byte InstantiateOperation = 1;

        #region  Asset Load
        /// <summary>
        /// Check resource location whether exists and throw <see cref="InvalidResourceRequestException"/> if not exist
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="TAsset"></typeparam>
        public static void CheckAsset<TAsset>(object key)
        {
            var location = Addressables.LoadResourceLocationsAsync(key, typeof(TAsset));
            location.WaitForCompletion();
            if (location.Status != AsyncOperationStatus.Succeeded || location.Result.Count == 0)
            {
                string stringValue;
                if (key is IEnumerable<string> list) stringValue = $"[{string.Join(",", list)}]";
                else stringValue = key.ToString();
                throw new InvalidResourceRequestException(stringValue, $"Address {stringValue} not valid for loading {typeof(TAsset)} asset");
            }
        }
        
        /// <summary>
        /// Check resource location whether exists and throw <see cref="InvalidResourceRequestException"/> if not exist
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mergeMode"></param>
        /// <typeparam name="TAsset"></typeparam>
        public static void CheckAsset<TAsset>(IEnumerable key, MergeMode mergeMode)
        {
            var location = Addressables.LoadResourceLocationsAsync(key, (Addressables.MergeMode)mergeMode, typeof(TAsset));
            location.WaitForCompletion();
            if (location.Status != AsyncOperationStatus.Succeeded || location.Result.Count == 0)
            {
                string stringValue;
                if (key is IEnumerable<string> list) stringValue = $"[{string.Join(",", list)}]";
                else stringValue = key.ToString();
                throw new InvalidResourceRequestException(stringValue, $"Address {stringValue} not valid for loading {typeof(TAsset)} asset");
            }
        }
        
        /// <summary>
        /// Check resource location whether exists and throw <see cref="InvalidResourceRequestException"/> if not exist
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="TAsset"></typeparam>
        /// <returns></returns>
        public static async UniTask CheckAssetAsync<TAsset>(object key)
        {
            var location = Addressables.LoadResourceLocationsAsync(key, typeof(TAsset));
            await location.ToUniTask();
            if (location.Status != AsyncOperationStatus.Succeeded || location.Result.Count == 0)
            {
                string stringValue;
                if (key is IEnumerable<string> list) stringValue = $"[{string.Join(',', list)}]";
                else stringValue = key.ToString();
                throw new InvalidResourceRequestException(stringValue, $"Address {stringValue} not valid for loading {typeof(TAsset)} asset");
            }
        }
        
        /// <summary>
        /// Check resource location whether exists and throw <see cref="InvalidResourceRequestException"/> if not exist
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mergeMode"></param>
        /// <typeparam name="TAsset"></typeparam>
        /// <returns></returns>
        public static async UniTask CheckAssetAsync<TAsset>(IEnumerable key, MergeMode mergeMode)
        {
            var location = Addressables.LoadResourceLocationsAsync(key, (Addressables.MergeMode)mergeMode, typeof(TAsset));
            await location.ToUniTask();
            if (location.Status != AsyncOperationStatus.Succeeded || location.Result.Count == 0)
            {
                string stringValue;
                if (key is IEnumerable<string> list) stringValue = $"[{string.Join(',', list)}]";
                else stringValue = key.ToString();
                throw new InvalidResourceRequestException(stringValue, $"Address {stringValue} not valid for loading {typeof(TAsset)} asset");
            }
        }

        /// <summary>
        /// Load asset async
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callBack"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ResourceHandle<T> LoadAssetAsync<T>(string address, Action<T> callBack = null)
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            if (callBack != null)
                handle.Completed += (h) => callBack.Invoke(h.Result);
            return CreateHandle(handle, AssetLoadOperation);
        }
        #endregion
        
        #region Instantiate

        /// <summary>
        /// Instantiate GameObject async
        /// </summary>
        /// <param name="address"></param>
        /// <param name="parent"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public static ResourceHandle<GameObject> InstantiateAsync(string address, Transform parent, Action<GameObject> callBack = null)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
            var resourceHandle = CreateHandle(handle, InstantiateOperation);
            handle.Completed += h => InstanceIDMap.Add(h.Result.GetInstanceID(), resourceHandle);
            if (callBack != null)
                handle.Completed += h => callBack.Invoke(h.Result);
            return resourceHandle;
        }
        #endregion
        
        #region Release
        /// <summary>
        /// Release resource
        /// </summary>
        /// <param name="handle"></param>
        /// <typeparam name="T"></typeparam>
        public static void Release<T>(ResourceHandle<T> handle)
        {
            if (!handle.IsValid()) return;
            if (handle.OperationType == InstantiateOperation)
                ReleaseInstance(handle.Result as GameObject);
            else
                ReleaseAsset(handle);
        }
        
        /// <summary>
        /// Release resource
        /// </summary>
        /// <param name="handle"></param>
        public static void Release(ResourceHandle handle)
        {
            if (!handle.IsValid()) return;
            if (handle.OperationType == InstantiateOperation)
                ReleaseInstance(handle.Result as GameObject);
            else
                ReleaseAsset(handle);
        }
        
        /// <summary>
        /// Release Asset, should align with <see cref="LoadAssetAsync{T}"/>
        /// </summary>
        /// <param name="handle"></param>
        public static void ReleaseAsset(ResourceHandle handle)
        {
            if (!handle.IsValid()) return;
            if (handle.InternalHandle.IsValid())
            {
                Addressables.Release(handle.InternalHandle);
            }
            ReleaseHandleInternal(handle);
        }
        
        /// <summary>
        /// Release GameObject Instance, should align with <see cref="InstantiateAsync"/>
        /// </summary>
        /// <param name="gameObject"></param>
        public static void ReleaseInstance(GameObject gameObject)
        {
            if (InstanceIDMap.TryGetValue(gameObject.GetInstanceID(), out var handle))
            {
                if (!handle.IsValid()) return;
                ReleaseHandleInternal(handle);
            }
            if (gameObject != null)
                Addressables.ReleaseInstance(gameObject);
        }
        
        private static void ReleaseHandleInternal(ResourceHandle handle)
        {
            Operations.RemoveAt(handle.Index);
            _version++;
        }
        #endregion
        
        #region  Multi Assets Load
        public static ResourceHandle<IList<T>> LoadAssetsAsync<T>(object key, Action<IList<T>> callBack = null)
        {
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(key, null);
            if (callBack != null)
                handle.Completed += (h) => callBack.Invoke(h.Result);
            return CreateHandle(handle, AssetLoadOperation);
        }
        
        public static ResourceHandle<IList<T>> LoadAssetsAsync<T>(IEnumerable key, MergeMode mode, Action<IList<T>> callBack = null)
        {
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(key, null, (Addressables.MergeMode)mode);
            if (callBack != null)
                handle.Completed += (h) => callBack.Invoke(h.Result);
            return CreateHandle(handle, AssetLoadOperation);
        }
        #endregion
        
        /// <summary>
        /// Start from 1 since 0 is always invalid handle
        /// </summary>
        private static uint _version = 1;
        
        private static readonly Dictionary<int, ResourceHandle> InstanceIDMap = new();
        
        private static readonly SparseList<AsyncOperationStructure> Operations = new(10, int.MaxValue);
        
        internal static ResourceHandle<T> CreateHandle<T>(AsyncOperationHandle<T> asyncOperationHandle, byte operation)
        {
            var index = Operations.AddUninitialized();
            var handle = new ResourceHandle<T>(_version, index, operation);
            Operations[index] = new AsyncOperationStructure
            {
                AsyncOperationHandle = asyncOperationHandle,
                ResourceHandle = handle
            };
            return handle;
        }
        
        internal static AsyncOperationHandle<T> CastOperationHandle<T>(uint version, int index)
        {
            return CastOperationHandle(version, index).Convert<T>();
        }
        
        internal static AsyncOperationHandle CastOperationHandle(uint version, int index)
        {
            if (Operations.IsAllocated(index))
            {
                if (Operations[index].ResourceHandle.Version == version)
                    return Operations[index].AsyncOperationHandle;
                return default;
            }

            return default;
        }
        
        public static bool IsValid(uint version, int index)
        {
            return Operations.IsAllocated(index) && Operations[index].ResourceHandle.Version == version;
        }
        
        private struct AsyncOperationStructure
        {
            public AsyncOperationHandle AsyncOperationHandle;
            
            public ResourceHandle ResourceHandle;
        }
    }
}