# Chris Gameplay API Index

All paths are relative to `{CHRIS_ROOT}` (see SKILL.md for how to resolve it).

---

## GameWorld / Actor Core — `Chris.Gameplay`

**Path:** `Gameplay/Runtime/Core/`

| File | Class | Role |
|---|---|---|
| `GameWorld.cs` | `GameWorld : MonoBehaviour` | Singleton scene container |
| `WorldContext.cs` | `WorldContext` (readonly struct) | Safe access wrapper for GameWorld |
| `Actor.cs` | `Actor : MonoBehaviour` | Base entity placed in the world |
| `ActorHandle.cs` | `ActorHandle` (readonly struct) | Versioned index identifying an Actor |
| `WorldSubsystem.cs` | `WorldSubsystem` (abstract) | World-lifetime service base |
| `ContainerSubsystem.cs` | `ContainerSubsystem : WorldSubsystem` | World-lifetime IoC container |
| `ActorQuerySystem.cs` | `ActorQuerySystem : WorldSubsystem` | NativeArray-based actor query |

**Subsystem auto-registration:** decorate your `WorldSubsystem` subclass with `[InitializeOnWorldCreate]` — it will be discovered and instantiated when `GameWorld` starts.

---

## Level — `Chris.Gameplay.Level`

**Path:** `Gameplay/Runtime/Level/`

| File | Class | Role |
|---|---|---|
| `LevelSystem.cs` | `LevelSystem` (static) | Level load/unload API |
| `LevelSceneRow.cs` | `LevelSceneRow` | DataTable row for a scene entry |

---

## Audio — `Chris.Gameplay.Audios`

**Path:** `Gameplay/Runtime/Audios/`

| File | Class | Role |
|---|---|---|
| `AudioSystem.cs` | `AudioSystem` (static) | Pooled spatial audio API |
| `AudioSourceHandle.cs` | `AudioSourceHandle` | Versioned handle to AudioSource |
| `VoiceProxy.cs` | `VoiceProxy` | Priority-queue character voice manager |
---

## Animation — `Chris.Gameplay.Animations`

**Path:** `Gameplay/Runtime/Animations/`

| File | Class | Role |
|---|---|---|
| `AnimationProxy.cs` | `AnimationProxy` (partial) | Playables-based animation controller |
| `AnimationSequenceBuilder.cs` | `AnimationSequenceBuilder` | Fluent clip sequence builder |

**Settings:**
- `proxy.ClearAnimatorControllerOnStart` (default `true`) — clears base controller when blended in
- `proxy.RestoreAnimatorControllerOnStop` (default `true`) — restores it on stop

---

## Graphics — `Chris.Gameplay.Graphics`

**Path:** `Gameplay/Runtime/Graphics/`

| File | Class | Role |
|---|---|---|
| `GraphicsController.cs` | `GraphicsController : MonoBehaviour` | URP/Volume driver |
| `GraphicsConfig.cs` | `GraphicsConfig : Config<GraphicsConfig>` | Persistent reactive settings |
| `GraphicsModule.cs` | `GraphicsModule` (abstract) | Extension plug-in |
| `GraphicsSettingsAsset.cs` | `GraphicsSettingsAsset : ScriptableObject` | Volume profile mapping |

---

## AI / EQS — `Chris.AI.EQS`

**Path:** `Gameplay/Runtime/AI/EQS/`

| File | Class | Role |
|---|---|---|
| `PostQueryComponent.cs` | `PostQueryComponent : ActorComponent` | Cover/post position queries |
| `FieldViewQueryComponent.cs` | `FieldViewQueryComponent : ActorComponent` | Field-of-view actor overlap |
| `PostQuerySystem.cs` | `PostQuerySystem : WorldSubsystem` | Jobs-based post query executor |
| `FieldViewQuerySystem.cs` | `FieldViewQuerySystem : WorldSubsystem` | Jobs-based FoV executor |

---

## Mod — `Chris.Gameplay.Mod`

**Path:** `Gameplay/Runtime/Mod/`

| File | Class | Role |
|---|---|---|
| `ModAPI.cs` | `ModAPI` (static) | Primary mod management entry point |
| `ModLoader.cs` | `ModLoader : IModLoader` | Default loader implementation |
| `ModInfo.cs` | `ModInfo` | Mod metadata |
| `ModConfig.cs` | `ModConfig` | Serialized enable/disable state |

---

## FX — `Chris.Gameplay.FX`

**Path:** `Gameplay/Runtime/FX/`

| File | Class | Role |
|---|---|---|
| `FXSystem.cs` | `FXSystem` (static) | Pooled particle system API |
| `PooledParticleSystem.cs` | `PooledParticleSystem : PooledComponent` | Auto-returns to pool |

---

## Capture — `Chris.Gameplay.Capture`

**Path:** `Gameplay/Runtime/Capture/`

| File | Class | Role |
|---|---|---|
| `ScreenshotTool.cs` | `ScreenshotTool : MonoBehaviour` | Scene-placed screenshot controller |
| `ScreenshotUtility.cs` | `ScreenshotUtility` (static) | Low-level capture primitives |
| `GalleryUtility.cs` | `GalleryUtility` (static) | Platform-aware save-to-gallery |

---

## Gameplay Flow Integration — `Chris.Gameplay.Flow`

**Path:** `Gameplay/Runtime/Flow/`

| File | Class | Role |
|---|---|---|
| `GameplayExecutableLibrary.cs` | `GameplayExecutableLibrary : ExecutableFunctionLibrary` | Exposes gameplay API to Ceres FlowGraph |

These functions are available inside FlowGraph nodes via `[ExecutableFunction]`:
```
Flow_GetSubsystem(type)          → SubsystemBase
Flow_GetOrCreateSubsystem(type)  → SubsystemBase
Flow_GetActor(ActorHandle)       → Actor
Flow_Play2DAudioClip(clip, vol)
Flow_Play2DAudioClipFromAddress(addr, vol)
```

---

## Singleton Asset Cache — `Chris.Gameplay.Resource`

**Path:** `Gameplay/Runtime/Resource/`

```csharp
// World-lifetime singleton asset cache (auto-disposed with GameWorld)
ResourceCache<MyAsset> cache = SingletonCache<MyAsset>.Instance;
```
