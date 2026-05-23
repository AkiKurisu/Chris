# Chris Core API Index

All paths are relative to `{CHRIS_ROOT}` (see SKILL.md for how to resolve it).

---

## Pool — `Chris.Pool`

**Path:** `Core/Runtime/Pool/`

| File | Class | Role |
|---|---|---|
| `PooledGameObject.cs` | `PooledGameObject : IDisposable` | Main developer-facing pool wrapper |
| `PooledComponent.cs` | `PooledComponent<T, TComponent>` | Typed component wrapper |
| `GameObjectPoolManager.cs` | `GameObjectPoolManager` | Singleton MonoBehaviour backing the pool |
| `UniTaskPool.cs` | `UniParallel` | Pooled list of UniTasks for parallel execution |

**Key API:**
```csharp
// Acquire (RAII — Dispose() returns to pool)
PooledGameObject.Get(string address, Transform parent = null)
PooledComponent<T, TComponent>.Get(Transform parent = null)
PooledComponent<T, TComponent>.Get(GameObject prefab, Transform parent = null)

// Parallel async helper
await using var p = UniParallel.Get();
p.Add(SomeUniTask());
```

---

## Events — `Chris.Events`

**Path:** `Core/Runtime/Events/`

| File | Class | Role |
|---|---|---|
| `EventSystem.cs` | `EventSystem` | Singleton coordinator |
| `Models/EventBase.cs` | `EventBase<T>` | Base for all pooled event types |
| `Models/Handler/EventHandler.cs` | `CallbackEventHandler` | Send/receive handler node |
| `Models/ChangeEvent.cs` | `ChangeEvent<T>` | Generic value-changed event |
| `Interfaces/` | `IEventHandler`, `IEventCoordinator` | Core interfaces |

**Key API:**
```csharp
// Global root handler
EventSystem.EventHandler.RegisterCallback<MyEvent>(evt => { });
EventSystem.EventHandler.UnregisterCallback<MyEvent>(callback);

// Send (always dispose pooled events)
var evt = MyEvent.GetPooled();
EventSystem.EventHandler.SendEvent(evt);
evt.Dispose();

// Handler hierarchy — parent broadcasts to children
handler.Parent = EventSystem.EventHandler;
```

**Dispatch modes:** `Default` (deferred), `Immediate`.  
**Propagation:** control via `Bubbles` / `TricklesDown` flags on `EventBase`.

---

## Configs — `Chris.Configs`

**Path:** `Core/Runtime/Configs/`

| File | Class | Role |
|---|---|---|
| `ConfigSystem.cs` | `ConfigSystem` (static) | Low-level provider registry |
| `ConfigBase.cs` | `Config<TConfig>` | Abstract base for user config classes |
| `ConfigFile.cs` | `IConfigFile` | File-level read/write |
| `Annotations/ConfigPathAttribute.cs` | `[ConfigPath]` | Marks config class path |
| `Annotations/ConfigVariableAttribute.cs` | `[ConfigVariable]` | Marks serializable field |

**Key API:**
```csharp
// Define
[ConfigPath("Settings.Graphics")]
class GraphicsSettings : Config<GraphicsSettings> {
    [ConfigVariable] public int Quality = 2;
}

// Access / persist
var cfg = GraphicsSettings.Get();
cfg.Quality = 3;
cfg.Save();

// Low-level
ConfigSystem.GetConfig<TConfig>()
ConfigSystem.RegisterConfigFileProvider(provider, priority)
```

**Providers (auto-registered):** `EditorConfigFileProvider` → `StreamingConfigFileProvider` → `PersistentConfigFileProvider` (user overrides win).

---

## Resource — `Chris.Resource`

**Path:** `Core/Runtime/Resource/`

| File | Class | Role |
|---|---|---|
| `ResourceSystem.cs` | `ResourceSystem` (static) | Addressables wrapper |
| `ResourceHandle.cs` | `ResourceHandle<T>` | Lightweight handle |
| `SoftAssetReference.cs` | `SoftAssetReference<T>` | Serializable address ref |

**Key API:**
```csharp
// Load
ResourceHandle<T> handle = ResourceSystem.LoadAssetAsync<T>(address, callback);

// Instantiate
ResourceHandle<GameObject> h = ResourceSystem.InstantiateAsync(address, parent, callback);

// Release
ResourceSystem.Release(handle);
ResourceSystem.ReleaseInstance(gameObject);

// Serializable reference (auto-caches handle)
[SerializeField] SoftAssetReference<Texture2D> icon;
var h = icon.LoadAsync(tex => { });
icon.Release();

// Validate before use (throws if missing in editor)
ResourceSystem.EnsureAssetExists<T>(key);
await ResourceSystem.EnsureAssetExistsAsync<T>(key);
```

---

## DataDriven — `Chris.DataDriven`

**Path:** `Core/Runtime/DataDriven/`

| File | Class | Role |
|---|---|---|
| `DataTableManager.cs` | `DataTableManager<TManager>` | Typed singleton manager |
| `DataTable.cs` | `DataTable : ScriptableObject` | Row container (Addressable) |
| `DataDrivenModule.cs` | `DataDrivenModule` | Auto-initializes registered managers |

**Key API:**
```csharp
// Define manager
class ItemTableManager : DataTableManager<ItemTableManager> {
    protected override async UniTask Initialize(bool sync) {
        await InitializeSingleTable("Items/ItemTable", sync);
    }
}

// Initialize (usually via DataDrivenModule)
await DataTableManager.InitializeAsync();

// Query
var mgr = ItemTableManager.Get();
DataTable table = mgr.GetDataTable("ItemTable");
ItemRow[] rows = table.GetAllRows<ItemRow>();
ItemRow row = table.GetRow<ItemRow>("item_001");
ItemRow[] filtered = table.GetRows<ItemRow>(r => r.Type == ItemType.Weapon);
```

**Row definition:** implement `IDataTableRow` on a `[Serializable]` struct/class.

---

## Schedulers — `Chris.Schedulers`

**Path:** `Core/Runtime/Schedulers/`

| File | Class | Role |
|---|---|---|
| `Models/Scheduler.cs` | `Scheduler` (static) | Primary scheduling API |
| `Models/SchedulerHandle.cs` | `SchedulerHandle` | Cancellable handle (disposable) |
| `Models/SchedulerExtensions.cs` | extensions | `WaitAsync`, `Assign` |

**Key API:**
```csharp
// Time-based
SchedulerHandle h = Scheduler.Delay(2f, OnComplete);
SchedulerHandle h = Scheduler.Delay(2f, OnComplete, progress => { }, TickFrame.Update);
SchedulerHandle h = Scheduler.Delay(2f, progress => { });          // progress 0→1

// Frame-based
SchedulerHandle h = Scheduler.WaitFrame(5, OnComplete);

// Ref overload (auto-cancels previous before starting new)
Scheduler.Delay(ref _handle, 1f, OnComplete);

// Cancel
h.Dispose();
h.IsValid();   h.IsDone();

// Bridge to UniTask
await h.WaitAsync(cancellationToken);

// Zero-alloc (function pointers)
Scheduler.DelayUnsafe(2f, new SchedulerUnsafeBinding(target, &StaticMethod));
```

**Tick frames:** `Update`, `FixedUpdate`, `LateUpdate`.

---

## Serialization — `Chris.Serialization`

**Path:** `Core/Runtime/Serialization/`

| File | Class | Role |
|---|---|---|
| `SaveUtility.cs` | `SaveUtility` (static) | Convenience save/load API |
| `SaveLoadSerializer.cs` | `SaveLoadSerializer` | Configurable serializer |
| `GlobalObjectManager.cs` | `GlobalObjectManager` (static) | Runtime object registry |
| `SoftObjectHandle.cs` | `SoftObjectHandle` | Version-stamped object reference |
| `SerializedObject.cs` | `SerializedObject<T>` | Polymorphic editor serialization |

**Key API:**
```csharp
// Simple save/load
SaveUtility.Save<MyData>(data);
MyData data = SaveUtility.Load<MyData>();
SaveUtility.Delete("MyData");

// Custom serializer
var s = new SaveLoadSerializer(rootPath, ".sav", BinarySerializeFormatter.Instance);
s.Serialize<T>(data);
T data = s.Deserialize<T>("key");

// Formatters: BinarySerializeFormatter.Instance
//             TextSerializeFormatter.Instance
//             new EncryptedSerializeFormatter(password)

// Runtime object handles
SoftObjectHandle handle = GlobalObjectManager.RegisterObject(myObj);  // or new SoftObjectHandle(obj)
GlobalObjectManager.TryGetObject(handle, out object obj);
handle.IsValid();
```

---

## Collections — `Chris.Collections`

**Path:** `Core/Runtime/Collections/`

| File | Class | Key Use Case |
|---|---|---|
| `SparseArray.cs` | `SparseArray<T>` | Stable indices, non-contiguous (Unreal TSparseArray) |
| `PriorityQueue.cs` | `PriorityQueue<T>` | Min-heap (`T : IComparable<T>`) |
| `RandomList.cs` | `RandomList<T>` | Weighted random with decay |

**Key API:**
```csharp
// SparseArray — O(1) add/remove, stable indices
var arr = new SparseArray<Actor>();
int idx = arr.Add(actor);
arr.Remove(idx);
bool live = arr.IsAllocated(idx);

// PriorityQueue
var pq = new PriorityQueue<MyTask>();
pq.Enqueue(task);
MyTask next = pq.Dequeue();

// RandomList — avoids repeating last N picks via decay
var rl = new RandomList<AudioClip>();
rl.Add(clip1, weight: 1.0);
rl.Add(clip2, weight: 2.0);
AudioClip next = rl.GetNext(decayFactor: 0.9);
```

---

## R3 Integration — `R3.Chris`

**Path:** `Core/Runtime/R3/`

| File | Class | Role |
|---|---|---|
| `ObservableExtensions.cs` | extensions on `CallbackEventHandler` | Bridge Events → R3 |
| `DisposableExtensions.cs` | `IDisposableUnregister`, `AddTo` | Lifetime management |
| `UGUIExtensions.cs` | extensions on UGUI controls | Two-way reactive binding |
| `ReactivePropertyConverter.cs` | JSON converter | Serialize `ReactiveProperty<T>` |

**Key API:**
```csharp
// Event → Observable
EventSystem.EventHandler
    .AsObservable<MyEvent>()
    .Subscribe(evt => { })
    .AddTo(disposableScope);

// Tie lifetime to a pooled object
myDisposable.AddTo(pooledGameObject);    // PooledGameObject : IDisposableUnregister

// UGUI two-way binding
slider.BindProperty(config.Volume, disposableScope);
toggle.BindProperty(config.Enabled, disposableScope);
```

---

## Tasks — `Chris.Tasks`

**Path:** `Core/Runtime/Tasks/`

| File | Class | Role |
|---|---|---|
| `Models/TaskBase.cs` | `TaskBase : CallbackEventHandler` | Abstract task (also an event handler) |
| `Models/SequenceTask.cs` | `SequenceTask` | Pooled sequential composite |
| `Models/TaskExtensions.cs` | `task.Run()` | Entry point |

**Key API:**
```csharp
// Define
class LoadTask : TaskBase {
    public override string GetTaskID() => "LoadTask";
    protected override void CompleteTask() => base.CompleteTask();
    public override void Tick() { /* per-frame work */ }
}

// Run
loadTask.Run();

// Sequence
SequenceTask seq = SequenceTask.GetPooled(taskA);
seq.Append(taskB).Append(taskC);
seq.Run();

// Task events (fires on TaskBase when completed)
taskA.RegisterCallback<TaskCompleteEvent>(evt => { });

// Prerequisite chaining
taskB.AddPrerequisite(taskA);  // taskB auto-starts when taskA completes
taskB.Run();                   // registers but waits
```

**Status:** `Running`, `Paused`, `Completed`, `Stopped`.

---

## Modules — `Chris.Modules`

**Path:** `Core/Runtime/Modules/`

| File | Class | Role |
|---|---|---|
| `RuntimeModule.cs` | `RuntimeModule` (abstract) | Framework boot extension point |
| `ModuleLoader.cs` | `ModuleLoader` (internal) | Auto-discovers and runs modules |

**Key API:**
```csharp
// Define a module — auto-discovered via reflection at BeforeSceneLoad
class MyModule : RuntimeModule {
    public override int Order => 200;    // lower = earlier; ConfigsModule runs first
    public override void Initialize() {
        // register services, start background work, etc.
    }
}
```

No registration needed. `ModuleLoader` finds all non-abstract `RuntimeModule` subclasses, sorts by `Order`, and calls `Initialize()` before the first scene loads.

> `ConfigsModule` always runs first (hard-coded). Use `Order < 100` to run before default modules.
