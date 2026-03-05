using System;
using System.Collections.Generic;
using System.Linq;
using Chris.DataDriven;
using Chris.Pool;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Chris.Gameplay.Level
{
    /// <summary>
    /// Reference to gameplay level structure
    /// </summary>
    public class LevelReference
    {
        private string[] _tags;
        
        public string Name => Scenes.Length > 0 ? Scenes[0].levelName : string.Empty;
        
        /// <summary>
        /// All scenes contained in level
        /// </summary>
        public LevelSceneRow[] Scenes { get; }
        
        /// <summary>
        /// Get level tags
        /// </summary>
        public string[] Tags => _tags ??= Scenes.SelectMany(row => row.tags).Distinct().ToArray();

        /// <summary>
        /// Is level contains main scene
        /// </summary>
        public bool IsMain => Scenes.Any(sceneRow => sceneRow.loadMode == LoadLevelMode.Single);

        /// <summary>
        /// Get a empty level reference
        /// </summary>
        public static readonly LevelReference Empty = new();

        private LevelReference()
        {
            Scenes = Array.Empty<LevelSceneRow>();
        }
        
        public LevelReference(IEnumerable<LevelSceneRow> levelSceneRows)
        {
            Scenes = levelSceneRows.ToArray();
        }
    }

    public sealed class LevelSceneDataTableManager : DataTableManager<LevelSceneDataTableManager>
    {
        public const string TableKey = "LevelSceneDataTable";
        
        public LevelSceneDataTableManager(object _) : base(_)
        {
        }

        protected override UniTask Initialize(bool sync)
        {
            return InitializeSingleTable(TableKey, sync);
        }
        
        private LevelReference[] _references;
        
        public LevelReference[] GetLevelReferences()
        {
            if (_references != null) return _references;
            
            var dict = new Dictionary<string, List<LevelSceneRow>>();
            foreach (var scene in DataTables.SelectMany(pair => pair.Value.GetAllRows<LevelSceneRow>()))
            {
                // Whether it can load in current platform
                if (!scene.ValidateLoadPolicy()) continue;

                if (!dict.TryGetValue(scene.levelName, out var rows))
                {
                    rows = dict[scene.levelName] = new List<LevelSceneRow>();
                }
                rows.Add(scene);
            }
            return _references = dict.Select(keyValuePair => new LevelReference(keyValuePair.Value)).ToArray();
        }
        
        public LevelReference FindLevel(string levelName)
        {
            foreach (var level in GetLevelReferences())
            {
                if (level.Name == levelName)
                {
                    return level;
                }
            }
            return LevelReference.Empty;
        }
        
        public LevelReference FindLevelFromTag(string tag)
        {
            foreach (var level in GetLevelReferences())
            {
                if (level.Tags.Contains(tag))
                {
                    return level;
                }
            }
            return LevelReference.Empty;
        }
    }
    
    public static class LevelSystem
    {
        /// <summary>
        /// Last loaded level that contains main scene
        /// </summary>
        public static LevelReference LastLevel { get; private set; } = LevelReference.Empty;
        
        /// <summary>
        /// Current loaded level that contains main scene
        /// </summary>
        public static LevelReference CurrentLevel { get; private set; } = LevelReference.Empty;
        
        private static readonly Subject<LevelReference> LevelPreloadSubject = new();
                
        private static readonly Subject<LevelReference> LevelPostLoadSubject = new();

        /// <summary>
        /// Event when level start loading
        /// </summary>
        public static Observable<LevelReference> LevelPreload => LevelPreloadSubject;

        /// <summary>
        /// Event when level end loading
        /// </summary>
        public static Observable<LevelReference> LevelPostLoad => LevelPostLoadSubject;

        private static readonly ReactiveProperty<float> LoadingProgressProperty = new(0f);

        /// <summary>
        /// Normalized [0, 1] loading progress for the current LoadAsync operation.
        /// Resets to 0 when loading begins and reaches 1 when all scenes are loaded.
        /// </summary>
        public static ReadOnlyReactiveProperty<float> LoadingProgress => LoadingProgressProperty;

        // Handles for all additive scenes loaded as part of the current level,
        // ordered by load time (last element = most recently loaded).
        // Cleared and released whenever a new Single-mode scene takes over.
        private static readonly List<AsyncOperationHandle<SceneInstance>> AdditiveHandles = new();

        /// <summary>
        /// Number of additive scenes currently loaded on top of the current level's base scene.
        /// </summary>
        public static int AdditiveSceneCount => AdditiveHandles.Count;

        /// <summary>
        /// Load level by name
        /// </summary>
        /// <param name="levelName"></param>
        public static async UniTask LoadAsync(string levelName)
        {
            var reference = LevelSceneDataTableManager.Get().FindLevel(levelName);
            if (reference != null)
            {
                await LoadAsync(reference);
            }
        }

        /// <summary>
        /// Load level from <see cref="LevelReference"/>
        /// </summary>
        /// <param name="reference"></param>
        public static async UniTask LoadAsync(LevelReference reference)
        {
            LoadingProgressProperty.Value = 0f;
            LevelPreloadSubject.OnNext(reference);
            
            // First check has single load scene
            var mainScene = reference.Scenes.FirstOrDefault(row => row.loadMode == LoadLevelMode.Single);
            var additiveScenes = reference.Scenes.Where(s => s.loadMode >= LoadLevelMode.Additive).ToArray();
            int totalScenes = (mainScene != null ? 1 : 0) + additiveScenes.Length;
            var aggregate = totalScenes > 0 ? new AggregateProgress(LoadingProgressProperty, totalScenes) : null;
            int slotIndex = 0;

            if (mainScene != null)
            {
                LastLevel = CurrentLevel;
                CurrentLevel = reference;

                // The incoming Single load will implicitly unload all previous scenes via Unity.
                // Release the Addressables handles for old additive scenes to avoid reference leaks.
                ReleaseAdditiveHandles();
                
                /* Since Unity destroy and awake MonoBehaviour in same frame, need notify world still valid */
                using (GameWorld.Pin())
                {
                    await Addressables.LoadSceneAsync(mainScene.reference.Address)
                        .ToUniTask(progress: aggregate?.GetSlotProgress(slotIndex));
                }
                slotIndex++;
            }
            
            // Parallel for the others, tracking each handle for later explicit unload
            using var parallel = UniParallel.Get();
            foreach (var scene in additiveScenes)
            {
                var handle = Addressables.LoadSceneAsync(scene.reference.Address, LoadSceneMode.Additive);
                AdditiveHandles.Add(handle);
                parallel.Add(handle.ToUniTask(progress: aggregate?.GetSlotProgress(slotIndex++)));
            }
            await parallel;

            LoadingProgressProperty.Value = 1f;
            LevelPostLoadSubject.OnNext(reference);
        }

        /// <summary>
        /// Unload only the most recently loaded additive scene of the current level.
        /// Returns true if a scene was unloaded, false if no additive scenes are loaded.
        /// </summary>
        public static async UniTask<bool> UnloadLastAdditiveAsync()
        {
            if (AdditiveHandles.Count == 0) return false;

            var lastIndex = AdditiveHandles.Count - 1;
            var handle = AdditiveHandles[lastIndex];
            AdditiveHandles.RemoveAt(lastIndex);

            if (handle.IsValid())
            {
                await Addressables.UnloadSceneAsync(handle).ToUniTask();
            }

            await Resources.UnloadUnusedAssets().ToUniTask();
            return true;
        }

        /// <summary>
        /// Explicitly unload all additive scenes belonging to the current level.
        /// </summary>
        public static async UniTask UnloadAdditiveAsync()
        {
            var handles = AdditiveHandles.ToArray();
            AdditiveHandles.Clear();

            foreach (var handle in handles)
            {
                if (handle.IsValid())
                {
                    await Addressables.UnloadSceneAsync(handle).ToUniTask();
                }
            }

            await Resources.UnloadUnusedAssets().ToUniTask();
        }

        // Release all stored additive handles without triggering scene unload
        // (used when Unity is already unloading those scenes via a Single-mode load).
        private static void ReleaseAdditiveHandles()
        {
            foreach (var handle in AdditiveHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            AdditiveHandles.Clear();
        }

        // Aggregates progress from N independent scene-loading slots into a single ReactiveProperty.
        // Each slot contributes equally (1/N weight) to the total progress value.
        private sealed class AggregateProgress
        {
            private readonly ReactiveProperty<float> _target;
            private readonly float[] _slots;

            public AggregateProgress(ReactiveProperty<float> target, int slotCount)
            {
                _target = target;
                _slots = new float[slotCount];
            }

            public IProgress<float> GetSlotProgress(int index)
            {
                return new SlotProgress(this, index);
            }

            private void UpdateSlot(int index, float value)
            {
                _slots[index] = value;
                float sum = 0f;
                for (int i = 0; i < _slots.Length; i++) sum += _slots[i];
                _target.Value = sum / _slots.Length;
            }

            private sealed class SlotProgress : IProgress<float>
            {
                private readonly AggregateProgress _parent;
                private readonly int _index;

                public SlotProgress(AggregateProgress parent, int index)
                {
                    _parent = parent;
                    _index = index;
                }

                public void Report(float value) => _parent.UpdateSlot(_index, value);
            }
        }
    }
}
