using System;
using System.Collections.Generic;
using Chris.Events;
using Chris.Pool;
using UnityEngine;
using UnityEngine.Pool;
namespace Chris.Tasks
{
    public enum TaskStatus
    {
        /// <summary>
        /// Task is enabled to run and can be updated
        /// </summary>
        Running,
        /// <summary>
        /// Task is paused and will be ignored
        /// </summary>
        Paused,
        /// <summary>
        /// Task is completed and wait to broadcast complete event
        /// </summary>
        Completed,
        /// <summary>
        /// Task is stopped and will not broadcast complete event
        /// </summary>
        Stopped
    }
    
    public interface ITaskEvent { }
    
    public sealed class TaskCompleteEvent : EventBase<TaskCompleteEvent>, ITaskEvent
    {
        public TaskBase Task { get; private set; }
        
        /// <summary>
        /// Soft reference to subtask, listener may be disposed before broadcast this event.
        /// So we check subtask's prerequisite whether contains this event to identify its lifetime version.
        /// </summary>
        /// <returns></returns>
        public readonly List<TaskBase> Listeners = new();
        
        public static TaskCompleteEvent GetPooled(TaskBase task)
        {
            var evt = GetPooled();
            evt.Task = task;
            evt.Listeners.Clear();
            return evt;
        }
        
        public void AddListenerTask(TaskBase taskBase)
        {
            Listeners.Add(taskBase);
        }
        
        public void RemoveListenerTask(TaskBase taskBase)
        {
            Listeners.Remove(taskBase);
        }
    }
    
    /// <summary>
    /// Base class for framework task
    /// </summary>
    public abstract class TaskBase : IDisposable
    {
        protected TaskStatus Status;
        
        public virtual TaskStatus GetStatus()
        {
            return Status;
        }
        
        public abstract string GetTaskID();
        
        /// <summary>
        /// Debug usage
        /// </summary>
        /// <returns></returns>
        protected virtual string GetTaskName()
        {
#if UNITY_EDITOR
            return GetType().Name;
#else
            return string.Empty;
#endif
        }
        
        internal string InternalGetTaskName() => GetTaskName();
        
        #region Lifetime Cycle
        protected virtual void Init()
        {
            Status = TaskStatus.Stopped;
        }
        
        public virtual void Stop()
        {
            Status = TaskStatus.Stopped;
        }
        
        public virtual void Start()
        {
            Status = TaskStatus.Running;
        }
        
        public virtual void Pause()
        {
            Status = TaskStatus.Paused;
        }

        public virtual void Tick()
        {

        }
        
        protected void CompleteTask()
        {
            Status = TaskStatus.Completed;
        }
        
        protected virtual void Reset()
        {
            Status = TaskStatus.Stopped;
        }
        
        public virtual void Acquire()
        {

        }
        
        public virtual void Dispose()
        {
            if (_prerequisites != null)
            {
                HashSetPool<TaskCompleteEvent>.Release(_prerequisites);
                _prerequisites = null;
            }
            if (_completeEvent != null)
            {
                _completeEvent.Dispose();
                _completeEvent = null;
            }
        }
        #endregion
        
        #region Prerequistes Management
        
        private HashSet<TaskCompleteEvent> _prerequisites;
        
        private TaskCompleteEvent _completeEvent;
        
        internal void PostComplete()
        {
            if (_completeEvent == null) return;
            EventSystem.EventHandler.SendEvent(_completeEvent);
            _completeEvent.Dispose();
            _completeEvent = null;
        }
        
        /// <summary>
        /// Release prerequisite if contains its reference
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        internal bool ReleasePrerequisite(TaskCompleteEvent evt)
        {
            return _prerequisites.Remove(evt);
        }
        
        internal bool HasPrerequisite()
        {
            return _prerequisites != null && _prerequisites.Count > 0;
        }
        
        /// <summary>
        /// Get task complete event
        /// </summary>
        /// <returns></returns>
        public TaskCompleteEvent GetCompleteEvent()
        {
            _completeEvent ??= TaskCompleteEvent.GetPooled(this);
            return _completeEvent;
        }
        
        /// <summary>
        /// Add a prerequisite task before this task run
        /// </summary>
        /// <param name="taskBase"></param>
        public void RegisterPrerequisite(TaskBase taskBase)
        {
            var evt = taskBase.GetCompleteEvent();
            evt.AddListenerTask(this);
            _prerequisites ??= HashSetPool<TaskCompleteEvent>.Get();
            _prerequisites.Add(evt);
        }
        
        /// <summary>
        /// Remove a prerequisite task if exist
        /// </summary>
        /// <param name="taskBase"></param>
        public bool UnregisterPrerequisite(TaskBase taskBase)
        {
            if (_prerequisites == null) return false;
            var evt = taskBase.GetCompleteEvent();
            // should not edit collections when is being dispatched
            if (!evt.Dispatch)
                evt.RemoveListenerTask(this);
            return _prerequisites.Remove(evt);
        }
        #endregion
    }
    
    public abstract class PooledTaskBase<T> : TaskBase where T : PooledTaskBase<T>, new()
    {
        private int m_RefCount;
        
        private bool pooled;
        
        private static readonly _ObjectPool<T> s_Pool = new(() => new T());
        
        private static readonly string defaultName;
        
        static PooledTaskBase()
        {
            defaultName = typeof(T).Name;
        }
        
        protected PooledTaskBase() : base()
        {
            m_RefCount = 0;
        }
        
        public sealed override void Dispose()
        {
            if (--m_RefCount == 0)
            {
                base.Dispose();
                ReleasePooled((T)this);
            }
        }
        
        private static void ReleasePooled(T evt)
        {
            if (evt.pooled)
            {
                evt.Reset();
                s_Pool.Release(evt);
                evt.pooled = false;
            }
        }
        
        public static T GetPooled()
        {
            T t = s_Pool.Get();
            t.Init();
            t.pooled = true;
            return t;
        }
        
        protected override void Init()
        {
            base.Init();
            if (m_RefCount != 0)
            {
                Debug.LogWarning($"Task improperly released, reference count {m_RefCount}.");
                m_RefCount = 0;
            }
        }
        
        public override void Acquire()
        {
            m_RefCount++;
        }
        
        public override string GetTaskID()
        {
            return defaultName;
        }
    }
}
