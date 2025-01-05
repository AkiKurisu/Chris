using Ceres.Annotations;
using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Ceres.Graph.Flow.Utilities;
using Chris.Schedulers;
using Chris.Serialization;
using UnityEngine.Scripting;
using UObject = UnityEngine.Object;
namespace Chris.Gameplay
{
    /// <summary>
    /// Executable function library for Gameplay
    /// </summary>
    [Preserve]
    public class GameplayExecutableFunctionLibrary: ExecutableFunctionLibrary
    {
        #region Scheduler

        [ExecutableFunction(IsSelfTarget = true), CeresLabel("Schedule Timer by Event")]
        public static SchedulerHandle Flow_SchedulerDelay(UObject context, 
            float delaySeconds, EventDelegate<float> onUpdate, EventDelegate onComplete)
        {
            var handle = Scheduler.Delay(delaySeconds, onUpdate: onUpdate?.Create(context), onComplete: onComplete?.Create(context));
            return handle;
        }
        
        [ExecutableFunction(IsSelfTarget = true), CeresLabel("Schedule FrameCounter by Event")]
        public static SchedulerHandle Flow_SchedulerWaitFrame(UObject context, 
            int frame, EventDelegate<int> onUpdate, EventDelegate onComplete)
        {
            var handle = Scheduler.WaitFrame(frame, onUpdate: onUpdate?.Create(context), onComplete: onComplete?.Create(context));
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
