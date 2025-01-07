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
        public void Flow_LoadAnimator(RuntimeAnimatorController runtimeAnimator, float blendInTime)
        {
            LoadAnimator(runtimeAnimator, blendInTime);
        }
        
        [ExecutableFunction]
        public void Flow_LoadAnimationClip(AnimationClip animationClip, float blendInTime)
        {
            LoadAnimationClip(animationClip, blendInTime);
        }
        
        [ExecutableFunction]
        public void Flow_PlayAnimation(
            AnimationClip animationClip, 
            float blendInTime, 
            float blendOutTime)
        {
            CreateSequenceBuilder()
                .Append(animationClip, blendInTime)
                .SetBlendOut(blendOutTime)
                .Build()
                .Run();
        }
        
        [ExecutableFunction]
        public void Flow_PlayAnimationWithCompleteEvent(
            AnimationClip animationClip, 
            float blendInTime, 
            float blendOutTime, 
            EventDelegate onComplete)
        {
            Action onCompleteAction = onComplete;
            CreateSequenceBuilder()
                .Append(animationClip, blendInTime)
                .SetBlendOut(blendOutTime)
                .AppendCallBack(_ => onCompleteAction?.Invoke())
                .Build()
                .Run();
        }
        
        [ExecutableFunction]
        public void Flow_StopAnimation(float fadeOutTime)
        {
            Stop(fadeOutTime);
        }
        
        #endregion Flow API
    }
}