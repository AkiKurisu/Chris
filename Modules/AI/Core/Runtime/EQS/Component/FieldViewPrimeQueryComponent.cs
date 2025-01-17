using System.Collections.Generic;
using Chris.Gameplay;
using UnityEngine;
using UnityEngine.Pool;
namespace Chris.AI.EQS
{
    /// <summary>
    /// Field view prime query data provider associated with an Actor as component
    /// </summary>
    public class FieldViewPrimeQueryComponent : FieldViewQueryComponentBase
    {
        [Header("Data")]
        public FieldViewPrime fieldView = new()
        {
            radius = 20,
            angle = 120,
            sides = 8,
            blend = 0.5f
        };
        
        public LayerMask queryLayerMask;
        
        private FieldViewPrimeQuerySystem _system;
        
        [Header("Gizmos")]
        public Vector3 offset;
        
        private void Start()
        {
            _system = WorldSubsystem.GetOrCreate<FieldViewPrimeQuerySystem>();
            if (_system == null)
            {
                Debug.LogError($"[FieldViewPrimeQueryComponent] Can not get FieldViewPrimeQuerySystem dynamically.");
            }
        }
        
        public override bool RequestFieldViewQuery()
        {
            if (_system == null)
            {
                return false;
            }
            _system.EnqueueCommand(new FieldViewPrimeQueryCommand()
            {
                self = GetActor().GetActorHandle(),
                fieldView = fieldView,
                layerMask = queryLayerMask
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
            fieldView.DrawGizmos(transform.position + offset, transform.rotation);
        }
    }
}
