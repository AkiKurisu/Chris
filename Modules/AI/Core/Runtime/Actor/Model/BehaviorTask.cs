using System;
using Chris.Tasks;
using Kurisu.AkiBT;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Chris.AI
{
    /// <summary>
    /// Task to run a behavior tree inside a agent-authority state machine.
    /// Whether behavior tree is failed or succeed will not affect task status. 
    /// </summary>
    [Serializable]
    internal class BehaviorTask : TaskBase, IBehaviorTreeContainer, IAITask
    {
        [SerializeField, TaskID]
        private string taskID;
        
        [SerializeField, Tooltip("Start this task automatically when controller is enabled")]
        private bool startOnEnabled;
        
        [SerializeField]
        private BehaviorTreeAsset behaviorTreeAsset;
        
        public BehaviorTree InstanceTree { get; private set; }
        
        Object IBehaviorTreeContainer.Object => host;
        
        private AIController host;
        
        public BehaviorTask()
        {
            // pause on cctor, manually start by controller
            Status = TaskStatus.Paused;
        }
        
        public void SetController(AIController hostController)
        {
            host = hostController;
            InstanceTree = behaviorTreeAsset.GetBehaviorTree();
            InstanceTree.InitVariables();
            InstanceTree.BlackBoard.MapTo(hostController.BlackBoard);
            InstanceTree.Run(hostController.gameObject);
            InstanceTree.Awake();
            InstanceTree.Start();
        }
        
        public override void Tick()
        {
            InstanceTree.Tick();
        }
        
        public override void Stop()
        {
            base.Stop();
            InstanceTree.Abort();
        }
        
        public override void Pause()
        {
            base.Pause();
            InstanceTree.Abort();
        }
        
        public override void Dispose()
        {
            base.Dispose();
            InstanceTree.Dispose();
        }
        
        public BehaviorTree GetBehaviorTree()
        {
            // get runtime instance tree only
            return InstanceTree;
        }

        public void SetBehaviorTreeData(BehaviorTreeData behaviorTreeData)
        {
            // should not edit instance
        }

        public override string GetTaskID()
        {
            return taskID;
        }

        public bool IsStartOnEnabled()
        {
            return startOnEnabled;
        }
        
        protected override string GetTaskName()
        {
            return $"BehaviorTask [{host.gameObject.name}>>{behaviorTreeAsset.name}]";
        }
    }
}