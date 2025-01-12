using UnityEditor;
using UnityEngine;
using UEditor = UnityEditor.Editor;
namespace Chris.Gameplay.Animations.Editor
{
    [CustomEditor(typeof(AnimationPreviewer))]
    public class AnimationPreviewerEditor : UEditor
    {
        private AnimationPreviewer Previewer => (AnimationPreviewer)target;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUI.enabled = Previewer.animationClip && Previewer.animator;
            if (IsPlaying())
            {
                if (GUILayout.Button("Stop"))
                {
                    Stop();
                }
            }
            else
            {
                if (GUILayout.Button("Preview"))
                {
                    Preview();
                }
            }
            GUI.enabled = true;
        }
        
        private bool IsPlaying()
        {
            if (Application.isPlaying)
            {
                return Previewer.IsPlaying();
            }
            return AnimationMode.InAnimationMode();
        }
        
        private void Preview()
        {
            if (Application.isPlaying)
            {
                Previewer.Preview();
            }
            else
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(Previewer.animator.gameObject, Previewer.animationClip, 0);
            }
        }
        
        private void Stop()
        {
            if (Application.isPlaying)
            {
                Previewer.Preview();
            }
            else
            {
                AnimationMode.EndSampling();
                AnimationMode.StopAnimationMode();
            }
        }
    }
}
