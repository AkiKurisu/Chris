# Gameplay

Based on Ceres to integrate visual scripting and C#. Also contains some tools that may be useful.

## Animations

- Use `AnimationProxy` to play montage by `RuntimeAnimatorController` and `AnimationClip`.
- Support multi layers and events.
- Use `AnimationPreviewer` to preview `AnimationClip` in Editor Mode and Play Mode.

### AnimationProxy Example

```C#
public class MontageExample : MonoBehaviour
{
    public Animator animator;
    private AnimationProxy animationProxy;
    public RuntimeAnimatorController controllerA;
    public RuntimeAnimatorController controllerB;
    private IEnumerator Start()
    {
        animationProxy = new AnimationProxy(animator);
        animationProxy.LoadAnimator(controllerA, 0.5f); /* Crossfade animator to controllerA in 0.5s */
        yield return new WaitForSeconds(1f);
        animationProxy.LoadAnimator(controllerB, 0.5f); /* Crossfade controllerA to controllerB in 0.5s */
        yield return new WaitForSeconds(1f);
        animationProxy.Stop(0.5f); /* Crossfade controllerB to animator in 0.5s */
    }
}
```

### SequenceBuilder Example

```C#
public class SequenceExample : MonoBehaviour
{
    public Animator animator;
    private AnimationProxy animationProxy;
    public AnimationClip[] clips;
    private void Start()
    {
        animationProxy = new AnimationProxy(animator);
        using var builder = animationProxy.CreateSequenceBuilder();
        foreach (var clip in clips)
        {
            builder.Append(clip, clip.length * 3 /* Play 3 loop */, 0.25f /* BlendIn duration */);
        }
        builder.SetBlendOut(0.5f);
        builder.Build().Run();
    }
}
```

### Debugger

Recommend to use [Unity PlayableGraph Monitor](`https://github.com/SolarianZ/UnityPlayableGraphMonitorTool`)

## Audios

- `AudioSystem` Use poolable audio source.

## FX

- `FXSystem` Use poolable particle system.