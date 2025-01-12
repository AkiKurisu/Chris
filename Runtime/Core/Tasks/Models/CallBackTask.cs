using System;
namespace Chris.Tasks
{
    /// <summary>
    /// Represent an immediately completed task for callBack purpose
    /// </summary>
    public class CallBackTask : PooledTaskBase<CallBackTask>
    {
        private Action callBack;
        public static CallBackTask GetPooled(Action callBack)
        {
            var task = GetPooled();
            task.callBack = callBack;
            return task;
        }
        public override void Tick()
        {
            callBack?.Invoke();
            Status = TaskStatus.Completed;
        }
        protected override void Reset()
        {
            base.Reset();
            callBack = null;
        }
    }
}
