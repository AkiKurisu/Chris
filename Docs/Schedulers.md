# Schedulers

Zero allocation timer/frame counter.

## API

```C#
// Scheduler.cs
// Delegate version
SchedulerHandle Delay(float delay, Action callBack, Action<float> onUpdate, TickFrame tickFrame = TickFrame.Update, bool isLooped = false, bool ignoreTimeScale = false);

SchedulerHandle WaitFrame(int frame, Action callBack, Action<int> onUpdate, TickFrame tickFrame = TickFrame.Update, bool isLooped = false);

// Unsafe version
void DelayUnsafe(ref SchedulerHandle handle, float delay, SchedulerUnsafeBinding callBack, SchedulerUnsafeBinding<float> onUpdate, 
TickFrame tickFrame = TickFrame.Update, bool isLooped = false, bool ignoreTimeScale = false);

void WaitFrameUnsafe(ref SchedulerHandle handle, int frame, SchedulerUnsafeBinding callBack, SchedulerUnsafeBinding<int> onUpdate, 
TickFrame tickFrame = TickFrame.Update, bool isLooped = false);
```


## Example

```C#
public class SchedulerTest : MonoBehaviour
{
    public void DelegateAPI()
    {
        Scheduler.Delay(1, Log);
    }

    public void UnsafeAPI()
    {
        Scheduler.DelayUnsafe(1, (SchedulerUnsafeBinding)(&Log));
    }

    public void UnsafeAPIWithObject()
    {
        Scheduler.DelayUnsafe(1, new SchedulerUnsafeBinding(this, &LogWithObject));
    }

    private static void Log()
    {
        Debug.Log("Log");
    }

    private static void LogWithObject(object @object)
    {
        Debug.Log($"Log {(@object as SchedulerTest).gameObject.name}");
    }
}

```

## Debugger

![Debugger](./Images/scheduler_debugger.png)

To use debugger, need enable `Stack Trace` in `Project Settings/CeresSettings`.

Notice that this will add GC allocation in Editor, which will disturb you when profiling.