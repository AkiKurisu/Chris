using Ceres.Annotations;
using Ceres.Graph;
using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Ceres.Graph.Flow.Utilities;
using Chris.Gameplay.Animations;
using Chris.Serialization;
using UnityEngine;
using UnityEngine.Scripting;
namespace Chris.Gameplay.Flow.Utilities
{
    /// <summary>
    /// Executable function library for Gameplay
    /// </summary>
    [Preserve]
    public class GameplayExecutableLibrary: ExecutableFunctionLibrary
    {
        [RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void InitializeOnLoad()
        {
            /* Implicit conversation */
            CeresPort<LayerHandle>.MakeCompatibleTo<int>(handle => handle.Id);
            CeresPort<int>.MakeCompatibleTo<LayerHandle>(d => new LayerHandle(d));
            CeresPort<string>.MakeCompatibleTo<LayerHandle>(str => new LayerHandle(str));
        }
        
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
