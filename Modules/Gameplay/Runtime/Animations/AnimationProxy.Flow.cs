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
        private readonly Dictionary<AnimationClip, SequenceTask> _runningSequences = new();
        
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
            _runningSequences[animationClip] = CreateSequenceBuilder()
                .Append(animationClip, animationClip.length * loopCount, blendInTime)
                .SetBlendOut(blendOutTime)
                .Build()
                .Run();
            _runningSequences[animationClip].Acquire();
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
            _runningSequences[animationClip] = CreateSequenceBuilder()
                .Append(animationClip, animationClip.length * loopCount, blendInTime)
                .SetBlendOut(blendOutTime)
                .AppendCallBack(_ => onCompleteAction?.Invoke())
                .Build()
                .Run();
            _runningSequences[animationClip].Acquire();
        }

        [ExecutableFunction]
        public void Flow_StopAnimation(AnimationClip animationClip)
        {
            if (!_runningSequences.TryGetValue(animationClip, out var task)) return;
            task?.Stop();
            task?.Dispose();
            _runningSequences.Remove(animationClip);
        }
        
        [ExecutableFunction]
        public void Flow_StopAllAnimation()
        {
            StopAllAnimationSequences();
        }
        
        #endregion Flow API

        /// <summary>
        /// Stop all running animation sequences created by this proxy
        /// </summary>
        public void StopAllAnimationSequences()
        {
            foreach (var pair in _runningSequences)
            {
                pair.Value?.Stop();
                pair.Value?.Dispose();
            }
            _runningSequences.Clear();
        }
    }
}