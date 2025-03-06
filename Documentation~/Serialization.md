# Serialization

Powerful serialization tool for workflow.

## SerializedType\<T\>

Serialize type of class implementing T and get new object from it at runtime.

```C#
SerializedType<ICustomInterface> myType;

ICustomInterface customInterface = myType.GetObjectType();
```

![SerializedType](./Images/serializedtype.png)

## SerializedObject\<T\>

More managable than Unity's `SerializeReference` attribute.

Serialize type and data of object implementing T and get template object from it at runtime.

You can edit all properties in the Insepector like `ScriptableObject` while finally serializing as a simple C# object.

```C#
SerializedObject<ICustomInterface> myObject;

ICustomInterface customInterface = myObject.GetObject();
```

### SerializedObject vs SerializeReference

When you need to serialize Unity Object reference like assets and prefabs, nice to use SerializeReference.

However, you can not handle the reference object lifetimescope in C#, since they are managed by Engine C++ part.

But for most of time when you only need a config without Object references, nice to use framework's SerializedObject.

### Usage Example

Framework's `DataTable` use SerializedObject to support polymorphic serialization of `IDataTableRow`.

See [Data Driven Document](./DataDriven.md)

## SerializedObject Editor

Since Unity's Editor need everything to be UnityEngine.Object, the framework emits IL to dynamically create ScriptableObject wrapper.

However, those wrapper will not actually be serialized but need to be tracked in memory. 
So the framework use a soft object management like Unreal's SoftObjectPtr, see `GlobalObjectManager.cs`.