using System;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UGraphics = UnityEngine.Graphics;

namespace Chris.Gameplay.Capture
{
    public enum ScreenshotMode
    {
        /// <summary>
        /// Take raw screenshot from camera.
        /// </summary>
        Camera,
        /// <summary>
        /// Take screenshot from current screen color buffer.
        /// </summary>
        Screen
    }
    
    public struct ScreenshotRequest
    {
        /// <summary>
        /// Whether capture raw frame from renderer
        /// </summary>
        public ScreenshotMode Mode;
        
        /// <summary>
        /// Capture camera used when enable <see cref="ScreenshotMode.Camera"/>
        /// </summary>
        public Camera Camera;

        /// <summary>
        /// Define capture destination
        /// </summary>
        public RenderTexture Destination;
    }
    
    public static class ScreenshotUtility
    {
        [BurstCompile]
        private struct LinearToGammaConvertJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<float> Data;

            public void Execute(int index)
            {
                Data[index] = Mathf.LinearToGammaSpace(Data[index]);
            }
        }
        
        private static void CameraScreenshot(RenderTexture renderTarget, Camera camera)
        {
            camera.targetTexture = renderTarget;
            camera.Render();
            camera.targetTexture = null;
        }
        
        private static void CaptureScreenshot(RenderTexture renderTarget)
        {
            var screenSize = GameViewUtils.GetSizeOfMainGameView();
            RenderTexture cameraTarget = RenderTexture.GetTemporary((int)screenSize.x, (int)screenSize.y, 
                0, RenderTextureFormat.ARGB32);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(cameraTarget);
            UGraphics.Blit(cameraTarget, renderTarget, new Vector2(1f, -1f), new Vector2(0.0f, 1f)); // Flip in DX12 and Vulkan
            RenderTexture.ReleaseTemporary(cameraTarget);
        }

        public static RenderTexture CaptureScreenshot(ScreenshotRequest request)
        {
            var destination = request.Destination;
            Assert.IsTrue((bool)destination);
            if (request.Mode == ScreenshotMode.Camera)
            {
                CameraScreenshot(destination, request.Camera);
            }
            else
            {
                CaptureScreenshot(destination);
            }

            return destination;
        }

        public static Texture2D CaptureActiveRenderTexture(int width, int height, TextureFormat format = TextureFormat.RGBA32)
        {
            Texture2D destination = new Texture2D(width, height, format, false);
            Rect rect = new Rect(0, 0, width, height);
            destination.ReadPixels(rect, 0, 0, false);
            return destination;
        }

        private static bool IsHDR(RenderTextureFormat renderTextureFormat)
        {
            return renderTextureFormat is RenderTextureFormat.ARGBHalf or RenderTextureFormat.ARGBFloat;
        }

        public static TextureFormat GetTextureFormat(RenderTextureFormat renderTextureFormat)
        {
            if (renderTextureFormat == RenderTextureFormat.ARGBHalf)
            {
                return TextureFormat.RGBAHalf;
            }
            
            if (renderTextureFormat == RenderTextureFormat.ARGBFloat)
            {
                return TextureFormat.RGBAFloat;
            }

            return TextureFormat.ARGB32;
        }

        private static void LinearToGamma(Texture2D texture2D)
        {
            var rawData = texture2D.GetRawTextureData<float>();
            var job = new LinearToGammaConvertJob
            {
                Data = rawData
            };
            JobHandle handle = job.Schedule(rawData.Length, 64);
            handle.Complete();
        }
        
        private static async UniTask LinearToGammaAsync(Texture2D texture2D)
        {
            var rawData = texture2D.GetRawTextureData<float>();
            var job = new LinearToGammaConvertJob
            {
                Data = rawData
            };
            var handle = job.Schedule(rawData.Length, 64);
            await handle.ToUniTask(PlayerLoopTiming.LastPostLateUpdate);
            handle.Complete();
        }
        
        public static Texture2D ToTexture2D(this RenderTexture renderTexture)
        {
            RenderTexture original = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var destination = CaptureActiveRenderTexture(renderTexture.width, renderTexture.height, GetTextureFormat(renderTexture.format));
            RenderTexture.active = original;
            if (IsHDR(renderTexture.format))
            {
                LinearToGamma(destination);
            }
            return destination;
        }
        
        public static void ToTexture2DAsync(this RenderTexture renderTexture, Action<Texture2D> callback)
        {
            Assert.IsNotNull(callback);
            var textureFormat = GetTextureFormat(renderTexture.format);
            var destination = new Texture2D(renderTexture.width, renderTexture.height, textureFormat, false);

            AsyncGPUReadback.Request(renderTexture, 0, 
                0, destination.width, 0, destination.height, 0, 1, textureFormat, request => ReadbackAsync(request).Forget());
            return;

            async UniTask ReadbackAsync(AsyncGPUReadbackRequest request)
            {
                var rawData = request.GetData<byte>();
                var processedData = destination.GetRawTextureData<byte>();
                var slice = new NativeSlice<byte>(processedData, 0, rawData.Length);
                slice.CopyFrom(rawData);
                if (IsHDR(renderTexture.format))
                {
                    await LinearToGammaAsync(destination);
                }
                callback.Invoke(destination);
            }
        }
    }
}