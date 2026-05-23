---
name: chris
description: Guide for using the Chris Unity framework (com.kurisu.chris). Covers Core systems (Pool, Events, Configs, Resource, DataDriven, Schedulers, Serialization, Collections, R3, Tasks, Modules) and Gameplay layer (GameWorld, Actor, Level, Audio, Animation, Graphics, AI/EQS, Mod, FX, Capture). Use when implementing features using Chris APIs, debugging Chris subsystems, understanding Chris architecture, or finding entry points for any Chris system.
---

# Chris API Guide

## Step 1: Locate the Chris Package

Before referencing any path, resolve `{CHRIS_ROOT}` using these two methods:

### Method A — Check `manifest.json`
Find the Unity project's `Packages/manifest.json` and look for `com.kurisu.chris`:
```json
"com.kurisu.chris": "file:../../some/relative/path"
```
Resolve that path relative to the `Packages/` folder to get `{CHRIS_ROOT}`.

### Method B — Scan for `package.json`
Search for a `package.json` containing `"name": "com.kurisu.chris"` under any `Packages/` directory. Its parent folder is `{CHRIS_ROOT}`.

---

## Package Structure

```
{CHRIS_ROOT}/
├── Core/
│   ├── Runtime/        # All core runtime systems
│   └── Editor/         # Core editor tools
├── Gameplay/
│   ├── Runtime/        # Gameplay-level systems
│   └── Editor/
└── ThirdParty/         # Vendored: R3, SharpZipLib, NativeGallery, UnityIngameDebugConsole
```

**Assemblies:**

| Assembly | Path | Purpose |
|---|---|---|
| `Chris` | `Core/Runtime/` | Core runtime systems |
| `Chris.Editor` | `Core/Editor/` | Core editor tools |
| `Chris.Gameplay` | `Gameplay/Runtime/` | Gameplay-level systems |
| `Chris.Gameplay.Editor` | `Gameplay/Editor/` | Gameplay editor tools |

---

## Key Dependencies

- **UniTask** (`com.cysharp.unitask`) — all async APIs return `UniTask`, not `Task`
- **R3** (vendored in `ThirdParty/R3.1.3.0/`) — reactive properties and observables
- **Unity.Addressables** — `ResourceSystem` and `DataTableManager` wrap Addressables
- **Newtonsoft.Json** — serialization throughout; `SaveLoadSerializer` uses it by default

---

## Architecture Overview

### Core Layer (`Chris` assembly)

| Subsystem | Namespace | Entry Point |
|---|---|---|
| Object Pool | `Chris.Pool` | `GameObjectPoolManager`, `PooledGameObject` |
| Event System | `Chris.Events` | `EventSystem.EventHandler` |
| Config System | `Chris.Configs` | `ConfigSystem.GetConfig<T>()`, `Config<T>.Get()` |
| Resource System | `Chris.Resource` | `ResourceSystem.*`, `SoftAssetReference<T>` |
| Data-Driven | `Chris.DataDriven` | `DataTableManager<T>.Get()`, `DataTable` |
| Schedulers | `Chris.Schedulers` | `Scheduler.Delay(...)`, `Scheduler.WaitFrame(...)` |
| Serialization | `Chris.Serialization` | `SaveUtility.*`, `SaveLoadSerializer` |
| Collections | `Chris.Collections` | `SparseArray<T>`, `PriorityQueue<T>`, `RandomList<T>` |
| R3 Integration | `R3.Chris` | `handler.AsObservable<T>()`, `AddTo(scope)` |
| Tasks | `Chris.Tasks` | `task.Run()`, `SequenceTask` |
| Modules | `Chris.Modules` | Subclass `RuntimeModule`, override `Initialize()` |

### Gameplay Layer (`Chris.Gameplay` assembly)

| Subsystem | Namespace | Entry Point |
|---|---|---|
| World / Actor | `Chris.Gameplay` | `GameWorld.Get()`, `ContainerSubsystem.Get()` |
| Level | `Chris.Gameplay.Level` | `LevelSystem.LoadAsync(...)` |
| Audio | `Chris.Gameplay.Audios` | `AudioSystem.*`, `VoiceProxy` |
| Animation | `Chris.Gameplay.Animations` | `AnimationProxy` |
| Graphics | `Chris.Gameplay.Graphics` | `GraphicsController.Get()`, `GraphicsConfig.Get()` |
| AI / EQS | `Chris.AI.EQS` | `PostQueryComponent`, `FieldViewQueryComponent` |
| Mod System | `Chris.Gameplay.Mod` | `ModAPI.*` |
| FX | `Chris.Gameplay.FX` | `FXSystem.*` |
| Capture | `Chris.Gameplay.Capture` | `ScreenshotUtility.*`, `GalleryUtility.*` |

---

## Detailed API Reference

- For Core subsystem paths and API summaries → see [core.md](core.md)
- For Gameplay subsystem paths and API summaries → see [gameplay.md](gameplay.md)

---

## Common Cross-Cutting Patterns

```csharp
// 1. Lifetime management — tie disposables to scoped objects
someDisposable.AddTo(pooledGameObject);          // IDisposableUnregister

// 2. Event → R3 bridge
EventSystem.EventHandler.AsObservable<MyEvent>()
    .Subscribe(evt => { ... })
    .AddTo(disposableScope);

// 3. Module boot order — subclass RuntimeModule, system auto-discovers it
class MyModule : RuntimeModule {
    public override int Order => 200;
    public override void Initialize() { ... }
}

// 4. Zero-alloc scheduling
var handle = Scheduler.Delay(2f, OnComplete);
handle.Dispose();                                // cancel early

// 5. Config access pattern
var cfg = MyConfig.Get();                        // Config<MyConfig>.Get()
cfg.SomeField = newValue;
cfg.Save();
```
