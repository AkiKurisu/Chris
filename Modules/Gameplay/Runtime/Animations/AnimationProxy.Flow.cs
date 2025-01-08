using System;
using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Chris.Tasks;
using UnityEngine;
namespace Chris.Gameplay.Animations
{
    public partial class AnimationProxy
    {
        /* Following API only works on default layer */
        #region Flow API
        
        [ExecutableFunction]
        public void Flow_LoadAnimator(RuntimeAnimatorController runtimeAnimator, float blendInTime = 0.25f)
        {
            LoadAnimator(runtimeAnimator, blendInTime);
        }
        
        [ExecutableFunction]
        public void Flow_LoadAnimationClip(AnimationClip animationClip, float blendInTime = 0.25f)
        {
            LoadAnimationClip(animationClip, blendInTime);
        }
        
        [ExecutableFunction]
        public void Flow_PlayAnimation(
            AnimationClip animationClip, 
            int loopCount = 1,
            float blendInTime = 0.25f, 
            float blendOutTime = 0.25f)
        {
            CreateSequenceBuilder()
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
            Action onCompleteAction = onComplete;
            CreateSequenceBuilder()
                .Append(animationClip, animationClip.length * loopCount, blendInTime)
                .SetBlendOut(blendOutTime)
                .AppendCallBack(_ => onCompleteAction?.Invoke())
                .Build()
                .Run();
        }
        
        [ExecutableFunction]
        public void Flow_StopAnimation(float blendOutTime = 0.25f)
        {
            Stop(blendOutTime);
        }
        
        #endregion Flow API
    }
}