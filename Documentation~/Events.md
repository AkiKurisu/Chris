# Events

Support dynamic and contextual event handling, ported from `UnityEngine.UIElements`.

### Custom Event

```C#
// Interface for your custom event
// Debugger will notify it and group them
public interface ICustomEvent { }

public class MyCustomEvent : EventBase<MyCustomEvent>, ICustomEvent
{
    public string Message { get; private set; }

    protected override void Init()
    {
        base.Init();
        Message = string.Empty;
    }

    public static MyCustomEvent GetPooled(string message)
    {
        var ce = GetPooled();
        ce.Message = message;
        return ce;
    }
}

public class MyCustom2Event : EventBase<MyCustom2Event>, ICustomEvent
{
    public string Message { get; private set; }

    protected override void Init()
    {
        base.Init();
        Message = string.Empty;
    }

    public static MyCustom2Event GetPooled(string message)
    {
        var ce = GetPooled();
        ce.Message = message;
        return ce;
    }
}
```

### Event System

Event System is an implementation for global event purpose.

```C#
public class EventSystemExample : MonoBehaviour
{
    private void Awake()
    {
        EventSystem.EventHandler.RegisterCallback<MyCustomEvent>(HandleEvent1);
        EventSystem.EventHandler.RegisterCallback<MyCustom2Event>(HandleEvent2);
    }

    private void Start()
    {
        using var ce1 = MyCustomEvent.GetPooled("Hello");
        EventSystem.SendEvent(ce1);
    }

    private void OnDestroy()
    {
        EventSystem.EventHandler.UnregisterCallback<MyCustomEvent>(HandleEvent1);
        EventSystem.EventHandler.UnregisterCallback<MyCustom2Event>(HandleEvent2);
    }

    private void HandleEvent1(MyCustomEvent e)
    {
        Debug.Log(e.Message);
        using var ce2 = MyCustom2Event.GetPooled("World");
        EventSystem.SendEvent(ce2);
    }

    private void HandleEvent2(MyCustom2Event e)
    {
        Debug.Log(e.Message);
    }
}
```
### Events Debugger

Events can be tracked in a debugger `Tools/Chris/Event Debugger`.

![Debugger](./Images/debugger.png)


### Record Events

Record events state and resend to target event handler.

Recommend to install `jillejr.newtonsoft.json-for-unity.converters` to solve serialization problem with `Newtonsoft.Json`.

### Dispatch Events On Specified Frame

Dispatching events on specified frames is a simple and effective scheduling strategy.

If you need longer scheduling, you can use `Scheduler`.

```C#
public class MonoDispatchExample : MonoBehaviour
{
    private void Start()
    {
        EventSystem.EventHandler.RegisterCallback<MyCustomEvent>()(e =>
        {
            Debug.Log(e.Message);
        });

        using var ce1 = MyCustomEvent.GetPooled("Hello");
        EventSystem.SendEvent(ce1, monoDispatchType: MonoDispatchType.LateUpdate);

        using var ce2 = MyCustomEvent.GetPooled("World");
        EventSystem.SendEvent(ce2, monoDispatchType: MonoDispatchType.Update);
    }
    // Will receive `World` first, then `Hello`
}
```

### Customize Dispatching Strategy

Useful for Gameplay level event customization.