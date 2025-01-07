using Ceres.Annotations;
using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Ceres.Graph.Flow.Utilities;
using Chris.Schedulers;
using Chris.Serialization;
using UnityEngine.Scripting;
namespace Chris.Gameplay
{
    /// <summary>
    /// Executable function library for Gameplay
    /// </summary>
    [Preserve]
    public class GameplayExecutableFunctionLibrary: ExecutableFunctionLibrary
    {
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

        #region Subsystem

        [ExecutableFunction]
        public static SubsystemBase Flow_GetSubsystem(
            [CeresMetadata(ExecutableFunction.RESOLVE_RETURN)] SerializedType<SubsystemBase> type)
        {
            return GameWorld.Get().GetSubsystem(type);
        }
        
        [ExecutableFunction]
        public static SubsystemBase Flow_GetOrCreateSubsystem(
            [CeresMetadata(ExecutableFunction.RESOLVE_RETURN)] SerializedType<SubsystemBase> type)
        {
            return WorldSubsystem.GetOrCreate(type);
        }

        #endregion
    }
}
