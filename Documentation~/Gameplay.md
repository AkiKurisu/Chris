# Gameplay

Based on Ceres to integrate visual scripting and C#. Also contains some tools that may be useful.

## Gameplay Architecture

Gameplay architecture is quite simple like Unreal.

### GameWorld

`GameWorld` is a gameplay level manager of scenes.

### Subsystem

`WorldSubsystem` is a singleton for each `GameWorld`.

### Actor

`Actor` is a gameplay level entity and is managed by `GameWorld`.

## Animation Proxy

`AnimationProxy` is a wrapper of `Animator` to play montage and sequence by script directly.

- Use `AnimationProxy` to play montage by `RuntimeAnimatorController` and `AnimationClip`.
- Support multi layers.
- Support subscribe events.
- Support visual scripting.

![AnimationProxy](./Images/animation_proxy.png)

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

### Debugging

Recommend to use [Unity PlayableGraph Monitor](`https://github.com/SolarianZ/UnityPlayableGraphMonitorTool`).

## Audios and FX

`AudioSystem` and `FXSystem` are designed to manage audio and fx within a scene using a general pooling method based on `Chris.Pool`.

- Support load from Addressables.
- Support visual scripting.
- Allocation optimization.

![FX Audios](./Images/fx_audio.png)

## Capture

The capture module provides a comprehensive screenshot and image capture system with support for both runtime and editor usage, visual scripting integration, and cross-platform gallery saving.

### ScreenshotTool

`ScreenshotTool` is a high-level component integrated with `Ceres.Flow` for visual scripting support and automated screenshot workflows.


![ScreenshotTool](./Images/screenshot_tool.png)

#### Properties

- **SuperSize** (1-4): Resolution multiplier for higher quality captures
- **SourceCamera**: Camera used for capture (defaults to Camera.main)
- **ScreenshotMode**: Camera or Screen capture mode
- **EnableHDR**: Enable HDR rendering for captures

#### Implementation Example

```C#
public class CustomScreenshotTool : ScreenshotTool
{
    public override void OnTakeScreenshotStart()
    {
        // Hide UI elements before capture
        UIManager.HideUI();
    }
    
    public override void OnTakeScreenshotEnd()
    {
        // Restore UI elements after capture
        UIManager.ShowUI();
    }
}
```

### Flow Graph Integration

The capture module provides four executable functions for visual scripting in `Ceres.Flow`:

#### Flow_CaptureRawScreenshot

```C#
[ExecutableFunction, CeresLabel("Capture Raw Screenshot"), CeresGroup("Gameplay/Capture")]
public static Texture2D Flow_CaptureRawScreenshot(Camera camera, Vector2 size, 
    int depthBuffer = 24, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32)
```

Synchronously captures a raw screenshot from the specified camera with custom resolution and format settings.

#### Flow_CaptureRawScreenshotAsync

```C#
[ExecutableFunction, CeresLabel("Capture Raw Screenshot"), CeresGroup("Gameplay/Capture")]
public static void Flow_CaptureRawScreenshotAsync(Camera camera, Vector2 size, 
    int depthBuffer = 24, RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32, 
    Action<Texture2D> onComplete = null)
```

Asynchronously captures a raw screenshot with callback support for non-blocking operations.

#### Flow_CaptureScreenShotFromScreen

```C#
[ExecutableFunction, CeresLabel("Capture Screenshot"), CeresGroup("Gameplay/Capture")]
public static Texture2D Flow_CaptureScreenShotFromScreen()
```

Synchronously captures the current screen content. Must be called at end of frame.

#### Flow_CaptureScreenshotAsync

```C#
[ExecutableFunction, CeresLabel("Capture Screenshot Async"), CeresGroup("Gameplay/Capture")]
public static void Flow_CaptureScreenshotAsync(Action<Texture2D> onComplete)
```

Asynchronously captures the current screen content with callback support.

## Actor Hotupdate

`Actor` is integrated with `Ceres.Flow` to support implementing logic in visual scripting.

Here is a simple example of how to implement a hotupdateable actor.

1. Create a `DataTable` named `ActorFlowGraphDataTable`.
2. Set `Row Type` to `Chris.Gameplay.ActorFlowGraphRow` and the dataTable will be automatically registered to Addressables.
3. Assign an address to `Advanced Settings/Actor Address` in your actor.

    ![Assign Address](./Images/assign_actor_address.png)

4. Create a new row in `ActorFlowGraphDataTable` and set `Row Id` to the address you assigned in step 3.

    ![Create Row](./Images/new_actor_row.png)

5. Enable `Remote Update` and overwrite remote update path if need.
6. Edit your actor flow graph and click `Save`.
7. Click `Download` button in `Advanced Settings` to export asset as an assetbundle.

    ![Export Asset](./Images/export_flow_asset.png)

    > [!TIP]
    > You can set `Remote Update/Serialize Mode` to `PreferText` in `Project Settings/Chris/Gameplay Settings` to export asset as a text file if has no asset dependencies for better readability.

8. Drag the bundle to `{Saved Path}/Flow` to update the actor.

### Saved Path

Saved path mentioned above is different for different platforms.

| Platform    | Path                            |
| ----------- | :-----------------------------: |
| Windows     | `Build Path` / Saved              |
| Android     | `Persistent Data Path` / Saved    |
