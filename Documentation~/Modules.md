# Modules

Runtime module loading system for modular architecture.

## Overview

The Modules system provides automatic initialization of framework components and game systems through a reflection-based module loading mechanism. It enables a plugin-style architecture where modules can be automatically discovered and initialized at runtime.

## Core Classes

### RuntimeModule

Abstract base class for all modules:

```csharp
[Preserve] // Prevent code stripping
public class MyGameModule : RuntimeModule
{
    public override void Initialize(ModuleConfig config)
    {
        // Module initialization logic
        Debug.Log("MyGameModule initialized");
        
        // Initialize systems, register services, etc.
        InitializeGameSystems();
    }
    
    private void InitializeGameSystems()
    {
        // Setup game-specific systems
    }
}
```

### ModuleLoader

Static class that handles automatic module discovery and initialization:

```csharp
public static class ModuleLoader
{
    // Control module loading
    public static bool Enable { get; set; } = true;
    
    // Automatically called before scene load
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeModules();
}
```

### ModuleConfig

Configuration system for modules:

```csharp
[ConfigPath("Chris.Modules")]
public class ModuleConfig : Config<ModuleConfig>
{
    // Module-specific configuration data
    // Automatically saved after module initialization
}
```

## Module Discovery Process

The ModuleLoader automatically discovers and initializes modules through reflection:

1. **Assembly Scanning** - Scans all assemblies that reference the Chris framework
2. **Type Discovery** - Finds all classes inheriting from `RuntimeModule`
3. **Instantiation** - Creates instances of discovered module types
4. **Initialization** - Calls `Initialize()` with shared `ModuleConfig`
5. **Configuration Save** - Persists any configuration changes

## Module Lifecycle

### Automatic Initialization

Modules are automatically initialized at `RuntimeInitializeLoadType.BeforeSceneLoad`:

```csharp
// This happens automatically - no manual setup required
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
public static void InitializeModules()
{
    // Framework handles this automatically
}
```

### Manual Control

You can disable automatic loading if needed:

```csharp
// Disable before BeforeSceneLoad phase
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
public static void DisableModuleLoading()
{
    ModuleLoader.Enable = false;
}

// Then manually initialize later
public void ManuallyInitializeModules()
{
    ModuleLoader.InitializeModules();
}
```

## Built-in Framework Modules

### ConfigsModule

Handles configuration system initialization:

```csharp
[Preserve]
public class ConfigsModule : RuntimeModule
{
    public override void Initialize(ModuleConfig config)
    {
        // Extracts streaming configs to persistent storage
        // Sets up config directories and serializers
    }
}
```

## Creating Custom Modules

```csharp
[Preserve]
public class AudioModule : RuntimeModule
{
    public override void Initialize(ModuleConfig config)
    {
        // Initialize audio systems
        SetupAudioMixer();
        RegisterAudioEvents();
        LoadAudioSettings();
    }
    
    private void SetupAudioMixer()
    {
        // Audio system setup
    }
    
    private void RegisterAudioEvents()
    {
        // Event system integration
    }
    
    private void LoadAudioSettings()
    {
        // Configuration loading
    }
}
```

## Assembly Filtering

The module loader intelligently filters assemblies:

- **Includes**: Assemblies that reference Chris framework
- **Excludes**: Editor assemblies (in builds)
- **Includes**: Chris framework assembly itself

```csharp
// This filtering happens automatically
var validAssemblies = AppDomain.CurrentDomain.GetAssemblies()
    .Where(assembly => 
    {
        // Skip editor assemblies in builds
        if (assembly.GetName().Name.Contains(".Editor"))
            return false;
            
        // Include assemblies referencing Chris
        return assembly.GetReferencedAssemblies()
            .Any(name => name.Name == nameof(Chris)) 
            || assembly.GetName().Name == nameof(Chris);
    });
```