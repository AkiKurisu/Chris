using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using R3.Chris;
using System;
using UnityEngine.Assertions;
using System.Collections.Generic;
using Chris.Pool;
using Chris.Resource;
using Chris.Schedulers;
using UnityEngine.Scripting;
namespace Chris.Gameplay.Audios
{
    [Preserve]
    public static class AudioSystem
    {
        private static Transform _hookRoot;
        
        private static Transform GetRoot()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return null;
#endif
            if (_hookRoot == null)
            {
                _hookRoot = GameWorld.Get().transform;
                Disposable.Create(ReleaseAll).AddTo(_hookRoot);
            }
            return _hookRoot;
        }
        
        private static readonly Dictionary<string, IDisposable> disposableCache = new();
        
        private static void Register(string address, IDisposable disposable)
        {
            Assert.IsNotNull(disposable);
            Assert.IsFalse(string.IsNullOrEmpty(address));
            if (disposableCache.TryGetValue(address, out var latestHandle)) latestHandle?.Dispose();
            disposableCache[address] = disposable;
        }
        
        /// <summary>
        /// Stop an addressable audio source
        /// </summary>
        /// <param name="address"></param>
        public static void Stop(string address)
        {
            if (disposableCache.TryGetValue(address, out var handle))
            {
                handle.Dispose();
                disposableCache.Remove(address);
            }
        }
        
        /// <summary>
        /// Stop an addressable audio source from audio <see cref="AudioClip.name"/>
        /// </summary>
        /// <param name="audioClip"></param>
        public static void Stop(AudioClip audioClip)
        {
            Stop(audioClip.name);
        }
        
        private static void ReleaseAll()
        {
            foreach (var handle in disposableCache.Values)
            {
                handle.Dispose();
            }
            disposableCache.Clear();
        }

        /// <summary>
        /// Play audioClip from address at point
        /// </summary>
        /// <param name="audioClipAddress"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        public static void PlayClipAtPoint(string audioClipAddress, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            PlayClipAtPointAsync(audioClipAddress, position, volume, spatialBlend, minDistance).Forget();
        }

        /// <summary>
        /// Play audioClip at point, optimized version of <see cref="AudioSource.PlayClipAtPoint(AudioClip, Vector3,float)"/> 
        /// </summary>
        /// <param name="audioClip"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        public static void PlayClipAtPoint(AudioClip audioClip, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            var audioObject = PooledAudioSource.Get(GetRoot(), volume, spatialBlend, minDistance);
            // This will forget audio object
            PlayClipAtPoint(audioClip, audioObject, position);
        }
        
        private static async UniTask PlayClipAtPointAsync(string audioClipAddress, Vector3 position, float volume, float spatialBlend, float minDistance)
        {
            var audioObject = PooledAudioSource.Get(GetRoot(), volume, spatialBlend, minDistance);
            var handle = ResourceSystem.LoadAssetAsync<AudioClip>(audioClipAddress).AddTo(audioObject);
            var audioClip = await handle;
            // This will forget audio object
            PlayClipAtPoint(audioClip, audioObject, position);
        }

        /// <summary>
        /// Schedule audioClip from address at point, stop it using <see cref="Stop"/>. 
        /// </summary>
        /// <param name="audioClipAddress"></param>
        /// <param name="position"></param>
        /// <param name="scheduleTime"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        /// <param name="appendClipDuration"></param>
        /// <returns></returns>
        public static void ScheduleClipAtPoint(string audioClipAddress, Vector3 position, float scheduleTime, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f, bool appendClipDuration = false)
        {
            ScheduleClipAtPointAsync(audioClipAddress, position, scheduleTime, volume, spatialBlend, minDistance, appendClipDuration).Forget();
        }
        
        /// <summary>
        ///  Schedule audioClip at point, stop it using <see cref="Stop"/>. 
        /// </summary>
        /// <param name="audioClip"></param>
        /// <param name="position"></param>
        /// <param name="scheduleTime"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        /// <param name="appendClipDuration"></param>
        public static void ScheduleClipAtPoint(AudioClip audioClip, Vector3 position, float scheduleTime, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f, bool appendClipDuration = false)
        {
            var audioObject = PooledAudioSource.Get(GetRoot(), volume, spatialBlend, minDistance);
            ScheduleClipAtPoint(audioClip, audioObject, position, scheduleTime, appendClipDuration);
            Register(audioClip.name, audioObject);
        }
        
        private static async UniTask ScheduleClipAtPointAsync(string audioClipAddress, Vector3 position, float scheduleTime, float volume, float spatialBlend = 1f, float minDistance = 10f, bool appendClipDuration = false)
        {
            var audioObject = PooledAudioSource.Get(GetRoot(), volume, spatialBlend, minDistance);
            var handle = ResourceSystem.LoadAssetAsync<AudioClip>(audioClipAddress).AddTo(audioObject);
            var audioClip = await handle;
            ScheduleClipAtPoint(audioClip, audioObject, position, scheduleTime, appendClipDuration);
            Register(audioClipAddress, audioObject);
        }
        
        public static float GetDuration(AudioClip clip)
        {
            return clip.length * (Time.timeScale < 0.01f ? 0.01f : Time.timeScale);
        }
        
        private static void PlayClipAtPoint(AudioClip clip, PooledAudioSource audioObject, Vector3 position)
        {
            var gameObject = audioObject.GameObject;
            gameObject.transform.position = position;
            audioObject.Component.clip = clip;
            audioObject.Component.Play();
            audioObject.Destroy(GetDuration(clip));
        }
        
        private static void ScheduleClipAtPoint(AudioClip clip, PooledAudioSource audioObject, Vector3 position, float scheduleTime, bool appendClipDuration)
        {
            var gameObject = audioObject.GameObject;
            gameObject.transform.position = position;
            audioObject.Component.clip = clip;
            audioObject.Component.Play();
            if (appendClipDuration)
                scheduleTime += GetDuration(clip);
            audioObject.SchedulePlay(scheduleTime);
        }
        
        private sealed class PooledAudioSource : PooledComponent<PooledAudioSource, AudioSource>
        {
            public static PooledAudioSource Get(Transform parent, float volume, float spatialBlend, float minDistance)
            {
                var pooledAudioSource = Get(parent);
                pooledAudioSource.Component.spatialBlend = 1f;
                pooledAudioSource.Component.volume = volume;
                pooledAudioSource.Component.spatialBlend = spatialBlend;
                pooledAudioSource.Component.minDistance = minDistance;
                return pooledAudioSource;
            }
            
            internal unsafe void SchedulePlay(float scheduleTime)
            {
                var handle = Scheduler.DelayUnsafe(scheduleTime, new SchedulerUnsafeBinding(this, &Play_Imp), isLooped: true);
                Add(handle);
            }
            
            private static void Play_Imp(object @object)
            {
                ((PooledAudioSource)@object).Component.Play();
            }
        }
    }
}
