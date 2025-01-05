using System;
using Ceres.Graph.Flow.Annotations;
using Chris.Collections;
using R3;
using UnityEngine;
using UnityEngine.Assertions;
namespace Chris.Gameplay
{
    /// <summary>
    /// Struct to represent an GamePlay actor level entity
    /// </summary>
    public readonly struct ActorHandle
    {
        public ulong Handle { get; }
        
        public const int IndexBits = 24;
        
        public const int SerialNumberBits = 40;
        
        public const int MaxIndex = 1 << IndexBits;
        
        public const ulong MaxSerialNumber = (ulong)1 << SerialNumberBits;
        
        public int GetIndex() => (int)(Handle & MaxIndex - 1);
        
        public ulong GetSerialNumber() => Handle >> IndexBits;
        
        public bool IsValid()
        {
            return Handle != 0;
        }
        
        public ActorHandle(ulong serialNum, int index)
        {
            Assert.IsTrue(index >= 0 && index < MaxIndex);
            Assert.IsTrue(serialNum < MaxSerialNumber);
#pragma warning disable CS0675
            Handle = (serialNum << IndexBits) | (ulong)index;
#pragma warning restore CS0675
        }

        public static bool operator ==(ActorHandle left, ActorHandle right)
        {
            return left.Handle == right.Handle;
        }
        
        public static bool operator !=(ActorHandle left, ActorHandle right)
        {
            return left.Handle != right.Handle;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is not ActorHandle handle) return false;
            return handle.Handle == Handle;
        }
        
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }
    }
    /// <summary>
    /// World container in GamePlay level.
    /// </summary>
    public class GameWorld : MonoBehaviour
    {
        private ulong _serialNum = 1;
        
        private const int InitialCapacity = 100;
        
        internal readonly SparseList<Actor> ActorsInWorld = new(InitialCapacity, ActorHandle.MaxIndex);
        
        internal readonly Subject<Unit> OnActorsUpdate = new();
        
        private WorldSubsystemCollection _subsystemCollection;
        
        private static GameWorld _current;

        private static bool _isPinned;

        private static bool _isInPendingDestroyed;

        private static int _inPendingDestroyedFrame;
        
        private bool _isSystemDirty;
        
        private bool _isDestroyed;
        
        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Destroy(gameObject);
                return;
            }
            _current = this;
            _subsystemCollection = new WorldSubsystemCollection(this);
        }
        
        private void Start()
        {
            _subsystemCollection.Init();
        }
        
        private void Update()
        {
            _subsystemCollection.Tick();
            if (!_isSystemDirty) return;
            _subsystemCollection.Rebuild();
            _isSystemDirty = false;
        }
        
        private void FixedUpdate()
        {
            _subsystemCollection.FixedTick();
            if (!_isSystemDirty) return;
            _subsystemCollection.Rebuild();
            _isSystemDirty = false;
        }
        
        private void OnDestroy()
        {
            _isDestroyed = true;
            _isInPendingDestroyed = true;
            _inPendingDestroyedFrame = Time.frameCount;
            _subsystemCollection?.Dispose();
            OnActorsUpdate.Dispose();
        }

        /// <summary>
        /// Make world always valid
        /// </summary>
        internal static void Pin()
        {
            _isPinned = true;
        }
        
        /// <summary>
        /// Reverse pin
        /// </summary>
        internal static void UnPin()
        {
            _isPinned = false;
        }

        /// <summary>
        /// Whether access to world is safe which will return null if world is being destroyed
        /// </summary>
        /// <returns></returns>
        public static bool IsValid()
        {
            if (_current) return !_current._isDestroyed;
            
            if (_isInPendingDestroyed)
            {
                if (Time.frameCount > _inPendingDestroyedFrame)
                {
                    _isInPendingDestroyed = false;
                }
            }
            
            if (_isPinned)
            {
                return true;
            }
            
            return !_isInPendingDestroyed;
        }
        
        public static GameWorld Get()
        {
            /* Access to world in destroy stage is not allowed */
            if (!IsValid()) return null;
            
            if (_current) return _current;
            _current = FindAnyObjectByType<GameWorld>();
            if (!_current)
            {
                _current = new GameObject(nameof(GameWorld)).AddComponent<GameWorld>();
            }
            return _current;
        }
        
        internal void RegisterActor(Actor actor, ref ActorHandle handle)
        {
            if (GetActor(handle) != null)
            {
                Debug.LogError($"[GameWorld] {actor.gameObject.name} is already registered to world!");
                return;
            }
            int index = ActorsInWorld.Add(actor);
            handle = new ActorHandle(_serialNum, index);
            OnActorsUpdate.OnNext(Unit.Default);
        }
        
        internal void UnregisterActor(Actor actor)
        {
            var handle = actor.GetActorHandle();
            int index = handle.GetIndex();
            if (!ActorsInWorld.IsAllocated(index)) return;
            
            var current = ActorsInWorld[index];
            if (current.GetActorHandle().GetSerialNumber() != handle.GetSerialNumber())
            {
                Debug.LogWarning($"[GameWorld] Actor {handle.Handle} has already been removed from world!");
                return;
            }
            // increase serial num as version update
            ++_serialNum;
            ActorsInWorld.RemoveAt(index);
            OnActorsUpdate.OnNext(Unit.Default);
        }
        
        /// <summary>
        /// Get actor from <see cref="ActorHandle"/>
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        [ExecutableFunction]
        public Actor GetActor(ActorHandle handle)
        {
            int index = handle.GetIndex();
            if (!handle.IsValid() || !ActorsInWorld.IsAllocated(index)) return null;
            
            var actor = ActorsInWorld[index];
            return actor.GetActorHandle().GetSerialNumber() != handle.GetSerialNumber() ? null : actor;
        }
        
        /// <summary>
        /// Get <see cref="SubsystemBase"/> from type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSubsystem<T>() where T : SubsystemBase
        {
            return _subsystemCollection.GetSubsystem<T>();
        }
        
        /// <summary>
        /// Get <see cref="SubsystemBase"/> from type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public SubsystemBase GetSubsystem(Type type)
        {
            return _subsystemCollection.GetSubsystem(type);
        }
        
        /// <summary>
        /// Register a <see cref="SubsystemBase"/> with type T
        /// </summary>
        /// <param name="subsystem"></param>
        /// <typeparam name="T"></typeparam>
        public void RegisterSubsystem<T>(T subsystem) where T : SubsystemBase
        {
            if (_isDestroyed)
            {
                Debug.LogError($"[GameWorld] World has already been destroyed!");
                return;
            }
            _subsystemCollection.RegisterSubsystem(subsystem);
            _isSystemDirty = true;
        }
    }
}
