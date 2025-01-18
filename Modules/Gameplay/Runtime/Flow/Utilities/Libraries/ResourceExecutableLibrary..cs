using Ceres.Annotations;
using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Ceres.Graph.Flow.Utilities;
using Chris.Resource;
using UnityEngine;
using UnityEngine.Scripting;
using UObject = UnityEngine.Object;
using R3;
namespace Chris.Gameplay.Flow.Utilities
{
    /// <summary>
    /// Executable function library for Chris.Resource
    /// </summary>
    [Preserve]
    [CeresGroup("Resource")]
    public class ResourceExecutableLibrary: ExecutableFunctionLibrary
    {
        [ExecutableFunction, CeresLabel("Load Asset Synchronous")]
        public static UObject Flow_LoadAssetSynchronous(string address)
        {
            return ResourceSystem.LoadAssetAsync<UObject>(address).AddTo(GameWorld.Get()).WaitForCompletion();
        }
        
        [ExecutableFunction, CeresLabel("Load Asset Async")]
        public static void Flow_LoadAssetAsync(string address, EventDelegate<UObject> onComplete)
        {
            ResourceSystem.LoadAssetAsync<UObject>(address, onComplete).AddTo(GameWorld.Get());
        }
        
        [ExecutableFunction, CeresLabel("Instantiate Synchronous")]
        public static GameObject Flow_InstantiateAsync(string address, Transform parent)
        {
           return ResourceSystem.InstantiateAsync(address, parent).AddTo(GameWorld.Get()).WaitForCompletion();
        }
        
        [ExecutableFunction, CeresLabel("Instantiate Async")]
        public static void Flow_InstantiateAsync(string address, Transform parent, EventDelegate<GameObject> onComplete)
        {
            ResourceSystem.InstantiateAsync(address, parent, onComplete).AddTo(GameWorld.Get());
        }
    }
}