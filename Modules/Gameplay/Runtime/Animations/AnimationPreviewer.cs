using UnityEngine;
namespace Chris.Gameplay.Animations
{
    /// <summary>
    /// MonoBehaviour to preview animation pose using <see cref="AnimationProxy"/>
    /// </summary>
    public class AnimationPreviewer : MonoBehaviour
    {
        public Animator Animator;
        public AnimationClip AnimationClip;
        private AnimationProxy animationProxy;
        private void Reset()
        {
            Animator = GetComponentInChildren<Animator>();
        }
        private void OnDestroy()
        {
            Release();
        }
        #region Runtime Rreview
        public void Preview()
        {
            animationProxy ??= new AnimationProxy(Animator);
            animationProxy.LoadAnimationClip(AnimationClip, 0);
        }
        public void Stop()
        {
            animationProxy.Stop(0);
        }

        public bool IsPlaying()
        {
            return animationProxy != null && animationProxy.IsPlaying;
        }
        #endregion Runtime Rreview

        internal void Release()
        {
            animationProxy?.Dispose();
        }
    }
}
