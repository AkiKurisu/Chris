using Ceres.Graph;
using Chris.Gameplay.Animations;
using Chris.Schedulers;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
namespace Chris.Gameplay.Flow.Utilities
{
    public class GameplaySetup
    {
        [RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static unsafe void InitializeOnLoad()
        {
            /* Register port implicit conversation */
            // ========================= Animation =========================== //
            CeresPort<LayerHandle>.MakeCompatibleTo<int>(handle => handle.Id);
            CeresPort<int>.MakeCompatibleTo<LayerHandle>(d => new LayerHandle(d));
            CeresPort<string>.MakeCompatibleTo<LayerHandle>(str => new LayerHandle(str));
            // ========================= Animation =========================== //
            
            // ========================= Scheduler =========================== //
            CeresPort<SchedulerHandle>.MakeCompatibleTo<double>(handle =>
            {
                double value = default;
                UnsafeUtility.CopyStructureToPtr(ref handle, &value);
                return value;
            });
            CeresPort<double>.MakeCompatibleTo<SchedulerHandle>(d =>
            {
                SchedulerHandle handle = default;
                UnsafeUtility.CopyStructureToPtr(ref d, &handle);
                return handle;
            });
            // ========================= Scheduler =========================== //
        }
    }
}