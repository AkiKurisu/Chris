using System;
using System.Collections.Generic;
using Chris.Schedulers;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Profiling;
using UnityEngine.Assertions;
namespace Chris.AI.EQS
{
    /// <summary>
    /// Command for schedule post query job
    /// </summary>
    public struct PostQueryCommand
    {
        public ActorHandle self;
        public ActorHandle target;
        public float3 offset;
        public int layerMask;
        public PostQueryParameters parameters;
    }
    public class PostQuerySystem : WorldSubsystem
    {
        [BurstCompile]
        public struct PrepareCommandJob : IJobParallelFor
        {
            [ReadOnly]
            public PostQueryCommand command;
            [ReadOnly]
            public ActorData source;
            [ReadOnly]
            public ActorData target;
            [ReadOnly]
            public int length;
            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeArray<RaycastCommand> raycastCommands;
            public void Execute(int index)
            {
                var direction = math.normalize(source.position - target.position);

                float angle = command.parameters.Angle / 2;

                quaternion rot = quaternion.RotateY(math.radians(math.lerp(-angle, angle, (float)index / length)));

                raycastCommands[index] = new RaycastCommand()
                {
                    from = target.position + command.offset,
                    direction = math.rotate(rot, direction),
                    distance = command.parameters.Distance,
                    queryParameters = new QueryParameters() { layerMask = command.layerMask }
                };
            }
        }
        /// <summary>
        /// Worker per actor
        /// </summary>
        private class PostQueryWorker
        {
            private NativeList<float3> _posts = new(Allocator.Persistent);
            
            private NativeArray<RaycastHit> _hits;
            
            private NativeArray<RaycastCommand> _raycastCommands;
            
            private JobHandle _jobHandle;
            
            public bool IsRunning { get; private set; }
            
            public bool HasPendingCommand { get; private set; }

            public NativeArray<float3>.ReadOnly GetPosts()
            {
                return _posts.AsReadOnly();
            }
            public void SetPending()
            {
                HasPendingCommand = true;
            }
            public void ExecuteCommand(ref PostQueryCommand command, ref NativeArray<ActorData> actorDatas)
            {
                HasPendingCommand = false;
                IsRunning = true;
                int length = command.parameters.Step * command.parameters.Depth;
                _raycastCommands.DisposeSafe();
                _raycastCommands = new(length, Allocator.TempJob);
                _hits.DisposeSafe();
                _hits = new(length, Allocator.TempJob);
                var job = new PrepareCommandJob()
                {
                    command = command,
                    raycastCommands = _raycastCommands,
                    length = length,
                    source = actorDatas[command.self.GetIndex()],
                    target = actorDatas[command.target.GetIndex()]
                };
                _jobHandle = job.Schedule(length, 32, default);
                _jobHandle = RaycastCommand.ScheduleBatch(_raycastCommands, _hits, _raycastCommands.Length, _jobHandle);
            }
            public void CompleteCommand()
            {
                IsRunning = false;
                _jobHandle.Complete();
                _posts.Clear();
                bool hasHit = false;
                foreach (var hit in _hits)
                {
                    bool isHit = hit.point != default;
                    if (!hasHit && isHit)
                    {
                        _posts.Add(hit.point);
                    }
                    hasHit = isHit;
                }
                _raycastCommands.Dispose();
                _hits.Dispose();
            }
            public void Dispose()
            {
                _posts.Dispose();
                _hits.DisposeSafe();
                _raycastCommands.DisposeSafe();
            }
        }
        private readonly Queue<PostQueryCommand> commandBuffer = new();
        
        private SchedulerHandle updateTickHandle;
        
        private SchedulerHandle lateUpdateTickHandle;
        
        private NativeArray<ActorHandle> batchHandles;
        
        private int batchLength;
        
        private readonly Dictionary<ActorHandle, PostQueryWorker> workerDic = new();
        
        /// <summary>
        /// Set system parallel workers count
        /// </summary>
        /// <value></value>
        public static int MaxWorkerCount { get; set; } = DefaultWorkerCount;
        
        /// <summary>
        /// Default parallel workers count: 5
        /// </summary>
        public const int DefaultWorkerCount = 5;
        
        /// <summary>
        /// Set sysytem tick frame
        /// </summary>
        /// <value></value>
        public static int FramePerTick { get; set; } = DefaultFramePerTick;
        
        /// <summary>
        /// Default tick frame: 2 fps
        /// </summary>
        public const int DefaultFramePerTick = 25;
        
        private static readonly ProfilerMarker ConsumeCommandsPM = new("PostQuerySystem.ConsumeCommands");
        
        private static readonly ProfilerMarker CompleteCommandsPM = new("PostQuerySystem.CompleteCommands");
        
        protected override void Initialize()
        {
            Assert.IsFalse(FramePerTick <= 3);
            Scheduler.WaitFrame(ref updateTickHandle, FramePerTick, ConsumeCommands, TickFrame.FixedUpdate, isLooped: true);
            // Allow job scheduled in 3 frames
            Scheduler.WaitFrame(ref lateUpdateTickHandle, 3, CompleteCommands, TickFrame.FixedUpdate, isLooped: true);
            lateUpdateTickHandle.Pause();
            batchHandles = new NativeArray<ActorHandle>(MaxWorkerCount, Allocator.Persistent);
        }
        private void ConsumeCommands(int _)
        {
            using (ConsumeCommandsPM.Auto())
            {
                batchLength = 0;
                var actorDatas = GetOrCreate<ActorQuerySystem>().GetAllActors(Allocator.Temp);
                while (batchLength < MaxWorkerCount)
                {
                    if (!commandBuffer.TryDequeue(out var command))
                    {
                        break;
                    }

                    var worker = workerDic[command.self];

                    if (worker.IsRunning)
                    {
                        Debug.LogWarning($"[PostQuerySystem] Should not enquene new command [ActorId: {command.self.Handle}] before last command completed!");
                        continue;
                    }

                    worker.ExecuteCommand(ref command, ref actorDatas);
                    batchHandles[batchLength++] = command.self;
                }
                actorDatas.Dispose();
            }
            lateUpdateTickHandle.Resume();
        }
        private void CompleteCommands(int _)
        {
            using (CompleteCommandsPM.Auto())
            {
                for (int i = 0; i < batchLength; ++i)
                {
                    workerDic[batchHandles[i]].CompleteCommand();
                }
            }
            lateUpdateTickHandle.Pause();
        }

        protected override void Release()
        {
            batchHandles.Dispose();
            updateTickHandle.Dispose();
            lateUpdateTickHandle.Dispose();
            foreach (var worker in workerDic.Values)
            {
                worker.Dispose();
            }
            workerDic.Clear();
        }
        /// <summary>
        /// Enqueue a new <see cref="PostQueryCommand"/> to the system
        /// </summary>
        /// <param name="command"></param>
        public void EnqueueCommand(PostQueryCommand command)
        {
            if (!workerDic.TryGetValue(command.self, out var worker))
            {
                worker = workerDic[command.self] = new();
            }
            worker.SetPending();
            commandBuffer.Enqueue(command);
        }
        /// <summary>
        /// Get cached posts has found for target actor use latest command
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public NativeArray<float3>.ReadOnly GetPosts(ActorHandle handle)
        {
            if (workerDic.TryGetValue(handle, out var worker))
                return worker.GetPosts();
            return default;
        }
        /// <summary>
        /// Whether the worker for target actor is free to execute new command
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public bool IsFree(ActorHandle handle)
        {
            if (workerDic.TryGetValue(handle, out var worker))
                return !worker.IsRunning && !worker.HasPendingCommand;
            return true;
        }
    }
}
