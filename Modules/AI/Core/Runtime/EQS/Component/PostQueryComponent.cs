using System;
using Unity.Mathematics;
using UnityEngine;
namespace Chris.AI.EQS
{
    /// <summary>
    /// Post Query data provider associated with an Actor as component
    /// </summary>
    public class PostQueryComponent : ActorComponent
    {
        [Header("Data")]
        public PostQueryParameters PostQuery = new()
        {
            Angle = 120,
            Distance = 30,
            Step = 6,
            Depth = 3
        };
        public LayerMask RaycastLayerMask;
        public Vector3 RaycastOffset;
        private PostQuerySystem system;
        private void Start()
        {
            system = WorldSubsystem.Get<PostQuerySystem>();
            if (system == null)
            {
                Debug.LogError($"[PostQueryComponent] Can not get PostQuerySystem dynamically.");
            }
        }
        /// <summary>
        /// Requst a new post query from target's view
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool RequestPostQuery(Actor target)
        {
            if (system == null)
            {
                return false;
            }
            if (target == GetActor())
            {
                Debug.LogWarning($"[PostQueryComponent] Can not request post query from self view.");
                return false;
            }
            if (!system.IsFree(GetActor().GetActorHandle()))
            {
                return false;
            }
            PostQueryCommand command = new()
            {
                self = GetActor().GetActorHandle(),
                target = target.GetActorHandle(),
                parameters = PostQuery,
                offset = RaycastOffset,
                layerMask = RaycastLayerMask
            };
            system.EnqueueCommand(command);
            return true;
        }
        /// <summary>
        /// Get current posts
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<float3> GetPosts()
        {
            return system.GetPosts(GetActor().GetActorHandle()).AsReadOnlySpan();
        }
    }
}
