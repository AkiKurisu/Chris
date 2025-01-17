using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UObject = UnityEngine.Object;
namespace Chris.Resource
{
    /// <summary>
    /// Loading and cache specific asset as a group and release them by control version
    /// </summary>
    /// <typeparam name="TAsset"></typeparam>
    public class ResourceCache<TAsset> : IDisposable, IReadOnlyDictionary<string, TAsset> 
        where TAsset : UObject
    {
        private readonly Dictionary<string, ResourceHandle<TAsset>> _internalHandles = new();
        
        private readonly Dictionary<string, TAsset> _cacheMap = new();
        
        private readonly Dictionary<string, int> _versionMap = new();

        /// <summary>
        /// Validate asset location before loading, throw <see cref="InvalidResourceRequestException"/> if not exist
        /// </summary>
        /// <value></value>
        public bool AddressSafeCheck { get; set; } = false;

        /// <summary>
        /// Current cache version
        /// </summary>
        /// <value></value>
        public int Version { get; private set; }

        public IEnumerable<string> Keys => _cacheMap.Keys;

        public IEnumerable<TAsset> Values => _cacheMap.Values;

        public int Count => _cacheMap.Count;

        public TAsset this[string key] => _cacheMap[key];
        
        private int _loadingRef;
        
        /// <summary>
        /// Flags when any asset is in loading
        /// </summary>
        public bool IsLoading => _loadingRef > 0;
        
        /// <summary>
        /// Load and cache asset async
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async UniTask<TAsset> LoadAssetAsync(string address)
        {
            _versionMap[address] = Version;
            if (!_cacheMap.TryGetValue(address, out TAsset asset))
            {
                _loadingRef++;
                if (AddressSafeCheck)
                    await ResourceSystem.CheckAssetAsync<TAsset>(address);
                asset = await LoadNewAssetAsync(address);
                _loadingRef--;
            }
            return asset;
        }
        
        /// <summary>
        /// Load and cache asset in sync way which will block game, not recommend
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public TAsset LoadAsset(string address)
        {
            _versionMap[address] = Version;
            if (!_cacheMap.TryGetValue(address, out TAsset asset))
            {
                if (AddressSafeCheck)
                    ResourceSystem.CheckAsset<TAsset>(address);
                asset = LoadNewAssetAsync(address).WaitForCompletion();
            }
            return asset;
        }
        private ResourceHandle<TAsset> LoadNewAssetAsync(string address, Action<TAsset> callBack = null)
        {
            if (_internalHandles.TryGetValue(address, out var internalHandle))
            {
                if (internalHandle.IsDone())
                {
                    callBack?.Invoke(internalHandle.Result);
                    return internalHandle;
                }

                internalHandle.RegisterCallback(callBack);
                return internalHandle;
            }
            //Create a new resource load call, also track it's handle
            internalHandle = ResourceSystem.LoadAssetAsync<TAsset>(address, (asset) =>
            {
                _cacheMap.Add(address, asset);
                callBack?.Invoke(asset);
            });
            _internalHandles.Add(address, internalHandle);
            return internalHandle;
        }
        
        /// <summary>
        /// Implementation of <see cref="IDisposable"/>, release all handles in cache.
        /// </summary>
        public void Dispose()
        {
            foreach (var handle in _internalHandles.Values)
            {
                ResourceSystem.ReleaseAsset(handle);
            }
            _internalHandles.Clear();
            _cacheMap.Clear();
            _versionMap.Clear();
        }
        
        /// <summary>
        /// Get cache addresses
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetCacheKeys() => _cacheMap.Keys;
        
        /// <summary>
        /// Update version
        /// </summary>
        public int UpdateVersion() => ++Version;
        
        /// <summary>
        /// Release all assets with target version
        /// </summary>
        /// <param name="version"></param>
        public void ReleaseAssetsWithVersion(int version)
        {
            _versionMap.Where(p => p.Value == version).Select(p => p.Key).ToList().ForEach(ads =>
            {
                if (_internalHandles.TryGetValue(ads, out var handle))
                    ResourceSystem.ReleaseAsset(handle);
                _cacheMap.Remove(ads);
                _internalHandles.Remove(ads);
                _versionMap.Remove(ads);
            });
        }
        
        /// <summary>
        /// Release assets with last version and update version
        /// </summary>
        public void ReleaseAssetsAndUpdateVersion()
        {
            ReleaseAssetsWithVersion(Version);
            UpdateVersion();
        }
        
        public bool ContainsKey(string key)
        {
            return _cacheMap.ContainsKey(key);
        }
        
        public bool TryGetValue(string key, out TAsset value)
        {
            return _cacheMap.TryGetValue(key, out value);
        }
        
        public IEnumerator<KeyValuePair<string, TAsset>> GetEnumerator()
        {
            return _cacheMap.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _cacheMap.GetEnumerator();
        }
    }

    #region Common Unity Assets
    
    /// <summary>
    /// Resource cache for <see cref="AudioClip"/>
    /// </summary>
    public class AudioClipCache : ResourceCache<AudioClip>
    {

    }
    
    /// <summary>
    /// Resource cache for <see cref="Texture2D"/>
    /// </summary>
    public class Texture2DCache : ResourceCache<Texture2D>
    {

    }
    
    /// <summary>
    /// Resource cache for <see cref="AnimationClip"/>
    /// </summary>
    public class AnimationClipCache : ResourceCache<AnimationClip>
    {

    }
    
    /// <summary>
    /// Resource cache for <see cref="RuntimeAnimatorController"/>
    /// </summary>
    public class RuntimeAnimatorControllerCache : ResourceCache<RuntimeAnimatorController>
    {

    }
    
    /// <summary>
    /// Resource cache for <see cref="TextAsset"/>
    /// </summary>
    public class TextAssetCache : ResourceCache<TextAsset>
    {

    }

    #endregion
}