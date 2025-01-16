using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
namespace Chris
{
    public static class FrameworkUtils
    {
        #region Public API
        public static Vector3 GetScreenPosition(float width, float height, Vector3 target)
        {
            return GetScreenPosition(Camera.main, width, height, target);
        }
        public static Vector3 GetScreenPosition(Camera camera, float width, float height, Vector3 target)
        {
            Vector3 pos = camera.WorldToScreenPoint(target);
            pos.x *= width / Screen.width;
            pos.y *= height / Screen.height;
            pos.x -= width * 0.5f;
            pos.y -= height * 0.5f;
            return pos;
        }
        /// <summary>
        /// Quadratic Bezier curve, dynamically draw a curve based on three points
        /// </summary>
        /// <param name="t">The arrival coefficient is 0 for the start and 1 for the arrival</param>
        /// <param name="p0">Starting point</param>
        /// <param name="p1">Middle point</param>
        /// <param name="p2">End point</param>
        /// <returns></returns>
        public static Vector2 GetQuadraticCurvePoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            return (uu * p0) + (2 * u * t * p1) + (tt * p2);
        }
        public static bool CompareTags(Component target, string[] allowedTags)
        {
            if (target == null || allowedTags == null) return false;

            bool match = false;
            foreach (string tag in allowedTags)
            {
                if (target.CompareTag(tag)) match = true;
            }
            return match;
        }
        public static string GetRelativePath(string path)
        {
            return path.Replace("\\", "/").Replace(Application.dataPath, "Assets/");
        }
        public static string GetAbsolutePath(string path)
        {
            return Application.dataPath + path.Replace("\\", "/")[6..];
        }
        #endregion Public API

        #region Internal API
        internal static MethodInfo GetStaticMethodWithNoParametersInBase(Type type, string methodName)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var method in methods)
            {
                if (method.Name == methodName && method.GetParameters().Length == 0)
                {
                    return method;
                }
            }

            return null;
        }
        
        internal static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (!component) component = gameObject.AddComponent<T>();
            return component;
        }
        
        internal static StackFrame GetCurrentStackFrame()
        {
            StackTrace stackTrace = new(1, true);
            int frameId = 0;

            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                MethodBase method = stackTrace.GetFrame(i).GetMethod();
                if (method.GetCustomAttribute<StackTraceFrameAttribute>() != null)
                {
                    frameId = i;
                }
            }

            if (frameId < stackTrace.FrameCount - 1) ++frameId;
            return stackTrace.GetFrame(frameId);
        }
        
        internal static string GetDelegatePath(Delegate callback)
        {
            var declType = callback.Method.DeclaringType?.Name ?? string.Empty;
            string itemName = $"{declType}.{callback.Method.Name}";
            if (callback.Target != null)
            {
                string objectName = callback.Target.ToString();
                itemName = $"{itemName}>[{objectName}]";
            };
            return itemName;
        }
        #endregion Internal API
    }
}
