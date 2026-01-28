// Reference: URP17 Samples
#if UNITY_6000_3_OR_NEWER
using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Chris.Gameplay.Graphics
{
    public enum GraphicsStateCollectMode
    {
        Auto,
        Tracing,
        WarmUp
    };

    [Serializable]
    public struct GraphicsStateWarmUpProgress
    {
        public int completedCount;

        public int total;
    }
    
    public class GraphicsStateCollectionManager : MonoBehaviour
    {
       public GraphicsStateCollectMode mode;

       public bool verbose;

       [Range(1, 100)]
       public int warmUpBatchSize = 10;

        // Set up the collection of PSOs, and set where to store the files in the project folder.
        public GraphicsStateCollection[] collections;
        
        public const string CollectionFolderPath = "Settings/GraphicsStateCollections/";

        // Create internal variables for the traced PSOs, and the file to output.
        private string _outputCollectionName;
        
        private GraphicsStateCollection _graphicsStateCollection;

        private bool _isWarmed;
        
        private readonly Subject<GraphicsStateWarmUpProgress> _warmUpProgressSubject = new();

        public Observable<GraphicsStateWarmUpProgress> WarmUpProgress => _warmUpProgressSubject;

        public static GraphicsStateCollectionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // For mobile platforms, data is additionally saved when focus is lost as OnDestroy() is not guaranteed to be called.
        private void OnApplicationFocus(bool focus)
        {
            if (focus) return;
            if (IsWarmupMode() || !_graphicsStateCollection) return;
            
            if (verbose) Debug.Log("Focus changed. Sending collection to Editor with " + _graphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
            _graphicsStateCollection.SendToEditor(_outputCollectionName);
        }

        private void OnDestroy()
        {
            Instance = null;
            if (IsWarmupMode() || !_graphicsStateCollection) return;
            
            _graphicsStateCollection.EndTrace();
            if (verbose) Debug.Log("Sending collection to Editor with " + _graphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
            _graphicsStateCollection.SendToEditor(_outputCollectionName);
        }

        private bool IsTracingMode()
        {
            return mode == GraphicsStateCollectMode.Tracing || (mode == GraphicsStateCollectMode.Auto && Application.isEditor);
        }
        
        private bool IsWarmupMode()
        {
            return mode == GraphicsStateCollectMode.WarmUp || (mode == GraphicsStateCollectMode.Auto && !Application.isEditor);
        }

        // Find the available collection file that matches the current platform and quality level.
        private GraphicsStateCollection FindExistingCollection()
        {
            foreach (var collection in collections)
            {
                if (collection)
                {
                    if (collection.runtimePlatform == Application.platform &&
                        collection.graphicsDeviceType == SystemInfo.graphicsDeviceType &&
                        collection.qualityLevelName == QualitySettings.names[QualitySettings.GetQualityLevel()])
                    {
                        return collection;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Start warmup or tracing Graphics State.
        /// </summary>
        /// <param name="onWarmupProgress"></param>
        public async UniTask StartGraphicsStateCollectAsync(Action<GraphicsStateWarmUpProgress> onWarmupProgress)
        {
            using var handle = _warmUpProgressSubject.Subscribe(onWarmupProgress);
            await StartGraphicsStateCollectAsync();
        }
        
        /// <summary>
        /// Start warmup or tracing Graphics State.
        /// </summary>
        public async UniTask StartGraphicsStateCollectAsync()
        {
            if (IsTracingMode())
            {
                // Find the existing collection file based on current settings.
                _graphicsStateCollection = FindExistingCollection();

                if (_graphicsStateCollection)
                {
                    // Use the existing file path if found.
                    _outputCollectionName = CollectionFolderPath + _graphicsStateCollection.name;
                }
                else
                {
                    // Create a new file if the file isn't found.
                    // Get the name of the current quality level.
                    int qualityLevelIndex = QualitySettings.GetQualityLevel();
                    string qualityLevelName = QualitySettings.names[qualityLevelIndex];
                    qualityLevelName = qualityLevelName.Replace(" ", "");

                    // Set up the file path to use for the output collection.
                    _outputCollectionName = string.Concat(CollectionFolderPath, "GfxState_", Application.platform,
                        "_", SystemInfo.graphicsDeviceType.ToString(), "_", qualityLevelName);

                    // Create a new GraphicsStateCollection.
                    _graphicsStateCollection = new GraphicsStateCollection();
                }

                // Start tracing PSOs.
                if (verbose) Debug.Log("Tracing started for GraphicsStateCollection.");
                _graphicsStateCollection.BeginTrace();
            }
            else
            {
                if (_isWarmed)
                {
                    if (verbose) Debug.LogWarning("GraphicsStateCollection has been warmed.");
                    return;
                }
                
                _isWarmed = true;
                // Find the existing collection file based on current settings.
                GraphicsStateCollection collection = FindExistingCollection();
                int total = collection.totalGraphicsStateCount;
                int num = 0;
                while (num < total)
                {
                    var handle = collection.WarmUpProgressively(warmUpBatchSize);
                    while (!handle.IsCompleted)
                    {
                        await UniTask.Yield();
                    }
                    _warmUpProgressSubject.OnNext(new GraphicsStateWarmUpProgress { completedCount = Mathf.Min(num, total), total = total });
                    num += warmUpBatchSize;
                }
            }
        }
    }
}
#endif