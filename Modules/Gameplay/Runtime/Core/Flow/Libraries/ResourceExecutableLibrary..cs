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
        [ExecutableFunction]
        public static UObject Flow_LoadAssetSynchronous(GameObject gameObject, string address)
        {
            var handle = ResourceSystem.LoadAssetAsync<UObject>(address);
            handle.AddTo(gameObject);
            return handle.WaitForCompletion();
        }
        
        [ExecutableFunction]
        public static void Flow_LoadAssetAsync(GameObject gameObject, string address, EventDelegate<UObject> onComplete)
        {
            ResourceSystem.LoadAssetAsync<UObject>(address, onComplete).AddTo(gameObject);
        }
        
        [ExecutableFunction]
        public static UObject Flow_LoadAssetSynchronous(Component component, string address)
        {
            var handle = ResourceSystem.LoadAssetAsync<UObject>(address);
            handle.AddTo(component);
            return handle.WaitForCompletion();
        }
        
        [ExecutableFunction]
        public static void Flow_LoadAssetAsync(Component component, string address, EventDelegate<UObject> onComplete)
        {
            ResourceSystem.LoadAssetAsync<UObject>(address, onComplete).AddTo(component);
        }
    }
}