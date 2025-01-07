using System;
using Chris.Schedulers;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace Chris.Gameplay.Animations
{
    public partial class AnimationProxy
    {
        public class AnimationPlayableNode : IDisposable
        {
            public PlayableGraph Graph { get; }
            
            public Playable Playable { get; }
            
            /// <summary>
            /// Get leaf animator controller, since we can not access to Playable's animator controller, 
            /// we cache playable source controller in constructor.         
            /// </summary>
            /// <value></value>
            public RuntimeAnimatorController AnimatorController { get; }
            
            public AnimationPlayableNode(Playable playable, RuntimeAnimatorController sourceController = null)
            {
                Graph = playable.GetGraph();
                Playable = playable;
                AnimatorController = sourceController;
            }
            
            public bool IsValid()
            {
                return Playable.IsValid();
            }
            
            /// <summary>
            /// Destroy playable recursively
            /// </summary>
            public void Destroy()
            {
                Playable playable = Playable;
                while (playable.IsValid())
                {
                    var input = playable.GetInput(0);
                    playable.Destroy();
                    playable = input;
                }
            }
            /// <summary>
            /// Dispose playable resources
            /// </summary>
            public virtual void Dispose()
            {

            }
        }
        
        public class AnimationMontageNode : AnimationPlayableNode
        {
            public AnimationMontageNode Parent;
            
            public AnimationPlayableNode Child; /* Can be montage or normal playable node */
            
            public SchedulerHandle BlendHandle;
            
            public AnimationMontageNode(Playable playable, RuntimeAnimatorController runtimeAnimatorController) : base(playable, runtimeAnimatorController)
            {

            }

            /// <summary>
            /// Whether node is composite root, which is child only
            /// </summary>
            /// <returns></returns>
            public bool IsCompositeRoot()
            {
                return Parent == null;
            }
            
            /// <summary>
            /// Whether node can be shrink to optimize graph structure when parent is completly blend out
            /// </summary>
            /// <returns></returns>
            public virtual bool CanShrink()
            {
                // Already reach to root
                if (IsCompositeRoot()) return false;
                // No optimized space
                if (Playable.GetInputCount() < 2) return false;
                // Not complete blend yet
                return Playable.GetInputWeight(1) == 1 && Playable.GetInputWeight(0) == 0;
            }
            
            /// <summary>
            /// Dispose playable resources recursively
            /// </summary>
            public override void Dispose()
            {
                BlendHandle.Dispose();
                Child?.Dispose();
                Child = null;
                Parent = null;
            }
            
            /// <summary>
            /// Cross-fade parent to child by weight
            /// </summary>
            /// <param name="weight"></param>
            public virtual void Blend(float weight)
            {
                Playable.SetInputWeight(0, 1 - weight);
                Playable.SetInputWeight(1, weight);
            }
            
            /// <summary>
            /// Get cross-fade current weight
            /// </summary>
            /// <returns></returns>
            public virtual float GetBlendWeight()
            {
                if (Playable.GetInputCount() > 1)
                    return 1 - Playable.GetInputWeight(0);
                return 0;
            }
            
            /// <summary>
            /// Shrink link list to release not used playables
            /// </summary>
            /// <returns>Current leaf node</returns>
            public AnimationMontageNode Shrink()
            {
                if (!CanShrink())
                {
                    return this;
                }
                // Disconnect child output first
                Graph.Disconnect(Playable, 1);
                Parent.SetChild(Child);
                var parent = Parent;
                // Release playable
                Child = null;
                Parent = null;
                Destroy();
                return parent.Shrink();
            }
            
            private void SetChild(AnimationPlayableNode newChild)
            {
                Child = newChild;
                Graph.Disconnect(Playable, 1);
                Graph.Connect(newChild.Playable, 0, Playable, 1);
            }
            
            public void ScheduleBlendIn(float duration, Action callBack = null)
            {
                Scheduler.Delay(ref BlendHandle, duration, () => { Blend(1); callBack?.Invoke(); }, x => Blend(x / duration));
            }
            
            public void ScheduleBlendOut(float duration, Action callBack = null)
            {
                Scheduler.Delay(ref BlendHandle, duration, () => { Blend(0); callBack?.Invoke(); }, x => Blend(1 - x / duration));
            }
            
            public static AnimationMontageNode operator |(AnimationMontageNode left, AnimationPlayableNode right)
            {
                return CreateMontage(left, right);
            }
            
            #region Factory

            /// <summary>
            /// Create layer montage
            /// </summary>
            /// <param name="source"></param>
            /// <param name="context"></param>
            /// <param name="contexts"></param>
            /// <returns></returns>
            public static AnimationLayerMontageNode CreateLayerMontage(AnimationMontageNode source, LayerContext context, LayerContext[] contexts)
            {
                var playablePtr = source.Playable;
                var graph = playablePtr.GetGraph();
                var newMixer = AnimationLayerMixerPlayable.Create(graph, contexts.Length + 1);
                var children = new AnimationMontageNode[contexts.Length];

                // Set weight
                newMixer.SetInputWeight(0, 1);
                for (int i = 1; i < contexts.Length + 1; i++)
                {
                    newMixer.SetInputWeight(1, 0);
                }
                // Use layer to start blend in, so set original source to completed blend in
                source.Blend(1);

                var descriptor = context.Descriptor;

                // Assign by input context
                if (context.Handle.IsValid())
                {
                    children[context.Descriptor.Index] = source;
                    int inputPortIndex = (int)(context.Descriptor.Index + 1);
                    graph.Connect(playablePtr, 0, newMixer, inputPortIndex);
                    newMixer.SetLayerMaskFromAvatarMask(context.Descriptor.Index + 1, descriptor.AvatarMask);
                    newMixer.SetLayerAdditive(context.Descriptor.Index + 1, descriptor.Additive);
                }
                else
                {
                    // No specific layer use default layer one
                    // Notice that we start proxy layer from 1, since layer 0 is always placed with source animator controller
                    children[0] = source;
                    const int defaultInputPortIndex = 1;
                    graph.Connect(playablePtr, 0, newMixer, defaultInputPortIndex);
                }


                return new AnimationLayerMontageNode(newMixer, source.AnimatorController)
                {
                    Parent = null,
                    Child = source,
                    Children = children
                };
            }

            /// <summary>
            /// Create blendable montage with child only, parent is out of proxy scope
            /// </summary>
            /// <param name="source"></param>
            /// <returns></returns>
            public static AnimationMontageNode CreateChildOnlyMontage(AnimationPlayableNode source)
            {
                var playablePtr = source.Playable;
                var graph = playablePtr.GetGraph();
                var newMixer = AnimationMixerPlayable.Create(graph, 2);

                // Only has child
                graph.Connect(playablePtr, 0, newMixer, 1);

                // Set weight
                newMixer.SetInputWeight(0, 1);
                newMixer.SetInputWeight(1, 0);

                return new AnimationMontageNode(newMixer, source.AnimatorController)
                {
                    Parent = null,
                    Child = source
                };
            }
            /// <summary>
            /// Create blendable montage from parent montage to child playable
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="source"></param>
            /// <returns></returns>
            public static AnimationMontageNode CreateMontage(AnimationMontageNode parent, AnimationPlayableNode source)
            {
                var playablePtr = source.Playable;
                var graph = parent.Graph;
                var leafMontage = parent.Playable;
                var leafNode = parent.Child;

                // Layout as a binary tree
                var newMontage = AnimationMixerPlayable.Create(graph, 2);

                // Disconnect right leaf from leaf montage
                graph.Disconnect(leafMontage, 1);
                // Current right leaf => New left leaf
                graph.Connect(leafNode.Playable, 0, newMontage, 0);
                // New right leaf
                graph.Connect(playablePtr, 0, newMontage, 1);
                // Connect to parent
                graph.Connect(newMontage, 0, leafMontage, 1);
                // Set weight
                newMontage.SetInputWeight(0, 1);
                newMontage.SetInputWeight(1, 0);

                var newMontageNode = new AnimationMontageNode(newMontage, source.AnimatorController)
                {
                    Parent = parent,
                    Child = source
                };
                // Link child
                parent.Child = newMontageNode;

                return newMontageNode;
            }
            #endregion Factory
        }
        public class AnimationLayerMontageNode : AnimationMontageNode
        {
            public AnimationLayerMontageNode(Playable playable, RuntimeAnimatorController runtimeAnimatorController) : base(playable, runtimeAnimatorController)
            {

            }
            public AnimationMontageNode[] Children;
            public override bool CanShrink()
            {
                /* Can not know whether any layer still need parent or not */
                return false;
            }
            public override void Blend(float weight)
            {
                /* No need to blend out parent in layer montage since we always keep it in graph */
                for (int i = 0; i < Children.Length; ++i)
                {
                    Playable.SetInputWeight(i + 1, weight);
                }
            }
        }
    }
}
