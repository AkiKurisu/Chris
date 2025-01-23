using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Ceres.Graph.Flow.Annotations;
using Chris.Gameplay;
using Chris.Tasks;
using Kurisu.AkiBT;
namespace Chris.AI
{
    [RequireComponent(typeof(BlackBoardComponent))]
    public abstract class AIController : PlayerController
    {
        [SerializeField]
        private BehaviorTask[] behaviorTasks;
        
        protected Dictionary<string, TaskBase> TaskMap { get; } = new();
        
        public bool IsAIEnabled { get; protected set; }
        
        public BlackBoard BlackBoard { get; private set; }
        
        [ImplementableEvent]
        protected override void Awake()
        {
            BlackBoard = GetComponent<BlackBoardComponent>().GetBlackBoard();
            base.Awake();
        }
        
        [ImplementableEvent]
        protected override void Start()
        {
            SetupBehaviorTree();
            base.Start();
        }
        
        [ExecutableFunction]
        public sealed override bool IsBot()
        {
            return true;
        }
        
        public IAIPawn GetAIPawn()
        {
            return GetActor() as IAIPawn;
        }
        
        public TPawn GetPawn<TPawn>() where TPawn : Actor, IAIPawn
        {
            return GetActor() as TPawn;
        }
        
        private void SetupBehaviorTree()
        {
            foreach (var task in behaviorTasks)
            {
                AddTask(task);
            }
        }
        
        [ImplementableEvent]
        protected override void OnDestroy()
        {
            foreach (var task in TaskMap.Values)
            {
                task.Stop();
                task.Dispose();
            }
            base.OnDestroy();
        }
        
        [ExecutableFunction]
        public void EnableAI()
        {
            IsAIEnabled = true;
            foreach (var task in TaskMap.Values)
            {
                if (((IAITask)task).IsStartOnEnabled())
                    task.Start();
            }
        }
        
        [ExecutableFunction]
        public void DisableAI()
        {
            IsAIEnabled = false;
            foreach (var task in TaskMap.Values)
            {
                task.Pause();
            }
        }
        
        [ImplementableEvent]
        protected override void OnEnable()
        {
            EnableAI();
            base.OnEnable();
        }
        
        [ImplementableEvent]
        protected override void OnDisable()
        {
            DisableAI();
            base.OnDisable();
        }
        
        [ExecutableFunction]
        public TaskBase GetTask(string taskID)
        {
            return TaskMap[taskID];
        }
        
        public void AddTask<T>(T task) where T : TaskBase, IAITask
        {
            string id = task.GetTaskID();
            if (TaskMap.ContainsKey(id))
            {
                Debug.LogWarning($"Already contained task with same id: {id}");
                return;
            }
            task.SetController(this);
            task.Acquire();
            TaskMap.Add(id, task);
            TaskRunner.RegisterTask(task);
            if (IsAIEnabled && task.IsStartOnEnabled())
            {
                task.Start();
            }
        }
        
        [ExecutableFunction]
        public TaskBase[] GetAllTasks()
        {
            return TaskMap.Values.ToArray();
        }
    }
}

