using System.Collections.Generic;
using Chris.Gameplay;
using UnityEngine;
using UnityEngine.Pool;
namespace Chris.AI.EQS
{
    public interface IFieldViewQueryComponent
    {
        /// <summary>
        /// Request a new query from target's field view
        /// </summary>
        /// <returns></returns>
        bool RequestFieldViewQuery();

        /// <summary>
        /// Detect whether can see the target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fromPosition"></param>
        /// <param name="fromRotation"></param>
        /// <param name="filterTags"></param>
        /// <returns></returns>
        bool Detect(Vector3 target, Vector3 fromPosition, Quaternion fromRotation, string[] filterTags = null);
        
        /// <summary>
        /// Query actors overlap in field of view from cache
        /// </summary>
        /// <param name="actors"></param>
        void CollectViewActors(List<Actor> actors);
        
        /// <summary>
        /// Query actors overlap in field of view from cache
        /// </summary>
        /// <param name="actors"></param>
        void CollectViewActors<T>(List<T> actors) where T : Actor;
    }
    
    public abstract class FieldViewQueryComponentBase : ActorComponent, IFieldViewQueryComponent
    {
        public abstract void CollectViewActors(List<Actor> actors);
        
        public abstract void CollectViewActors<T>(List<T> actors) where T : Actor;
        
        public abstract bool Detect(Vector3 target, Vector3 fromPosition, Quaternion fromRotation, string[] filterTags = null);
        
        public abstract bool RequestFieldViewQuery();
    }
    
    /// <summary>
    /// Field view query data provider associated with an Actor as component
    /// </summary>
    public class FieldViewQueryComponent : FieldViewQueryComponentBase
    {
        [Header("Data")]
        public FieldView fieldView = new()
        {
            radius = 20,
            angle = 120
        };
        
        public LayerMask queryLayerMask;
        
        private FieldViewQuerySystem _system;
        
        [Header("Gizmos")]
        public Vector3 offset;
        
        private void Start()
        {
            _system = WorldSubsystem.GetOrCreate<FieldViewQuerySystem>();
            if (_system == null)
            {
                Debug.LogError($"[FieldViewQueryComponent] Can not get FieldViewQuerySystem dynamically.");
            }
        }
        public override bool RequestFieldViewQuery()
        {
            if (_system == null)
            {
                return false;
            }
            _system.EnqueueCommand(new FieldViewQueryCommand()
            {
                Self = GetActor().GetActorHandle(),
                FieldView = fieldView,
                LayerMask = queryLayerMask
            });
            return true;
        }
        
        public override void CollectViewActors(List<Actor> actors)
        {
            _system.GetActorsInFieldView(GetActor().GetActorHandle(), actors);
        }
        
        public override void CollectViewActors<T>(List<T> actors)
        {
            var list = ListPool<Actor>.Get();
            CollectViewActors(list);
            foreach (var actor in list)
            {
                if (actor is T tActor) actors.Add(tActor);
            }
            ListPool<Actor>.Release(list);
        }
        
        public override bool Detect(Vector3 target, Vector3 fromPosition, Quaternion fromRotation, string[] filterTags = null)
        {
            return fieldView.Detect(target, fromPosition, fromRotation, queryLayerMask, filterTags);
        }
        
        private void OnDrawGizmos()
        {
            fieldView.DrawGizmos(transform.position + offset, transform.forward);
        }
    }
}
