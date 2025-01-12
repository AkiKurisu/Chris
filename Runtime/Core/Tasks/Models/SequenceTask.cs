using System;
using System.Collections;
using System.Collections.Generic;
namespace Chris.Tasks
{
    /// <summary>
    /// Sequence to composite tasks
    /// </summary>
    public class SequenceTask : PooledTaskBase<SequenceTask>, IEnumerable<TaskBase>
    {
        public event Action OnCompleted;
        
        private readonly Queue<TaskBase> _tasks = new();
        
        private TaskBase _runningTask;
        
        public static SequenceTask GetPooled(Action callBack)
        {
            var task = GetPooled();
            task.OnCompleted = callBack;
            return task;
        }
        
        public static SequenceTask GetPooled(TaskBase firstTask, Action callBack)
        {
            var task = GetPooled();
            task.OnCompleted = callBack;
            task.Append(firstTask);
            return task;
        }
        
        public static SequenceTask GetPooled(IReadOnlyList<TaskBase> sequence, Action callBack)
        {
            var task = GetPooled();
            task.OnCompleted = callBack;
            foreach (var tb in sequence)
                task.Append(tb);
            return task;
        }
        
        protected override void Reset()
        {
            base.Reset();
            _runningTask = null;
            Status = TaskStatus.Stopped;
            OnCompleted = null;
            _tasks.Clear();
        }
        
        /// <summary>
        /// Append a task to the end of sequence
        /// </summary>
        /// <param name="task"></param>
        public SequenceTask Append(TaskBase task)
        {
            _tasks.Enqueue(task);
            task.Acquire();
            return this;
        }
        
        public SequenceTask AppendRange(IEnumerable<TaskBase> enumerable)
        {
            foreach (var task in enumerable)
                Append(task);
            return this;
        }

        public override void Tick()
        {
            while (true)
            {
                if (_runningTask == null)
                {
                    _tasks.TryPeek(out _runningTask);
                    _runningTask.Start();
                }

                if (_runningTask != null)
                {
                    _runningTask.Tick();
                    var status = _runningTask.GetStatus();
                    if (status is TaskStatus.Completed or TaskStatus.Stopped)
                    {
                        if (status == TaskStatus.Completed)
                        {
                            _runningTask.PostComplete();
                        }

                        _tasks.Dequeue().Dispose();
                        _runningTask = null;

                        if (_tasks.Count == 0)
                        {
                            Status = TaskStatus.Completed;
                            CompleteWithEvent();
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    Status = TaskStatus.Completed;
                    CompleteWithEvent();
                }

                break;
            }
        }

        private void CompleteWithEvent()
        {
            OnCompleted?.Invoke();
            OnCompleted = null;
        }

        public IEnumerator<TaskBase> GetEnumerator()
        {
            return _tasks.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tasks.GetEnumerator();
        }
        
        /// <summary>
        /// Append a call back after current last action in the sequence
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public SequenceTask AppendCallback(Action callback)
        {
            return Append(CallBackTask.GetPooled(callback));
        }
    }
}
