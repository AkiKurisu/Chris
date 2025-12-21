using System;
using System.Collections.Generic;
using System.Linq;
using Chris.DataDriven;
using Chris.Pool;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Chris.Gameplay.Level
{
    /// <summary>
    /// Reference to gameplay level structure
    /// </summary>
    public class LevelReference
    {
        public string Name => Scenes.Length > 0 ? Scenes[0].levelName : string.Empty;
        
        public LevelSceneRow[] Scenes;
        
        public static readonly LevelReference Empty = new() { Scenes = Array.Empty<LevelSceneRow>() };

        private string[] _tags;
        public string[] Tags => _tags ??= Scenes.SelectMany(row => row.tags).Distinct().ToArray();
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
            return _references = dict.Select(x => new LevelReference
            {
                Scenes = x.Value.ToArray()
            }).ToArray();
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
        public static LevelReference LastLevel { get; private set; } = LevelReference.Empty;
        
        public static LevelReference CurrentLevel { get; private set; } = LevelReference.Empty;
        
        
        private static SceneInstance _mainScene;
        
        private static readonly Subject<LevelReference> _levelPreload = new();
                
        private static readonly Subject<LevelReference> _levelPostLoad = new();

        /// <summary>
        /// Event when level start loading
        /// </summary>
        public static Observable<LevelReference> LevelPreload => _levelPreload;

        /// <summary>
        /// Event when level end loading
        /// </summary>
        public static Observable<LevelReference> LevelPostLoad => _levelPostLoad;

        public static async UniTask LoadAsync(string levelName)
        {
            var reference = LevelSceneDataTableManager.Get().FindLevel(levelName);
            if (reference != null)
            {
                await LoadAsync(reference);
            }
        }

        public static async UniTask LoadAsync(LevelReference reference)
        {
            LastLevel = CurrentLevel;
            CurrentLevel = reference;
            _levelPreload.OnNext(reference);
            // First check has single load scene
            var singleScene = reference.Scenes.FirstOrDefault(row => row.loadMode == LoadLevelMode.Single);
            bool hasDynamicScene = reference.Scenes.Any(row => row.loadMode == LoadLevelMode.Dynamic);
            if (singleScene == null)
            {
                // Unload current main scene if there is no dynamic scene
                if (!hasDynamicScene && _mainScene.Scene.IsValid())
                {
                    await Addressables.UnloadSceneAsync(_mainScene).ToUniTask();
                }
            }
            else
            {
                /* Since Unity destroy and awake MonoBehaviour in same frame, need notify world still valid */
                using (GameWorld.Pin())
                {
                    _mainScene = await Addressables.LoadSceneAsync(singleScene.reference.Address).ToUniTask();
                }
            }
            // Parallel for the others
            using var parallel = UniParallel.Get();
            foreach (var scene in reference.Scenes)
            {
                if (scene.loadMode >= LoadLevelMode.Additive)
                {
                    parallel.Add(Addressables.LoadSceneAsync(scene.reference.Address, LoadSceneMode.Additive).ToUniTask());
                }
            }
            await parallel;
            
            _levelPostLoad.OnNext(reference);
        }
    }
}
