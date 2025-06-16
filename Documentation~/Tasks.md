# Tasks

Asynchronous task system with prerequisites and event-driven completion.

## Overview

The Tasks module provides a comprehensive asynchronous task management system that supports task dependencies, event-driven completion, and automatic lifecycle management. Tasks can have prerequisites and notify listeners when completed.

## Core Classes

### TaskBase

Abstract base class for all framework tasks:

```csharp
public class MyCustomTask : TaskBase
{
    public override string GetTaskID() => "MyCustomTask";
    
    public override void Start()
    {
        base.Start();
        // Initialize task
    }
    
    public override void Tick()
    {
        // Update task logic
        if (IsComplete())
        {
            CompleteTask();
        }
    }
}
```

### PooledTaskBase<T>

Memory-efficient pooled task implementation:

```csharp
public class PooledDelayTask : PooledTaskBase<PooledDelayTask>
{
    private float delay;
    private float elapsed;
    
    public static PooledDelayTask Create(float delayTime)
    {
        var task = GetPooled();
        task.delay = delayTime;
        task.elapsed = 0f;
        return task;
    }
    
    public override void Tick()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= delay)
        {
            CompleteTask();
        }
    }
    
    protected override void Reset()
    {
        base.Reset();
        delay = 0f;
        elapsed = 0f;
    }
}
```

## Task Status Management

Tasks have four possible states:

```csharp
public enum TaskStatus
{
    Running,    // Task is active and updating
    Paused,     // Task is paused and ignored
    Completed,  // Task finished successfully
    Stopped     // Task was terminated
}
```

### Lifecycle Methods

```csharp
var task = new MyCustomTask();

// Control task execution
task.Start();    // Begin execution
task.Pause();    // Temporarily pause
task.Stop();     // Terminate task
task.Tick();     // Update (called by TaskRunner)

// Check status
var status = task.GetStatus();
```

## Prerequisites System

Tasks can depend on other tasks completing first:

```csharp
var prerequisiteTask = new LoadDataTask();
var mainTask = new ProcessDataTask();

// mainTask will only start after prerequisiteTask completes
mainTask.RegisterPrerequisite(prerequisiteTask);

// Start both tasks
prerequisiteTask.Start();
mainTask.Start(); // Will wait for prerequisite
```

### Prerequisite Management

```csharp
// Add prerequisite
mainTask.RegisterPrerequisite(prerequisiteTask);

// Remove prerequisite
mainTask.UnregisterPrerequisite(prerequisiteTask);

// Check if task has prerequisites
bool hasPrereqs = mainTask.HasPrerequisite();
```

## Event-Driven Completion

Tasks use the event system for completion notifications:

```csharp
public class TaskListener : MonoBehaviour
{
    private void Start()
    {
        var task = new MyCustomTask();
        
        // Listen for task completion
        task.RegisterCallback<TaskCompleteEvent>(OnTaskComplete);
        
        task.Start();
    }
    
    private void OnTaskComplete(TaskCompleteEvent evt)
    {
        Debug.Log($"Task {evt.Task.GetTaskID()} completed!");
        
        // Access completed task
        var completedTask = evt.Task;
        
        // Clean up
        evt.Task.UnregisterCallback<TaskCompleteEvent>(OnTaskComplete);
    }
}
```

## Built-in Task Types

### DelayTask

Simple time-based delay:

```csharp
var delayTask = new DelayTask(2.0f); // 2 second delay
delayTask.Start();
```

### SequenceTask

Execute multiple tasks in sequence:

```csharp
var sequence = new SequenceTask();
sequence.AddTask(new LoadTask());
sequence.AddTask(new ProcessTask());
sequence.AddTask(new SaveTask());

sequence.Start(); // Executes tasks in order
```

## Task Runner Integration

Tasks are managed by the `TaskRunner` component:

```csharp
public class GameManager : MonoBehaviour
{
    private TaskRunner taskRunner;
    
    private void Awake()
    {
        taskRunner = gameObject.AddComponent<TaskRunner>();
    }
    
    private void StartGameSequence()
    {
        var initTask = new InitializeGameTask();
        var loadTask = new LoadLevelTask();
        
        loadTask.RegisterPrerequisite(initTask);
        
        // TaskRunner will manage lifecycle
        taskRunner.AddTask(initTask);
        taskRunner.AddTask(loadTask);
    }
}
```

## Memory Management

### Pooled Tasks

Use `PooledTaskBase<T>` for frequently created tasks:

```csharp
// Efficient creation and disposal
using var delayTask = PooledDelayTask.Create(1.0f);
delayTask.Start();
// Automatically returned to pool when disposed
```

### Manual Disposal

```csharp
var task = new MyCustomTask();
task.Start();

// Manual cleanup when done
task.Dispose();
```

## Advanced Features

### Task Hierarchies

Tasks can have parent-child relationships:

```csharp
var parentTask = new ParentTask();
var childTask = new ChildTask();

childTask.SetParentEventHandler(parentTask);
// Child events bubble up to parent
```

### Custom Task Events

Create custom events for task communication:

```csharp
public class TaskProgressEvent : EventBase<TaskProgressEvent>, ITaskEvent
{
    public float Progress { get; private set; }
    
    public static TaskProgressEvent GetPooled(float progress)
    {
        var evt = GetPooled();
        evt.Progress = progress;
        return evt;
    }
}

// In your task
using var progressEvent = TaskProgressEvent.GetPooled(0.5f);
SendEvent(progressEvent);
```

## Best Practices

1. **Use pooled tasks** for frequently created/destroyed tasks
2. **Implement proper cleanup** in Reset() method for pooled tasks
3. **Avoid circular dependencies** in prerequisite chains
4. **Use events for loose coupling** between tasks and systems
5. **Dispose tasks properly** to prevent memory leaks
6. **Keep Tick() methods lightweight** for performance

## Integration with Other Modules

- **Events** - Task completion and progress notifications
- **Pool** - Memory-efficient task object reuse
- **Schedulers** - Time-based task scheduling
- **Serialization** - Task state persistence and restoration
