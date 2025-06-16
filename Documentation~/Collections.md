# Collections

Utility collection classes including IOC container, priority queue, and sparse arrays.

## Overview

The Collections module provides specialized data structures and utility classes that extend Unity's built-in collections with performance-optimized and feature-rich alternatives.

## Core Classes

### IOCContainer

Simple Inversion of Control container for dependency injection:

```csharp
var container = new IOCContainer();

// Register instances
container.Register<IService>(new MyService());
container.Register<IRepository>(new DatabaseRepository());

// Resolve dependencies
var service = container.Resolve<IService>();
var repository = container.Resolve<IRepository>();

// Cleanup
container.Clear();
```

### PriorityQueue<T>

Heap-based priority queue implementation:

```csharp
var queue = new PriorityQueue<Task>();

// Add items with priority
queue.Enqueue(task1, priority: 10);
queue.Enqueue(task2, priority: 5);
queue.Enqueue(task3, priority: 15);

// Process in priority order
while (queue.Count > 0)
{
    var highestPriorityTask = queue.Dequeue();
    // Process task...
}
```

### SparseArray<T>

Memory-efficient sparse array for large indices with few elements:

```csharp
var sparseArray = new SparseArray<GameObject>();

// Set values at arbitrary indices
sparseArray[1000] = gameObject1;
sparseArray[50000] = gameObject2;
sparseArray[1000000] = gameObject3;

// Efficient iteration over only set values
foreach (var kvp in sparseArray)
{
    int index = kvp.Key;
    GameObject obj = kvp.Value;
    // Process only non-null entries
}
```

### RandomList<T>

List with O(1) random removal:

```csharp
var randomList = new RandomList<Enemy>();

// Add enemies
randomList.Add(enemy1);
randomList.Add(enemy2);
randomList.Add(enemy3);

// Remove random enemy efficiently
var randomEnemy = randomList.RemoveRandom();
```

## Utility Extensions

### NativeCollectionsExtensions

Extensions for Unity's native collections:

```csharp
// Example usage with NativeArray
var nativeArray = new NativeArray<int>(100, Allocator.Temp);
// Extensions provide additional functionality
```

### ShufflingExtension

Efficient shuffling algorithms:

```csharp
var list = new List<Card> { card1, card2, card3, card4 };

// Fisher-Yates shuffle
list.Shuffle();

// Shuffle with custom random
list.Shuffle(customRandom);
```

### ArrayUtils

Static utility methods for array operations:

```csharp
// Fast array operations
ArrayUtils.FastClear(array);
ArrayUtils.FastCopy(source, destination);
ArrayUtils.FastResize(ref array, newSize);
```

## Performance Characteristics

| Collection | Access | Insert | Remove | Memory |
|------------|--------|--------|--------|---------|
| IOCContainer | O(1) | O(1) | O(1) | Low |
| PriorityQueue | O(log n) | O(log n) | O(log n) | Optimal |
| SparseArray | O(1) | O(1) | O(1) | Sparse |
| RandomList | O(1) | O(1) | O(1) | Compact |

## Use Cases

**IOCContainer** - Lightweight dependency injection for modular systems
**PriorityQueue** - Task scheduling, pathfinding, event processing
**SparseArray** - Entity systems with large ID ranges, sparse world data
**RandomList** - Random selection with frequent additions/removals
**Extensions** - Performance-critical array and collection operations

## Integration

Collections module integrates with other Chris framework systems:
- **Pool** - Used internally for efficient object reuse
- **Tasks** - PriorityQueue used for task scheduling
- **Events** - RandomList for efficient event handler management
- **Schedulers** - Priority-based timer management
