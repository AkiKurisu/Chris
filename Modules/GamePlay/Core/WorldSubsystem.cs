using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ceres.Graph.Flow.Annotations;
using R3;
using UnityEngine;
namespace Chris.Gameplay
{
    internal class WorldSubsystemCollection : IDisposable
    {
        private readonly Dictionary<Type, SubsystemBase> _systems;
        
        private SubsystemBase[] _subsystems;
        
        private readonly IDisposable _actorsUpdateSubscription;
        
        public WorldSubsystemCollection(GameWorld world)
        {
            var types = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(x => x.GetTypes())
                    .SelectMany(x => x)
                    .Where(x => x.IsSubclassOf(typeof(WorldSubsystem)) && !x.IsAbstract)
                    .Where(x => x.GetCustomAttribute<InitializeOnWorldCreateAttribute>() != null)
                    .ToList();
            _systems = types.ToDictionary(x => x, x => Activator.CreateInstance(x) as SubsystemBase);
            foreach (var type in types)
            {
                if (!((WorldSubsystem)_systems[type]).CanCreate(world)) _systems.Remove(type);
                else _systems[type].SetWorld(world);
            }
            _subsystems = _systems.Values.ToArray();
            _actorsUpdateSubscription = world.OnActorsUpdate.Subscribe(OnActorsUpdate);
        }
        
        internal void RegisterSubsystem<T>(T subsystem) where T : SubsystemBase
        {
            _systems.Add(typeof(T), subsystem);
            subsystem.InternalInit();
        }
        
        internal void Rebuild()
        {
            _subsystems = _systems.Values.ToArray();
            Init();
        }
        
        public T GetSubsystem<T>() where T : SubsystemBase
        {
            if (_systems.TryGetValue(typeof(T), out var subsystem))
                return (T)subsystem;
            return null;
        }
        
        public SubsystemBase GetSubsystem(Type type)
        {
            return _systems.GetValueOrDefault(type);
        }
        
        public void Init()
        {
            for (int i = 0; i < _subsystems.Length; ++i)
            {
                _subsystems[i].InternalInit();
            }
        }
        
        public void Tick()
        {
            for (int i = 0; i < _subsystems.Length; ++i)
            {
                _subsystems[i].Tick();
            }
        }
        
        public void FixedTick()
        {
            for (int i = 0; i < _subsystems.Length; ++i)
            {
                _subsystems[i].FixedTick();
            }
        }
        
        public void Dispose()
        {
            for (int i = 0; i < _subsystems.Length; ++i)
            {
                _subsystems[i].InternalRelease();
            }
            _subsystems = null;
            _actorsUpdateSubscription.Dispose();
        }
        
        private void OnActorsUpdate(Unit _)
        {
            for (int i = 0; i < _subsystems.Length; ++i)
            {
                _subsystems[i].IsActorsDirty = true;
            }
        }
    }
    /// <summary>
    /// Base class for subsystem, gameplay subsystem should not implement from this directly. 
    /// See <see cref="WorldSubsystem"/>.
    /// </summary>
    public abstract class SubsystemBase
    {
        /// <summary>
        /// Buffered dirty flag when world actors changed
        /// </summary>
        protected internal bool IsActorsDirty { get; set; }
        
        /// <summary>
        /// Is system initialized
        /// </summary>
        /// <value></value>
        protected bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Is system destroyed
        /// </summary>
        /// <value></value>
        protected bool IsDestroyed { get; private set; }

        private GameWorld _world;

        /// <summary>
        /// Subsystem initialize phase, should bind callbacks and collect references in this phase
        /// </summary>
        protected virtual void Initialize()
        {

        }

        internal virtual void InternalInit()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            Initialize();
        }

        public virtual void Tick()
        {

        }

        public virtual void FixedTick()
        {

        }

        /// <summary>
        /// Subsystem release phase, should unbind callbacks and release references in this phase
        /// </summary>
        protected virtual void Release()
        {

        }

        internal virtual void InternalRelease()
        {
            if (IsDestroyed) return;
            _world = null;
            IsDestroyed = true;
            Release();
        }

        internal void SetWorld(GameWorld world)
        {
            _world = world;
        }

        /// <summary>
        /// Get attached world
        /// </summary>
        /// <returns></returns>
        [ExecutableFunction]
        public GameWorld GetWorld() => _world;

        /// <summary>
        /// Get all actors in world, readonly
        /// </summary>
        /// <returns></returns>
        protected void GetActorsInWorld(List<Actor> actors)
        {
            if (!_world)
            {
                Debug.LogWarning("[World Subsystem] System not bound to an actor world.");
                return;
            }
            foreach (var actor in _world.ActorsInWorld)
            {
                actors.Add(actor);
            }
        }

        /// <summary>
        /// Get actor num in world, readonly
        /// </summary>
        /// <returns></returns>
        protected int GetActorsNum()
        {
            if (!_world)
            {
                Debug.LogWarning("[World Subsystem] System not bound to an actor world.");
                return default;
            }
            return _world.ActorsInWorld.Count;
        }
    }
    
    /// <summary>
    /// Subsystem bound to an actor world.
    /// </summary>
    public abstract class WorldSubsystem : SubsystemBase
    {
        /// <summary>
        /// Whether <see cref="WorldSubsystem"/> can create
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public virtual bool CanCreate(GameWorld world) => true;

        /// <summary>
        /// Get or create system if not registered
        /// </summary>
        /// <param name="world"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetOrCreate<T>(GameWorld world) where T : WorldSubsystem, new()
        {
            if (!GameWorld.IsValid()) return null;
            
            var system = world.GetSubsystem<T>();
            if (system != null) return system;
            
            system = new T();
            if (!system.CanCreate(world)) return null;
            
            system.SetWorld(world);
            world.RegisterSubsystem(system);
            return system;
        }
        
        /// <summary>
        /// Get or create system if not registered
        /// </summary>
        /// <param name="world"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static WorldSubsystem GetOrCreate(GameWorld world, Type type)
        {
            if (!GameWorld.IsValid()) return null;
            
            var system = (WorldSubsystem)world.GetSubsystem(type);
            if (system != null) return system;
            
            system = (WorldSubsystem)Activator.CreateInstance(type);
            if (!system.CanCreate(world)) return null;
            
            system.SetWorld(world);
            world.RegisterSubsystem(system);
            return system;
        }

        /// <summary>
        /// Get or create system if not registered
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetOrCreate<T>() where T : WorldSubsystem, new()
        {
            return !GameWorld.IsValid() ? null : GetOrCreate<T>(GameWorld.Get());
        }

        /// <summary>
        /// Get or create system if not registered
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static WorldSubsystem GetOrCreate(Type type)
        {
            return !GameWorld.IsValid() ? null : GetOrCreate(GameWorld.Get(), type);
        }
    }
}
