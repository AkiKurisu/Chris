using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
namespace Kurisu.Framework.AI
{
    /// <summary>
    /// Reference article: 《Naughty Dog-Human Enemy AI In Last of The Us》.
    /// </summary>
    [Serializable]
    public struct PostQuerier
    {
        [Range(0, 360), Tooltip("Post query angle, can only see target within angle")]
        public float Angle;
        [Range(0, 500), Tooltip("Post query max distance")]
        public float Distance;
        [Range(2, 12), Tooltip("Post query iterate step, decrease step to increase performance but loss diversity")]
        public int Step;
        [Range(1, 6), Tooltip("Post query sampling depth, decrease depth to increase performance but loss precision")]
        public int Depth;
        private struct RaycastPair
        {
            public bool isHitL;
            public bool isHitR;
            public RaycastHit hitL;
            public RaycastHit hitR;
            public Vector3 left;
            public Vector3 right;
            public readonly Vector3 Half => (left + right) / 2;
        }
        private static readonly ProfilerMarker m_ProfilerMarker = new("PostQuerier.QueryPosts");
        /// <summary>
        /// Query posts in source view
        /// </summary>
        /// <param name="posts">Store results</param>
        /// <param name="source">View source transform</param>
        /// <param name="direction">View direction</param>
        /// <param name="layerMask"></param>
        /// <returns>Has post</returns>
        public readonly bool QueryPosts(List<RaycastHit> posts, Transform source, Vector3 direction, LayerMask layerMask)
        {
            using (m_ProfilerMarker.Auto())
            {
                float angleInRadians = Angle * Mathf.Deg2Rad;
                Vector3 left = Vector3.RotateTowards(direction, -source.right, angleInRadians / 2, float.MaxValue);
                Vector3 right = Vector3.RotateTowards(direction, source.right, angleInRadians / 2, float.MaxValue);
                float anglePerStep = angleInRadians / Step;
                for (int i = 0; i < Step - 1; ++i)
                {

                    RaycastPair pair = new()
                    {
                        left = Vector3.RotateTowards(left, right, i * anglePerStep, float.MaxValue),
                        right = Vector3.RotateTowards(left, right, (i + 1) * anglePerStep, float.MaxValue)
                    };
                    int d = 0;
                    RaycastHit hit = default;
                    while (d < Depth)
                    {
                        int count = DoubleRaycast(source, layerMask, ref pair, out var newHit);
                        if (count == 1)
                        {
                            d++;
                            hit = newHit;
                            // use the most closest hit
                            if (Depth == d)
                            {
                                posts.Add(hit);
                                break;
                            }
                            continue;
                        }
                        // use last hit if possible
                        if (d != 0)
                            posts.Add(hit);
                        break;
                    }
                }
            }
            return posts.Count > 0;
        }
        private readonly int DoubleRaycast(Transform from, LayerMask layer, ref RaycastPair rayPair, out RaycastHit hit)
        {
            if (!rayPair.isHitL)
                rayPair.isHitL = Raycast(from, rayPair.left, layer, out rayPair.hitL);
            if (!rayPair.isHitR)
                rayPair.isHitR = Raycast(from, rayPair.right, layer, out rayPair.hitR);
            hit = default;
            if (rayPair.isHitL && rayPair.isHitR)
            {
                return 2;
            }
            if (rayPair.isHitL && !rayPair.isHitR)
            {
                hit = rayPair.hitL;
                rayPair.left = rayPair.Half;
                rayPair.isHitL = false;
                return 1;
            }
            if (!rayPair.isHitL && rayPair.isHitR)
            {
                hit = rayPair.hitR;
                rayPair.right = rayPair.Half;
                rayPair.isHitR = false;
                return 1;
            }
            return 0;
        }
        private readonly bool Raycast(Transform from, Vector3 direction, LayerMask layer, out RaycastHit hit)
        {
            return Physics.Raycast(from.position, direction, out hit, Distance, layer);
        }
    }
}