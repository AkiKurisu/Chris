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

        /// <summary>
        /// Define capture delay frames, ignored when using synchronization methods
        /// </summary>
        public int DelayFrames;
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
        
        public abstract class ScreenshotHandler : IDisposable
        {
            protected RenderTexture RenderTarget { get; }

            protected ScreenshotHandler(RenderTexture renderTarget)
            {
                RenderTarget = renderTarget;
            }
            
            public abstract void Execute();

            public abstract void Dispose();
        }

        private sealed class CameraScreenshotHandler : ScreenshotHandler
        {
            private readonly Camera _camera;
            
            private readonly bool _async;

            public CameraScreenshotHandler(Camera camera, RenderTexture destination, bool async)
                : base(destination)
            {
                _camera = camera;
                _async = async;
            }

            public override void Execute()
            {
                _camera.targetTexture = RenderTarget;
                if (!_async)
                {
                    _camera.Render();
                }
            }

            public override void Dispose()
            {
                _camera.targetTexture = null;
            }
        }

        private sealed class ScreenScreenshotHandler : ScreenshotHandler
        {
            private RenderTexture _scratch;
            
            private bool _disposed;

            public ScreenScreenshotHandler(RenderTexture destination): base(destination)
            {

            }

            public override void Execute()
            {
                var screenSize = GameViewUtils.GetSizeOfMainGameView();
                _scratch = RenderTexture.GetTemporary((int)screenSize.x, (int)screenSize.y,
                    0, RenderTextureFormat.ARGB32);
                ScreenCapture.CaptureScreenshotIntoRenderTexture(_scratch);
                UGraphics.Blit(_scratch, RenderTarget, new Vector2(1f, -1f), new Vector2(0.0f, 1f)); // Flip in DX12 and Vulkan
            }

            public override void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (_scratch)
                {
                    RenderTexture.ReleaseTemporary(_scratch);
                    _scratch = null;
                }
            }
        }

        private static ScreenshotHandler CreateHandler(ScreenshotRequest request, bool async)
        {
            if (request.Mode == ScreenshotMode.Camera)
            {
                return new CameraScreenshotHandler(request.Camera, request.Destination, async);
            }

            return new ScreenScreenshotHandler(request.Destination);
        }
        
        public static ScreenshotHandler CaptureScreenshot(ScreenshotRequest request)
        {
            var destination = request.Destination;
            Assert.IsTrue((bool)destination);
            var handler = CreateHandler(request, false);
            handler.Execute();
            return handler;
        }
        
        public static async UniTask<ScreenshotHandler> CaptureScreenshotAsync(ScreenshotRequest request)
        {
            var destination = request.Destination;
            Assert.IsTrue((bool)destination);
            var handler = CreateHandler(request, true);
            handler.Execute();
            await UniTask.WaitForEndOfFrame();
            await UniTask.DelayFrame(request.DelayFrames, PlayerLoopTiming.PostLateUpdate);
            return handler;
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
        
                
        /// <summary>
        /// Capture raw screenshot from renderer to a new <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="size"></param>
        /// <param name="depthBuffer"></param>
        /// <param name="renderTextureFormat"></param>
        /// <returns></returns>
        public static Texture2D CaptureRawScreenshot(Camera camera, Vector2 size, 
            int depthBuffer = 24, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32)
        {
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            var screenTexture = RenderTexture.GetTemporary((int)size.x, (int)size.y, 
                depthBuffer, renderTextureFormat, RenderTextureReadWrite.Default, antiAliasing);
            var handler = CaptureScreenshot(new ScreenshotRequest
            {
                Camera = camera,
                Destination = screenTexture,
                Mode = ScreenshotMode.Camera
            });
            try
            {
                return screenTexture.ToTexture2D();
            }
            finally
            {
                handler.Dispose();
                RenderTexture.ReleaseTemporary(screenTexture);
            }
        }

        /// <summary>
        /// Capture raw screenshot from renderer to a new <see cref="Texture2D"/> in async.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="size"></param>
        /// <param name="depthBuffer"></param>
        /// <param name="renderTextureFormat"></param>
        /// <param name="delayFrames"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public static async UniTask CaptureRawScreenshotAsync(Camera camera, Vector2 size, 
            int depthBuffer = 24, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32, int delayFrames = 1, Action<Texture2D> onComplete = null)
        {
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            var screenTexture = RenderTexture.GetTemporary((int)size.x, (int)size.y, 
                depthBuffer, renderTextureFormat, RenderTextureReadWrite.Default, antiAliasing);
            var handler = await CaptureScreenshotAsync(new ScreenshotRequest
            {
                Camera = camera,
                Destination = screenTexture,
                Mode = ScreenshotMode.Camera,
                DelayFrames = delayFrames
            });
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                try
                {
                    var result = screenTexture.ToTexture2D();
                    onComplete?.Invoke(result);
                }
                finally
                {
                    handler.Dispose();
                    RenderTexture.ReleaseTemporary(screenTexture);
                }
                return;
            }
#endif
            screenTexture.ToTexture2DAsync(result =>
            {
                onComplete?.Invoke(result);
                handler.Dispose();
                RenderTexture.ReleaseTemporary(screenTexture);
            });
        }
        
        /// <summary>
        /// Capture screenshot to a new <see cref="Texture2D"/>. Need be called at the end of frame.
        /// </summary>
        /// <returns></returns>
        public static Texture2D CaptureScreenShotFromScreen()
        {
            var screenSize = GameViewUtils.GetSizeOfMainGameView();
#if UNITY_EDITOR
            return CaptureActiveRenderTexture((int)screenSize.x, (int)screenSize.y);
#else
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            var screenTexture = RenderTexture.GetTemporary((int)screenSize.x, (int)screenSize.y, 
                24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, antiAliasing);
            var handler = CaptureScreenshot(new ScreenshotRequest
            {
                Destination = screenTexture,
                Mode = ScreenshotMode.Screen
            });
            try
            {
                return screenTexture.ToTexture2D();
            }
            finally
            {
                handler.Dispose();
                RenderTexture.ReleaseTemporary(screenTexture);
            }
#endif
        }
        
        /// <summary>
        /// Capture screenshot to a new <see cref="Texture2D"/> in async. Need be called at the end of frame.
        /// </summary>
        /// <param name="onComplete"></param>
        public static async UniTask CaptureScreenshotAsync(Action<Texture2D> onComplete)
        {
            Assert.IsNotNull(onComplete);
            var screenSize = GameViewUtils.GetSizeOfMainGameView();
#if UNITY_EDITOR
            onComplete(CaptureActiveRenderTexture((int)screenSize.x, (int)screenSize.y));
            await UniTask.Yield();
#else
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            var screenTexture = RenderTexture.GetTemporary((int)screenSize.x, (int)screenSize.y, 
                24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, antiAliasing);
            var handler = await CaptureScreenshotAsync(new ScreenshotRequest
            {
                Destination = screenTexture,
                Mode = ScreenshotMode.Screen
            });
            screenTexture.ToTexture2DAsync(result =>
            {
                onComplete.Invoke(result);
                handler.Dispose();
                RenderTexture.ReleaseTemporary(screenTexture);
            });
#endif
        }
    }
}