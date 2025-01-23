using System;
using System.Collections.Generic;
using Chris.Gameplay;
using UnityEngine;
using UnityEngine.Pool;
namespace Chris.AI.EQS
{
    /// <summary>
    /// Represents a field of view for AI. 
    /// </summary>
    [Serializable]
    public struct FieldView
    {
        [Range(0, 500), Tooltip("Field of view radius, ai can only sensor new target within this radius")]
        public float radius;
        [Range(0, 360), Tooltip("Field of view angle, ai can only see target within angle")]
        public float angle;
        public FieldView(float radius, float angle)
        {
            this.radius = radius;
            this.angle = angle;
        }
        
        /// <summary>
        /// Collect actors in field of view
        /// </summary>
        /// <param name="actors"></param>
        /// <param name="position"></param>
        /// <param name="forward"></param>
        /// <param name="targetMask"></param>
        /// <param name="actorToIgnore"></param>
        public readonly void CollectViewActors(List<Actor> actors, Vector3 position, Vector3 forward, LayerMask targetMask, Actor actorToIgnore = null)
        {
            EnvironmentQuery.OverlapFieldView(actors, position, forward, radius, angle, targetMask, actorToIgnore);
        }
        
        /// <summary>
        /// Collect actors in field of view with generic type filter
        /// </summary>
        /// <param name="actors"></param>
        /// <param name="position"></param>
        /// <param name="forward"></param>
        /// <param name="targetMask"></param>
        /// <param name="actorToIgnore"></param>
        public readonly void CollectViewActors<T>(List<T> actors, Vector3 position, Vector3 forward, LayerMask targetMask, T actorToIgnore = null) where T : Actor
        {
            var list = ListPool<Actor>.Get();
            EnvironmentQuery.OverlapFieldView(list, position, forward, radius, angle, targetMask, actorToIgnore);
            foreach (var actor in list)
            {
                if (actor is T tActor) actors.Add(tActor);
            }
            ListPool<Actor>.Release(list);
        }

        /// <summary>
        /// Detect whether field view can see the target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fromPosition"></param>
        /// <param name="fromRotation"></param>
        /// <param name="layerMask"></param>
        /// <param name="filterTags"></param>
        /// <returns></returns>
        public readonly bool Detect(Vector3 target, Vector3 fromPosition, Quaternion fromRotation, LayerMask layerMask, string[] filterTags = null)
        {
            bool isVisible = true;
            Vector3 viewDirection = fromRotation * Vector3.forward;
            Vector3 directionToTarget = (target - fromPosition).normalized;

            if (Vector3.Angle(viewDirection, directionToTarget) > angle / 2)
            {
                isVisible = false;
            }
            else
            {
                // Raycast detect, ignore height
                float normalDistance = Vector3.Distance(new Vector3(fromPosition.x, 0, fromPosition.z), new Vector3(target.x, 0, target.z));
                if (normalDistance > radius)
                {
                    return false;
                }
                Physics.Linecast(fromPosition, target, out RaycastHit hit, layerMask);
                if (hit.collider != null)
                {
                    if (FrameworkUtils.CompareTags(hit.collider, filterTags) == false)
                    {
                        Debug.DrawLine(hit.point, fromPosition, Color.cyan);
                        isVisible = false;
                    }
                }
            }
            return isVisible;
        }
        
        public readonly void DrawGizmos(Vector3 position, Vector3 forward)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(position, Vector3.up, radius);

            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireArc(position, Vector3.up, forward, angle / 2, radius - 0.1f);
            UnityEditor.Handles.DrawWireArc(position, Vector3.up, forward, -angle / 2, radius - 0.1f);

            UnityEditor.Handles.color = new Color(1, 0, 0, 0.1f);
            UnityEditor.Handles.DrawSolidArc(position, Vector3.up, forward, angle / 2, radius - 0.2f);
            UnityEditor.Handles.DrawSolidArc(position, Vector3.up, forward, -angle / 2, radius - 0.2f);
#endif
        }
    }
}