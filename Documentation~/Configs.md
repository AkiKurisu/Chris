# Configs

Global configuration management system with hierarchical organization and automatic serialization.

## Overview

The Configs module provides a robust configuration management system that allows you to create global settings with automatic caching, hierarchical organization, and flexible serialization. It supports both streaming assets (read-only) and persistent data (read-write) storage locations.

## Architecture

### Core Classes

**Config<TConfig>** - Generic base class for all global configurations
- Provides static `Get()` method for singleton access
- Automatic type-safe caching and serialization
- Support for hierarchical configuration paths
- Integration with Unity's JSON serialization and Newtonsoft.Json

**ConfigSystem** - Central configuration management system
- Manages configuration providers with priority-based loading
- Handles file caching and configuration instance caching
- Coordinates between streaming and persistent storage

**ConfigProvider** - Configuration file providers
- `StreamingConfigFileProvider` - Loads from StreamingAssets (read-only)
- `PersistentConfigFileProvider` - Loads from persistent data path (read-write)
- Priority-based provider system (Streaming: 200, Persistent: 100)

**ConfigFile** - Dictionary-based configuration file storage
- Stores multiple configurations in a single file as JSON
- Supports both Unity JsonUtility and Newtonsoft.Json serialization
- Automatic type detection based on `PreferJsonConvert` attribute

## Usage

### Creating a Configuration Class

```csharp
[Serializable]
[ConfigPath("MyGame.Graphics")]
public class GraphicsSettings : Config<GraphicsSettings>
{
    [SerializeField]
    public int targetFrameRate = 60;
    
    [SerializeField]
    public bool enableVSync = true;
    
    [SerializeField]
    public QualityLevel qualityLevel = QualityLevel.High;
}
```

### Accessing Configuration

```csharp
// Get configuration instance (automatically cached)
var graphics = GraphicsSettings.Get();

// Use configuration values
Application.targetFrameRate = graphics.targetFrameRate;
QualitySettings.vSyncCount = graphics.enableVSync ? 1 : 0;
```

### Saving Configuration Changes

```csharp
var graphics = GraphicsSettings.Get();
graphics.targetFrameRate = 120;
graphics.enableVSync = false;

// Save to persistent storage
graphics.Save();
```

### Configuration Path Hierarchy

The `ConfigPathAttribute` allows organizing configurations hierarchically:

```csharp
[ConfigPath("MyGame.Audio")]          // Root: MyGame, Name: Audio
[ConfigPath("MyGame.Audio.Music")]    // Root: MyGame, Name: Audio.Music
[ConfigPath("MyGame.Audio.SFX")]      // Root: MyGame, Name: Audio.SFX
```

Configurations with the same root path are stored in the same file, enabling efficient batch loading and saving.

## Configuration Flow

1. **Initialization** - `ConfigsModule` extracts streaming configs to persistent storage on first run
2. **Loading** - `ConfigSystem` checks providers in priority order (Streaming → Persistent)
3. **Caching** - Loaded configurations are cached by type ID for fast access
4. **Access** - `Config<T>.Get()` returns cached instance or loads from providers
5. **Saving** - `Save()` serializes configuration to persistent storage

## Built-in Framework Configurations

The Chris framework includes several built-in configurations:

**DataDrivenSettings** (`Chris.DataDriven`)
```csharp
var settings = DataDrivenSettings.Get();
// Controls DataTable manager initialization
```

**ModuleConfig** (`Chris.Modules`)
```csharp
var config = ModuleConfig.Get();
// Module loading configuration
```

**SchedulerSettings** (`Chris.Schedulers`)
```csharp
var settings = SchedulerSettings.Get();
// Scheduler debugging and stack trace options
```

## Advanced Features

### Custom Serialization

Use `PreferJsonConvert` attribute to choose serialization method:

```csharp
[PreferJsonConvert]  // Uses Newtonsoft.Json
public class AdvancedConfig : Config<AdvancedConfig>
{
    // Complex types, collections, polymorphic data
}
```

### Configuration Providers

Register custom configuration providers:

```csharp
ConfigSystem.RegisterConfigFileProvider(new CustomConfigProvider(), priority: 300);
```

### File Locations

- **Streaming**: `Application.streamingAssetsPath/Configs/`
- **Persistent**: `SaveUtility.SavePath/Configs/`
- **Extension**: `.cfg`

## Best Practices

1. **Use descriptive paths** - Organize configurations logically with `ConfigPathAttribute`
2. **Group related settings** - Configurations with the same root path share a file
3. **Prefer immutable access** - Get configuration once and cache locally if accessed frequently
4. **Save sparingly** - Only call `Save()` when user explicitly changes settings
5. **Use serializable types** - Ensure all fields are compatible with chosen serialization method

## Integration with Other Modules

The Configs system integrates seamlessly with other Chris framework modules:
- **Serialization** - Uses `SerializedType` and `SerializedObject` for complex data
- **Modules** - `ModuleLoader` uses `ModuleConfig` for initialization settings
- **DataDriven** - `DataDrivenSettings` controls DataTable behavior
- **Schedulers** - `SchedulerSettings` configures debugging features
