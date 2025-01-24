<div align="center">

# Chris

A Unity game framework designed for efficient development.

</div>

## Install

Use git URL to download package by Unity Package Manager ```https://github.com/AkiKurisu/Chris.git```

## Core Features

[Events](./Docs/Events.md) 
> A powerful event solution for dynamic and contextual event handling ported from UIElement.

![Debugger](./Docs/Images/debugger.png)

[Pool](./Docs/Pool.md) 
> Zero allocation GameObject/Component pooling. 

![Pooling Performance](./Docs/Images/pooling-performance.png)

[Schedulers](./Docs/Schedulers.md) 
> Zero allocation timer/frame counter. 

![Debugger](./Docs/Images/scheduler_debugger.png)

[Serialization](./Docs/Serialization.md)
> Powerful serialization tool for workflow.

![SerializedType](./Docs/Images/serializedtype.png)

[Resource](./Docs/Resource.md) 
> Resource loading system, effect system based on Addressables. 

![SoftAssetReference](./Docs/Images/soft_asset_reference.png)

[Data Driven](./Docs/DataDriven.md)
>Use Unreal-like DataTable workflow in Unity.

![DataTable](./Docs/Images/datatable_editor_window.png)

## Modules

Modules are based on core features.

To install modules, use git URL to download package by Unity Package Manager ```https://github.com/AkiKurisu/Chris-Modules.git```

[Gameplay](./Docs/Gameplay.md)
>Based on Ceres to integrate visual scripting and C#. Also contains some tools that may be useful.

![Visual Scripting](./Docs/Images/visual_scripting.png)

[AI](./Docs/AI.md)

>Personal AI tools from AAA games research. 

![FieldViewPrime](./Docs/Images/fov-prime.gif)

[Mod](./Docs/Mod.md) 
> Mod workflow based on Addressables. 

![Mod Exporter](./Docs/Images/mod_exporter.png)

[UI](./Docs/UI.md) 
> Build UGUI in script easily, useful for creating dynamic setting UI and debug menu.

![UIPanel](./Docs/Images/ui_panel_view.png)

## Dependencies

```json
"dependencies": {
    "com.cysharp.unitask":"2.5.3",
    "com.unity.addressables": "1.21.0",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.collections": "2.2.1",
    "com.unity.burst": "1.8.9",
    "com.unity.mathematics": "1.3.1"
  }
```

## Reference

[R3](https://github.com/Cysharp/R3)

[UniTask](https://github.com/Cysharp/UniTask)

[Unity.UIElements](https://github.com/Unity-Technologies/UnityCsReference/tree/2022.3/ModuleOverrides/com.unity.ui/Core)

[Unity Timer](https://github.com/akbiggs/UnityTimer)

[Ceres](https://github.com/AkiKurisu/Ceres)

## License

MIT