using Ceres.Annotations;
using Ceres.Graph;
using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Ceres.Graph.Flow.Utilities;
using Chris.Schedulers;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;
namespace Chris.Gameplay.Flow.Utilities
{
    /// <summary>
    /// Executable function library for Chris.Schedulers
    /// </summary>
    [Preserve]
    public class SchedulerExecutableLibrary: ExecutableFunctionLibrary
    {
        [RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static unsafe void InitializeOnLoad()
        {
            /* Implicit conversation */
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
        }
        
        #region Scheduler

        [ExecutableFunction, CeresLabel("Schedule Timer by Event")]
        public static SchedulerHandle Flow_SchedulerDelay(
            float delaySeconds, EventDelegate onComplete, EventDelegate<float> onUpdate, 
            TickFrame tickFrame, bool isLooped, bool ignoreTimeScale)
        {
            var handle = Scheduler.Delay(delaySeconds,onComplete,onUpdate,
                tickFrame, isLooped, ignoreTimeScale);
            return handle;
        }
        
        [ExecutableFunction, CeresLabel("Schedule FrameCounter by Event")]
        public static SchedulerHandle Flow_SchedulerWaitFrame(
            int frame, EventDelegate onComplete, EventDelegate<int> onUpdate,
            TickFrame tickFrame, bool isLooped)
        {
            var handle = Scheduler.WaitFrame(frame, onComplete, onUpdate, tickFrame, isLooped);
            return handle;
        }
        
        [ExecutableFunction(IsScriptMethod = true, DisplayTarget = false), CeresLabel("Cancel Scheduler")]
        public static void Flow_SchedulerHandleCancel(SchedulerHandle handle)
        {
            handle.Cancel();
        }
        
        #endregion Scheduler
    }
}