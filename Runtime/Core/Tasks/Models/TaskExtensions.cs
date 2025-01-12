namespace Chris.Tasks
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Run the task, return <see cref="TaskBase"/> self
        /// </summary>
        public static TaskBase Run(this TaskBase taskBase)
        {
            if (taskBase.GetStatus() == TaskStatus.Running)
            {
                return taskBase;
            }
            if (taskBase.HasPrerequisite()) return taskBase;
            taskBase.Start();
            TaskRunner.RegisterTask(taskBase);
            return taskBase;
        }
        
        public static TTask Run<TTask>(this TTask taskBase) where TTask: TaskBase
        {
            return (TTask)Run((TaskBase)taskBase);
        }
    }
}
