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

## Console Variables

Console Variables allow developers to modify configuration values at runtime through the in-game debug console. This feature is particularly useful for debugging, testing, and fine-tuning gameplay parameters without restarting the application.

### Overview

The Console Variables system automatically discovers fields and properties marked with the `[ConsoleVariable]` attribute in Config classes and registers them as console commands. This enables real-time modification of configuration values through the integrated debug console.

### How It Works

1. **Auto-Discovery**: At startup, the system scans all Config classes for `[ConsoleVariable]` attributes
2. **Command Generation**: Automatically creates console commands for getting and setting values
3. **Type Safety**: Only supports `int`, `float`, `bool`, and `string` types
4. **Persistence**: Changes are automatically saved to the persistent configuration file

### Usage

#### Adding Console Variables

```csharp
[Serializable]
[ConfigPath("MyGame.Graphics")]
public class GraphicsConfig : Config<GraphicsConfig>
{
    [SerializeField]
    [ConsoleVariable("r.shadows")]
    public bool enableShadows = true;
    
    [SerializeField]
    [ConsoleVariable("r.quality")]
    public int qualityLevel = 2;
    
    [SerializeField]
    [ConsoleVariable("r.fov")]
    public float fieldOfView = 60f;
    
    [SerializeField]
    [ConsoleVariable("r.playerName")]
    public string playerName = "Player";
    
    // Regular config field (not exposed to console)
    [SerializeField]
    public bool internalSetting = false;
}
```

#### Console Commands

The system automatically generates console commands for each variable:

**Get Current Value:**
```
r.shadows
r.quality
r.fov
r.playerName
```

**Set New Value:**
```
r.shadows= true
r.quality= 3
r.fov= 90.0
r.playerName= "NewPlayer"
```

**Boolean Variables** (supports both formats):
```
r.shadows= true
r.shadows= 1      // 1 = true, 0 = false
```

### Supported Types

| Type | Example Command | Notes |
|------|-----------------|-------|
| `int` | `r.quality= 3` | Integer values |
| `float` | `r.fov= 90.5` | Floating-point values |
| `bool` | `r.shadows= true` | Boolean values (true/false or 1/0) |
| `string` | `r.name= "Player"` | String values |

### Integration with Debug Console

![Console Variables](./Images/console_variables.png)

The system integrates seamlessly with [yasirkula/UnityIngameDebugConsole](https://github.com/yasirkula/UnityIngameDebugConsole) which is modified and included in this repo.

Console variables are automatically registered as console commands.