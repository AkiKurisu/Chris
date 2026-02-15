using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using R3.Chris;
using System;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
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
            if (_hookRoot) return _hookRoot;
            _hookRoot = GameObjectPoolManager.Instance.transform;
            Disposable.Create(ReleaseAll).AddTo(_hookRoot);
            return _hookRoot;
        }
        
        private readonly struct AudioKey: IEquatable<AudioKey>
        {
            private readonly string _address;

            private readonly int _instanceId;

            public AudioKey(int instanceId)
            {
                _instanceId = instanceId;
                _address = null;
            }
            
            public AudioKey(string address)
            {
                _instanceId = 0;
                _address = address;
            }

            public bool Equals(AudioKey other)
            {
                return other._instanceId == _instanceId && other._address == _address;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_address, _instanceId);
            }
            
            public class Comparer : IEqualityComparer<AudioKey>
            {
                public bool Equals(AudioKey x, AudioKey y)
                {
                    return x.Equals(y);
                }

                public int GetHashCode(AudioKey key)
                {
                    return key.GetHashCode();
                }
            }
        }
        
        private static readonly Dictionary<AudioKey, PooledAudioSource> AudioCache = new(new AudioKey.Comparer());
        
        private static void Register(AudioKey key, PooledAudioSource audioSource)
        {
            Assert.IsNotNull(audioSource);
            if (AudioCache.TryGetValue(key, out var latestHandle)) latestHandle?.Dispose();
            AudioCache[key] = audioSource;
        }
        
        private static void Unregister(AudioKey key, PooledAudioSource audioSource)
        {
            Assert.IsNotNull(audioSource);
            if (!AudioCache.TryGetValue(key, out var latestHandle)) return;
            if (Equals(latestHandle, audioSource))
            {
                AudioCache.Remove(key);
            }
        }
        
        /// <summary>
        /// Stop an addressable audio source
        /// </summary>
        /// <param name="address"></param>
        public static void StopAudio(string address)
        {
            var key = new AudioKey(address);
            if (!AudioCache.TryGetValue(key, out var latestHandle)) return;
            latestHandle?.Dispose();
            AudioCache.Remove(key);
        }
        
        /// <summary>
        /// Stop an addressable audio source from audio <see cref="AudioClip"/>
        /// </summary>
        /// <param name="audioClip"></param>
        public static void StopAudio(AudioClip audioClip)
        {
            var key = new AudioKey(audioClip.GetInstanceID());
            if (!AudioCache.TryGetValue(key, out var latestHandle)) return;
            latestHandle?.Dispose();
            AudioCache.Remove(key);
        }
        
        /// <summary>
        /// Find an addressable audio source
        /// </summary>
        /// <param name="address"></param>
        public static AudioSource FindAudio(string address)
        {
            var key = new AudioKey(address);
            return AudioCache.GetValueOrDefault(key)?.Component;
        }
        
        /// <summary>
        /// Find an addressable audio source from audio <see cref="AudioClip"/>
        /// </summary>
        /// <param name="audioClip"></param>
        public static AudioSource FindAudio(AudioClip audioClip)
        {
            var key = new AudioKey(audioClip.GetInstanceID());
            return AudioCache.GetValueOrDefault(key)?.Component;
        }
        
        private static void ReleaseAll()
        {
            foreach (var handle in AudioCache.Values.ToArray())
            {
                handle?.Dispose();
            }
            AudioCache.Clear();
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
        public static AudioSource PlayClipAtPoint(AudioClip audioClip, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            var audioObject = PooledAudioSource.Get(new AudioKey(audioClip.GetInstanceID()), GetRoot(), volume, spatialBlend, minDistance);
            return PlayClipAtPoint(audioClip, audioObject, position);
        }
        
        /// <summary>
        /// Play audioClip async from address at point
        /// </summary>
        /// <param name="audioClipAddress"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        public static async UniTask<AudioSource> PlayClipAtPointAsync(string audioClipAddress, Vector3 position, float volume = 1f, 
            float spatialBlend = 1f, float minDistance = 10f)
        {
            var audioObject = PooledAudioSource.Get(new AudioKey(audioClipAddress), GetRoot(), volume, spatialBlend, minDistance);
            var handle = ResourceSystem.LoadAssetAsync<AudioClip>(audioClipAddress).AddTo(audioObject);
            var audioClip = await handle;
            return PlayClipAtPoint(audioClip, audioObject, position);
        }
        
        /// <summary>
        /// Play loop audioClip from address at point, stop it using <see cref="StopAudio(string)"/>. 
        /// </summary>
        /// <param name="audioClipAddress"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        public static void PlayLoopClipAtPoint(string audioClipAddress, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            PlayLoopClipAtPointAsync(audioClipAddress, position, volume, spatialBlend, minDistance).Forget();
        }

        /// <summary>
        /// Play loop audioClip at point, stop it using <see cref="StopAudio(UnityEngine.AudioClip)"/>. 
        /// </summary>
        /// <param name="audioClip"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        public static AudioSource PlayLoopClipAtPoint(AudioClip audioClip, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            var audioObject = PooledAudioSource.Get(new AudioKey(audioClip.GetInstanceID()), GetRoot(), volume, spatialBlend, minDistance);
            return PlayLoopClipAtPoint(audioClip, audioObject, position);
        }
        
        /// <summary>
        /// Play loop audioClip from address at point async, stop it using <see cref="StopAudio(string)"/>. 
        /// </summary>
        /// <param name="audioClipAddress"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        public static async UniTask<AudioSource> PlayLoopClipAtPointAsync(string audioClipAddress, Vector3 position, float volume = 1f, 
            float spatialBlend = 1f, float minDistance = 10f)
        {
            var audioObject = PooledAudioSource.Get(new AudioKey(audioClipAddress), GetRoot(), volume, spatialBlend, minDistance);
            var handle = ResourceSystem.LoadAssetAsync<AudioClip>(audioClipAddress).AddTo(audioObject);
            var audioClip = await handle;
            return PlayLoopClipAtPoint(audioClip, audioObject, position);
        }

        /// <summary>
        /// Schedule audioClip from address at point, stop it using <see cref="StopAudio(string)"/>. 
        /// </summary>
        /// <param name="audioClipAddress"></param>
        /// <param name="position"></param>
        /// <param name="scheduleTime"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        public static void ScheduleClipAtPoint(string audioClipAddress, Vector3 position, float scheduleTime, float volume = 1f, 
            float spatialBlend = 1f, float minDistance = 10f)
        {
            ScheduleClipAtPointAsync(audioClipAddress, position, scheduleTime, volume, spatialBlend, minDistance).Forget();
        }

        /// <summary>
        ///  Schedule audioClip at point, stop it using <see cref="StopAudio(UnityEngine.AudioClip)"/>. 
        /// </summary>
        /// <param name="audioClip"></param>
        /// <param name="position"></param>
        /// <param name="scheduleTime"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        public static AudioSource ScheduleClipAtPoint(AudioClip audioClip, Vector3 position, float scheduleTime, float volume = 1f, 
            float spatialBlend = 1f, float minDistance = 10f)
        {
            var audioObject = PooledAudioSource.Get(new AudioKey(audioClip.name),GetRoot(), volume, spatialBlend, minDistance);
            return ScheduleClipAtPoint(audioClip, audioObject, position, scheduleTime);
        }
        
        /// <summary>
        /// Schedule audioClip async from address at point, stop it using <see cref="StopAudio(string)"/>. 
        /// </summary>
        /// <param name="audioClipAddress"></param>
        /// <param name="position"></param>
        /// <param name="scheduleTime"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        public static async UniTask<AudioSource> ScheduleClipAtPointAsync(string audioClipAddress, Vector3 position, float scheduleTime, float volume, 
            float spatialBlend = 1f, float minDistance = 10f)
        {
            var audioObject = PooledAudioSource.Get(new AudioKey(audioClipAddress),GetRoot(), volume, spatialBlend, minDistance);
            var handle = ResourceSystem.LoadAssetAsync<AudioClip>(audioClipAddress).AddTo(audioObject);
            var audioClip = await handle;
            return ScheduleClipAtPoint(audioClip, audioObject, position, scheduleTime);
        }

        private static float GetDuration(AudioClip clip)
        {
            return clip.length * (Time.timeScale < 0.01f ? 0.01f : Time.timeScale);
        }
        
        private static AudioSource PlayClipAtPoint(AudioClip clip, PooledAudioSource audioObject, Vector3 position)
        {
            var gameObject = audioObject.GameObject;
            gameObject.transform.position = position;
            audioObject.Component.clip = clip;
            audioObject.PlayDuration(GetDuration(clip));
            return audioObject.Component;
        }
                
        private static AudioSource PlayLoopClipAtPoint(AudioClip clip, PooledAudioSource audioObject, Vector3 position)
        {
            var gameObject = audioObject.GameObject;
            gameObject.transform.position = position;
            audioObject.Component.clip = clip;
            audioObject.PlayLoop();
            return audioObject.Component;
        }
        
        private static AudioSource ScheduleClipAtPoint(AudioClip clip, PooledAudioSource audioObject, Vector3 position, float scheduleTime)
        {
            var gameObject = audioObject.GameObject;
            gameObject.transform.position = position;
            audioObject.Component.clip = clip;
            scheduleTime += GetDuration(clip);
            audioObject.SchedulePlay(scheduleTime);
            return audioObject.Component;
        }
        
        private sealed class PooledAudioSource : PooledComponent<PooledAudioSource, AudioSource>
        {
            private AudioKey _audioKey;
            
            public static PooledAudioSource Get(AudioKey key, Transform parent, float volume, float spatialBlend, float minDistance)
            {
                var pooledAudioSource = Get(parent);
                pooledAudioSource._audioKey = key;
                pooledAudioSource.Component.loop = false;
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
                Register(_audioKey, this);
                Component.Play();
            }
                        
            public void PlayLoop()
            {
                Register(_audioKey, this);
                Component.loop = true;
                Component.Play();
            }

            public void PlayDuration(float duration)
            {
                Destroy(duration);
                Component.Play();
            }
            
            private static void Play_Imp(object @object)
            {
                ((PooledAudioSource)@object).Component.Play();
            }

            protected override void OnDispose()
            {
                Component.Stop();
                Component.clip = null;
                Unregister(_audioKey, this);
                base.OnDispose();
            }
        }
    }
}
