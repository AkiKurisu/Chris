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
    public class ResourceExecutableLibrary: ExecutableFunctionLibrary
    {
        [ExecutableFunction(IsScriptMethod = true, IsSelfTarget = true), CeresLabel("Load Asset Synchronous")]
        public static UObject Flow_GameObjectLoadAssetSynchronous([HideInGraphEditor] GameObject gameObject, string address)
        {
            return ResourceSystem.LoadAssetAsync<UObject>(address).AddTo(gameObject).WaitForCompletion();
        }
        
        [ExecutableFunction(IsScriptMethod = true, IsSelfTarget = true), CeresLabel("Load Asset Async")]
        public static void Flow_GameObjectLoadAssetAsync([HideInGraphEditor] GameObject gameObject, string address, EventDelegate<UObject> onComplete)
        {
            ResourceSystem.LoadAssetAsync<UObject>(address, onComplete).AddTo(gameObject);
        }
        
        [ExecutableFunction(IsScriptMethod = true, IsSelfTarget = true), CeresLabel("Load Asset Synchronous")]
        public static UObject Flow_ComponentLoadAssetSynchronous([HideInGraphEditor] Component component, string address)
        {
            return ResourceSystem.LoadAssetAsync<UObject>(address).AddTo(component).WaitForCompletion();
        }
        
        [ExecutableFunction(IsScriptMethod = true, IsSelfTarget = true), CeresLabel("Load Asset Async")]
        public static void Flow_ComponentLoadAssetAsync([HideInGraphEditor] Component component, string address, EventDelegate<UObject> onComplete)
        {
            ResourceSystem.LoadAssetAsync<UObject>(address, onComplete).AddTo(component);
        }
    }
}