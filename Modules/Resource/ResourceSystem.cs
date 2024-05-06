using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Collections;
namespace Kurisu.Framework.Resource
{
    /// <summary>
    /// Simple system to load resource from address and label using addressable
    /// </summary>
    public class ResourceSystem
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
        internal const int AssetLoadOperation = 0;
        internal const int InstantiateOperation = 1;
        #region  Asset Load
        /// <summary>
        /// Check location whether valid and throw exception earlier
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="TAsset"></typeparam>
        private static void SafeCheck<TAsset>(object key)
        {
#if AF_RESOURCES_SAFE_CHECK
            var location = Addressables.LoadResourceLocationsAsync(key, typeof(TAsset));
            location.WaitForCompletion();
            if (location.Status != AsyncOperationStatus.Succeeded || location.Result.Count == 0)
            {
                string stringValue;
                if (key is IEnumerable<string> list) stringValue = $"[{string.Join(",", list)}]";
                else stringValue = key.ToString();
                throw new InvalidResourceRequestException(stringValue, $"Address {stringValue} not valid for loading {typeof(TAsset)} asset");
            }
#endif
        }
        /// <summary>
        /// Load asset
        /// </summary>
        /// <param name="address"></param>
        /// <param name="action"></param>
        /// <param name="unRegisterHandle"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ResourceHandle<T> AsyncLoadAsset<T>(string address, Action<T> action = null)
        {
            SafeCheck<T>(address);
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            if (action != null)
                handle.Completed += (h) => action.Invoke(h.Result);
            return CreateHandle(handle, AssetLoadOperation);
        }
        #endregion
        #region Instantiate
        /// <summary>
        /// Instantiate GameObject
        /// </summary>
        /// <param name="address"></param>
        /// <param name="parent"></param>
        /// <param name="action"></param>
        /// <param name="bindObject"></param>
        /// <returns></returns>
        public static ResourceHandle<GameObject> AsyncInstantiate(string address, Transform parent, Action<GameObject> action = null, GameObject bindObject = null)
        {
            SafeCheck<GameObject>(address);
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
            var resourceHandle = CreateHandle(handle, InstantiateOperation);
            handle.Completed += (h) => instanceIDMap.Add(h.Result.GetInstanceID(), resourceHandle.handleID);
            if (action != null)
                handle.Completed += (h) => action.Invoke(h.Result);
            return resourceHandle;
        }
        #endregion
        #region Release
        /// <summary>
        /// Release resource handle, should align with <see cref="AsyncLoadAsset"/>
        /// </summary>
        /// <param name="handle"></param>
        public static void ReleaseAsset(ResourceHandle handle)
        {
            if (handle.InternalHandle.Equals(null) || handle.InternalHandle.Result != null)
                Addressables.Release(handle.InternalHandle);
            internalHandleMap.Remove(handle.handleID);
        }
        /// <summary>
        /// Release GameObject Instance, should align with <see cref="AsyncInstantiate"/>
        /// </summary>
        /// <param name="obj"></param>
        public static void ReleaseInstance(GameObject obj)
        {
            if (instanceIDMap.TryGetValue(obj.GetInstanceID(), out int handleID))
            {
                internalHandleMap.Remove(handleID);
            }
            if (obj != null)
                Addressables.ReleaseInstance(obj);
        }
        #endregion
        #region  Multi Assets Load
        public static ResourceHandle<IList<T>> AsyncLoadAssets<T>(object key, Action<IList<T>> action = null)
        {
            SafeCheck<T>(key);
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(key, null);
            if (action != null)
                handle.Completed += (h) => action.Invoke(h.Result);
            return CreateHandle(handle, AssetLoadOperation);
        }
        public static ResourceHandle<IList<T>> AsyncLoadAssets<T>(IEnumerable key, MergeMode mode, Action<IList<T>> action = null)
        {
            SafeCheck<T>(key);
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(key, null, (Addressables.MergeMode)mode);
            if (action != null)
                handle.Completed += (h) => action.Invoke(h.Result);
            return CreateHandle(handle, AssetLoadOperation);
        }
        #endregion
        /// <summary>
        /// Start from 1 since 0 is always invalid handle
        /// </summary>
        private static int handleIndex = 1;
        private static readonly Dictionary<int, int> instanceIDMap = new();
        private static readonly Dictionary<int, AsyncOperationHandle> internalHandleMap = new();
        internal static ResourceHandle<T> CreateHandle<T>(AsyncOperationHandle<T> asyncOperationHandle, int operation)
        {
            internalHandleMap.Add(++handleIndex, asyncOperationHandle);
            return new ResourceHandle<T>(handleIndex, operation);
        }
        internal static AsyncOperationHandle<T> CastOperationHandle<T>(int handleID)
        {
            if (internalHandleMap.TryGetValue(handleID, out var handle))
            {
                return handle.Convert<T>();
            }
            else
            {
                return default;
            }
        }
        internal static AsyncOperationHandle CastOperationHandle(int handleID)
        {
            if (internalHandleMap.TryGetValue(handleID, out var handle))
            {
                return handle;
            }
            else
            {
                return default;
            }
        }
        public static bool IsValid(int handleID)
        {
            return internalHandleMap.TryGetValue(handleID, out _);
        }
    }
}