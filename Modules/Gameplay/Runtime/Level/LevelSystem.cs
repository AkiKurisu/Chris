using System;
using System.Collections.Generic;
using System.Linq;
using Chris.DataDriven;
using Chris.Resource;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
namespace Chris.Gameplay.Level
{
    public class LevelReference
    {
        public string Name => Scenes.Length > 0 ? Scenes[0].levelName : string.Empty;
        
        public LevelSceneRow[] Scenes;
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
            foreach (var scene in DataTables.SelectMany(x=>x.Value.GetAllRows<LevelSceneRow>()))
            {
                // Whether it can load in current platform
                if (!scene.ValidateLoadPolicy()) continue;

                if (!dict.TryGetValue(scene.levelName, out var cache))
                {
                    cache = dict[scene.levelName] = new List<LevelSceneRow>();
                }
                cache.Add(scene);
            }
            return _references = dict.Select(x => new LevelReference()
            {
                Scenes = x.Value.ToArray()
            }).ToArray();
        }
    }
    public static class LevelSystem
    {
        public static readonly LevelReference EmptyLevel = new() { Scenes = Array.Empty<LevelSceneRow>() };
        
        public static LevelReference LastLevel { get; private set; } = EmptyLevel;
        
        public static LevelReference CurrentLevel { get; private set; } = EmptyLevel;
        
        
        private static SceneInstance _mainScene;

        public static async UniTask LoadAsync(string levelName)
        {
            var reference = FindLevel(levelName);
            if (reference != null)
            {
                await LoadAsync(reference);
            }
        }

        public static async UniTask LoadAsync(LevelReference reference)
        {
            LastLevel = CurrentLevel;
            CurrentLevel = reference;
            // First check has single load scene
            var singleScene = reference.Scenes.FirstOrDefault(x => x.loadMode == LoadLevelMode.Single);
            bool hasDynamicScene = reference.Scenes.Any(x => x.loadMode == LoadLevelMode.Dynamic);
            if (singleScene == null)
            {
                // Unload current main scene if have no dynamic scene
                if (!hasDynamicScene && !_mainScene.Equals(default))
                {
                    await Addressables.UnloadSceneAsync(_mainScene).Task;
                }
            }
            else
            {
                /* Since Unity destroy and awake MonoBehaviour in same frame, need notify world still valid */
                GameWorld.Pin();
                _mainScene = await Addressables.LoadSceneAsync(singleScene.reference.Address).ToUniTask();
                GameWorld.UnPin();
            }
            // Parallel for the others
            using var parallel = UniParallel.Get();
            foreach (var scene in reference.Scenes)
            {
                if (scene.loadMode >= LoadLevelMode.Additive)
                {
                    parallel.Add(Addressables.LoadSceneAsync(scene.reference.Address, LoadSceneMode.Additive).Task.AsUniTask());
                }
            }
            await parallel;
        }

        public static LevelReference FindLevel(string levelName)
        {
            foreach (var level in LevelSceneDataTableManager.Get().GetLevelReferences())
            {
                if (level.Name == levelName)
                {
                    return level;
                }
            }
            return EmptyLevel;
        }
    }
}
