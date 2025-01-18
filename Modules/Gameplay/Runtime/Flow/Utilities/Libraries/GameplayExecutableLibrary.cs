using Ceres.Annotations;
using Ceres.Graph.Flow.Annotations;
using Ceres.Graph.Flow.Utilities;
using Chris.Gameplay.Audios;
using Chris.Gameplay.FX;
using Chris.Serialization;
using UnityEngine;
using UnityEngine.Scripting;
namespace Chris.Gameplay.Flow.Utilities
{
    /// <summary>
    /// Executable function library for Gameplay
    /// </summary>
    [Preserve]
    [CeresGroup("Gameplay")]
    public class GameplayExecutableLibrary: ExecutableFunctionLibrary
    {
        #region Subsystem

        [ExecutableFunction]
        public static SubsystemBase Flow_GetSubsystem(
            [ResolveReturn] SerializedType<SubsystemBase> type)
        {
            return GameWorld.Get().GetSubsystem(type);
        }
        
        [ExecutableFunction]
        public static SubsystemBase Flow_GetOrCreateSubsystem(
            [ResolveReturn] SerializedType<SubsystemBase> type)
        {
            return WorldSubsystem.GetOrCreate(type);
        }
        
        #endregion

        #region Audios

        [ExecutableFunction, CeresLabel("Play 2D AudioClip")]
        public static void Flow_Play2DAudioClip(AudioClip audioClip, float volume = 1f)
        {
            AudioSystem.PlayClipAtPoint(audioClip, default, volume, 0);
        }
        
        [ExecutableFunction, CeresLabel("Play 2D AudioClip from Address")]
        public static void Flow_Play2DAudioClipFromAddress(string audioClipAddress, float volume = 1f)
        {
            AudioSystem.PlayClipAtPoint(audioClipAddress, default, volume, 0);
        }
        
        [ExecutableFunction, CeresLabel("Play 3D AudioClip")]
        public static void Flow_Play3DAudioClip(AudioClip audioClip, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            AudioSystem.PlayClipAtPoint(audioClip, position, volume, spatialBlend, minDistance);
        }
        
        [ExecutableFunction, CeresLabel("Play 3D AudioClip from Address")]
        public static void Flow_Play3DAudioClipFromAddress(string audioClipAddress, Vector3 position, float volume = 1f, float spatialBlend = 1f, float minDistance = 10f)
        {
            AudioSystem.PlayClipAtPoint(audioClipAddress, position, volume, spatialBlend, minDistance);
        }

        #endregion

        #region FX
        
        [ExecutableFunction, CeresLabel("Play ParticleSystem")]
        public static void Flow_PlayParticleSystem(GameObject prefab, Vector3 position, Vector3 eulerAngle, Transform parent = null, bool useLocalPosition = false)
        {
            FXSystem.PlayFX(prefab, position, Quaternion.Euler(eulerAngle), parent, useLocalPosition);
        }

        [ExecutableFunction, CeresLabel("Play ParticleSystem from Address")]
        public static void Flow_PlayParticleSystemFromAddress(string prefabAddress, Vector3 position, Vector3 eulerAngle, Transform parent = null, bool useLocalPosition = false)
        {
            FXSystem.PlayFX(prefabAddress, position, Quaternion.Euler(eulerAngle), parent, useLocalPosition);
        }
        
        #endregion
    }
}
