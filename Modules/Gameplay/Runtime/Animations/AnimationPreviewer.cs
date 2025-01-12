using UnityEngine;
namespace Chris.Gameplay.Animations
{
    /// <summary>
    /// MonoBehaviour to preview animation pose both in editor and play mode
    /// </summary>
    public class AnimationPreviewer : MonoBehaviour
    {
        public Animator animator;
        
        public AnimationClip animationClip;
        
        private AnimationProxy _animationProxy;
        
        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        private void OnDestroy()
        {
            Release();
        }
        
        #region Runtime Rreview
        public void Preview()
        {
            _animationProxy ??= new AnimationProxy(animator);
            _animationProxy.LoadAnimationClip(animationClip, 0);
        }
        public void Stop()
        {
            _animationProxy.Stop(0);
        }

        public bool IsPlaying()
        {
            return _animationProxy != null && _animationProxy.IsPlaying;
        }
        #endregion Runtime Rreview

        internal void Release()
        {
            _animationProxy?.Dispose();
        }
    }
}
