# Mod

Efficient mod workflow based on Addressables. 

## Features

- Build mods in other project for your game.

## Version

Unity 2022.3

> TODO: Support Unity 6

## Runtime API

```C#
// Only support async way using UniTask
private async UniTaskVoid LoadMod()
{
    // Setting defines mod's on and off
    ModSetting setting = new();
    await ModAPI.Initialize(setting, new ModImporter(setting));
}
```
## Editor Export

Use `Mod Exporter` to create new addressable group and build only the mod group you edited.

You can inherit ``CustomBuilder`` and add it to export config to write mod additional meta data such as game assets sub catalog into `ModInfo` or make a pre-process such as looping the group's addressable entries.

![Mod Exporter](./Images/mod_exporter.png)

## Build Notice

If you build mod in source project, you should add `DefaultBundleNamePatchBuilder` to export config for preventing bundle name conflict.

## Reference
>https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/MultiProject.html.
