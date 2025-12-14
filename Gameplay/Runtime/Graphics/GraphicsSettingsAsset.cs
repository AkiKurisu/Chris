using System;
using Chris.Serialization;
using UnityEngine;

namespace Chris.Gameplay.Graphics
{
    [Flags]
    public enum GraphicsFeatures
    {
        None = 0,
        ScreenSpaceReflection = 1 << 0,
        ScreenSpaceGlobalIllumination = 1 << 1,
        ScreenSpaceAmbientOcclusion = 1 << 2,
        DepthOfField = 1 << 3,
        MotionBlur = 1 << 4
    }
    
    [CreateAssetMenu(fileName = "GraphicsConfig", menuName = "Chris/Graphics/GraphicsSettingsAsset")]
    public class GraphicsSettingsAsset : ScriptableObject
    {
        // Camera Settings
        [Header("Camera Settings")]
        public float fieldOfView = 23;
            
        public float nearClipPlane = 0.1f;
            
        public float farClipPlane = 5000;

        // Quality Settings
        [Header("Quality Settings")]
        public int[] frameRateOptions = { 30, 60, -1 };

        // Extra Settings
        [Header("Extra Settings")]
        public SerializedType<GraphicsModule>[] graphicsModules;

        [SerializeField]
        private GraphicsFeatures disableFeatures;

        public bool IsFeatureSupport(GraphicsFeatures features)
        {
            return (disableFeatures & features) == 0;
        }
    }
}