using Chris.Resource;
using R3;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Chris.Gameplay.Resource
{
    /// <summary>
    /// Singleton <see cref="ResourceCache{TAsset}"/> for sharing storage in <see cref="GameWorld"/> lifetime scope
    /// </summary>
    /// <typeparam name="TCache"></typeparam>
    /// <typeparam name="TAsset"></typeparam>
    public abstract class SingletonCache<TCache, TAsset> : ResourceCache<TAsset> 
        where TAsset: UObject
        where TCache: SingletonCache<TCache, TAsset>, new()
    {
        private static TCache _cache;

        private bool _isGlobal;
        
        /// <summary>
        /// Get singleton cache for sharing storage in <see cref="GameWorld"/> lifetime scope
        /// </summary>
        /// <returns></returns>
        public static TCache GetInstance()
        {
            if (!Application.isPlaying)
            {
                return new TCache();
            }
            if (_cache != null) return _cache;
            _cache = new TCache();
            _cache._isGlobal = true;
            Disposable.Create(() =>
            {
                _cache._isGlobal = false;
                _cache.Dispose();
                _cache = null;
            }).AddTo(GameWorld.Get().Cast());
            return _cache;
        }
        
        public override void Dispose()
        {
            /* Singleton should be managed by GameWorld */
            if (_isGlobal) return;
            base.Dispose();
        }
    }
    
    #region Common Unity Assets
    
    /// <summary>
    /// Resource cache for <see cref="AudioClip"/>
    /// </summary>
    public class AudioClipCache : SingletonCache<AudioClipCache, AudioClip>
    {

    }
    
    /// <summary>
    /// Resource cache for <see cref="Texture2D"/>
    /// </summary>
    public class Texture2DCache : SingletonCache<Texture2DCache, Texture2D>
    {

    }
    
    /// <summary>
    /// Resource cache for <see cref="AnimationClip"/>
    /// </summary>
    public class AnimationClipCache : SingletonCache<AnimationClipCache, AnimationClip>
    {

    }
    
    /// <summary>
    /// Resource cache for <see cref="RuntimeAnimatorController"/>
    /// </summary>
    public class RuntimeAnimatorControllerCache : SingletonCache<RuntimeAnimatorControllerCache, RuntimeAnimatorController>
    {

    }
    
    /// <summary>
    /// Resource cache for <see cref="TextAsset"/>
    /// </summary>
    public class TextAssetCache : SingletonCache<TextAssetCache, TextAsset>
    {

    }

    #endregion
}