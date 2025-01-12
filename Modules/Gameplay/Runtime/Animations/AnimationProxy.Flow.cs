using System;
using System.Collections.Generic;
using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Chris.Tasks;
using UnityEngine;
namespace Chris.Gameplay.Animations
{
    public partial class AnimationProxy
    {
        private readonly Dictionary<AnimationClip, SequenceTask> _runningTasks = new();
        
        /* Following API only works on default layer */
        #region Flow API
        
        [ExecutableFunction]
        public void Flow_PlayAnimation(
            AnimationClip animationClip, 
            int loopCount = 1,
            float blendInTime = 0.25f, 
            float blendOutTime = 0.25f)
        {
            Flow_StopAnimation(animationClip);
            _runningTasks[animationClip] = CreateSequenceBuilder()
                .Append(animationClip, animationClip.length * loopCount, blendInTime)
                .SetBlendOut(blendOutTime)
                .Build()
                .Run();
        }
        
        [ExecutableFunction]
        public void Flow_PlayAnimationWithCompleteEvent(
            AnimationClip animationClip, 
            int loopCount = 1,
            float blendInTime = 0.25f, 
            float blendOutTime = 0.25f, 
            EventDelegate onComplete = null)
        {
            Flow_StopAnimation(animationClip);
            Action onCompleteAction = onComplete;
            _runningTasks[animationClip] = CreateSequenceBuilder()
                .Append(animationClip, animationClip.length * loopCount, blendInTime)
                .SetBlendOut(blendOutTime)
                .AppendCallBack(_ => onCompleteAction?.Invoke())
                .Build()
                .Run();
        }

        [ExecutableFunction]
        public void Flow_StopAnimation(AnimationClip animationClip)
        {
            if (!_runningTasks.TryGetValue(animationClip, out var task)) return;
            task?.Stop();
            _runningTasks.Remove(animationClip);
        }
        
        [ExecutableFunction]
        public void Flow_StopAllAnimation()
        {
            foreach (var runningTask in _runningTasks)
            {
                runningTask.Value?.Stop();
            }
            _runningTasks.Clear();
        }
        
        #endregion Flow API
    }
}