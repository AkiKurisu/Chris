using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using UnityEngine;
namespace Chris.Gameplay.Animations
{
    public partial class AnimationProxy
    {
        #region Flow API
        /* Only works on default layer */
        
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
        public void Flow_PlayAnimationWithCompleteEvent(
            AnimationClip animationClip, 
            float blendInTime, 
            float blendOutTime, 
            EventDelegate onComplete)
        {
            CreateSequenceBuilder()
                .Append(animationClip, blendInTime)
                .SetBlendOut(blendOutTime)
                .AppendCallBack(_ => onComplete.Invoke(animationClip))
                .Build();
        }
        
        [ExecutableFunction]
        public void Flow_StopAnimation(float fadeOutTime)
        {
            Stop(fadeOutTime);
        }
        
        #endregion Flow API
    }
}