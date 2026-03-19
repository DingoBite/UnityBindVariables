# UnityBindVariables

[Русская версия](README_ru.md)

Minimal reactive primitives for Unity and plain C# application models.

`UnityBindVariables` is a small library built around one core idea: keep state in plain model classes, expose it through readonly contracts, and notify interested systems when that state changes. It is intentionally lighter than full reactive frameworks and works well in projects where you want explicit data flow without introducing a heavy dependency stack.

The solution is especially useful for:

- application and gameplay state models;
- UI and view-model synchronization;
- dictionary-backed runtime stores;
- derived flags and computed read models;
- lightweight async state transitions.

## Why this solution

Compared to ad-hoc `event` fields spread across a codebase, `UnityBindVariables` provides one consistent contract for observable mutable state:

- one value-container model for regular values, async flows, dictionaries and lists;
- readonly public exposure through interfaces such as `IReadonlyBind<T>`;
- explicit derived state through `BindProcessed<TIn, TOut>`;
- convenient initial UI sync via `SafeSubscribeAndSet(...)`;
- no dependency on `MonoBehaviour`, `ScriptableObject`, UniRx, editor tooling, or a custom runtime framework;
- a very small code footprint that is easy to audit, vendor into a project, or keep as a git submodule.

For Unity projects this usually translates into less glue code, fewer duplicated flags, and cleaner boundaries between models, runtime systems, and views.

## Package contents

The repository currently contains:

- `Bind.cs`
  Synchronous `Bind<T>` and asynchronous `AsyncBind<T>` implementations.
- `BindInterfaces.cs`
  Readonly and mutable interfaces for sync and async binds.
- `BindProcessed.cs`
  Readonly derived bind that transforms a source bind value.
- `BindDict.cs`
  Convenience wrappers for dictionary and list binds.
- `BindExtensions.cs`
  Safe subscription helpers and bridges to `UnityEvent`.
- `BindAssembly.asmdef`
  Assembly definition with the name `BindAssembly`.

Namespace:

```csharp
using Bind;
```

## Dependencies

### Direct code dependencies

`UnityBindVariables` itself is intentionally low-dependency:

- C# / .NET base class library;
- Unity runtime types;
- `UnityEngine.Events` for the helper methods in `BindExtensions.cs`.

There is no hard repository-level dependency on sibling modules such as input systems, app frameworks, ECS layers, or project-specific gameplay code.

### Companion repositories in the HotelSnake submodule setup

The following repositories are listed in the host project's `.gitmodules`. They are not required to use `UnityBindVariables`, but they are part of the surrounding ecosystem in which this package is consumed.

| Repository | URL | Branch in `.gitmodules` |
| --- | --- | --- |
| `AppStructure` | `https://github.com/DingoBite/AppStructure` | `string-as-key-refactor` |
| `DingoProjectAppStructure` | `https://github.com/DingoBite/DingoProjectAppStructure.git` | default branch not pinned |
| `DingoLevelBasedInputSystem` | `https://github.com/DingoBite/DingoLevelBasedInputSystem.git` | default branch not pinned |
| `DingoECSUtils` | `https://github.com/DingoBite/DingoECSUtils` | default branch not pinned |
| `DingoAssetsLoadSystem` | `https://github.com/DingoBite/DingoAssetsLoadSystem` | default branch not pinned |
| `DingoUnityExtensions` | `https://github.com/DingoBite/DingoUnityExtensions` | `dev` |
| `DingoGameObjectsCMS` | `https://github.com/DingoBite/DingoGameObjectsCMS` | default branch not pinned |

## Installation

### Option 1. Git submodule

```bash
git submodule add https://github.com/DingoBite/UnityBindVariables.git Assets/AppSDK/UnityBindVariables
```

### Option 2. Copy into project

Copy the `.cs` files and `BindAssembly.asmdef` into any folder under `Assets/`, for example:

```text
Assets/Plugins/UnityBindVariables/
```

No package manifest or additional setup is required.

## Core concepts

### 1. `Bind<T>`

`Bind<T>` stores a value and emits `OnValueChange` whenever its state changes.

```csharp
private readonly Bind<int> _health = new(100, equalityCheck: true);
public IReadonlyBind<int> Health => _health;

public void Damage(int amount)
{
    _health.V -= amount;
}
```

Key points:

- `V` is the main property for reading and writing the value;
- `SetValue(T value)` and `GetValue()` are convenience aliases around `V`;
- listeners can be added via `AddListener`, `RemoveListener`, or direct event subscription;
- `RemoveAllListeners()` exists on `Bind<T>` for hard reset scenarios.

### 2. Readonly exposure

The recommended pattern is:

- keep `Bind<T>` private;
- expose `IReadonlyBind<T>` publicly.

```csharp
private readonly Bind<bool> _isOpen = new();
public IReadonlyBind<bool> IsOpen => _isOpen;
```

This keeps mutation rights local to the owning model while still allowing other systems to read and subscribe.

### 3. `AsyncBind<T>`

`AsyncBind<T>` is the async equivalent for workflows where a state change should trigger asynchronous listeners.

```csharp
private readonly AsyncBind<string> _status = new();

public Task SetStatusAsync(string value, CancellationTokenSource cts)
{
    return _status.SetValueAsync(value, cts);
}
```

Listener signature:

```csharp
Func<T, CancellationTokenSource, Task>
```

This is useful for loading flows, deferred reactions, or async side effects modeled as state changes.

### 4. `BindProcessed<TIn, TOut>`

`BindProcessed` creates a readonly bind derived from another bind.

```csharp
[Flags]
public enum PanelFlags
{
    None = 0,
    Visible = 1 << 0,
    Interactive = 1 << 1,
}

private readonly Bind<PanelFlags> _flags = new();
private readonly BindProcessed<PanelFlags, bool> _visible;

public IReadonlyBind<bool> Visible => _visible;

public PanelModel()
{
    _visible = new BindProcessed<PanelFlags, bool>(
        _flags,
        flags => (flags & PanelFlags.Visible) != 0,
        equalityCheck: true);
}
```

Important behavior:

- the initial value is computed immediately in the constructor;
- the processed bind subscribes to the source bind during construction;
- `BindProcessed` is readonly by design;
- `Dispose()` must be called when the processed bind should stop observing the source.

### 5. `BindDict<TKey, TValue>` and `BindList<TValue>`

These are convenience wrappers over mutable collections:

- `BindDict<TKey, TValue>` initializes an empty `Dictionary<TKey, TValue>`;
- `BindList<TValue>` initializes an empty `List<TValue>`.

They do not observe internal collection mutations automatically. The wrapper only emits change notifications when `V` is assigned.

Recommended pattern:

```csharp
private readonly BindDict<string, PlayerData> _players = new();

public void AddPlayer(string id, PlayerData player)
{
    _players.V[id] = player;
    _players.V = _players.V;
}
```

That explicit reassignment is intentional and useful when you want mutable collections without allocating a new dictionary or list on every update.

## API reference

### Interfaces

#### `IReadonlyBind<out T>`

Readonly synchronous contract:

- `event Action<T> OnValueChange`
- `T V { get; }`
- `T GetValue()`
- `AddListener(Action<T>)`
- `RemoveListener(Action<T>)`

#### `IBind<T>`

Mutable synchronous contract:

- inherits `IReadonlyBind<T>`;
- inherits `IValueContainer<T>`.

#### `IReadonlyAsyncBind<out T>`

Readonly asynchronous contract:

- `event Func<T, CancellationTokenSource, Task> OnValueChangeAsync`
- `T V { get; }`
- `T GetValue()`
- `AddListener(Func<T, CancellationTokenSource, Task>)`
- `RemoveListener(Func<T, CancellationTokenSource, Task>)`

#### `IAsyncBind<T>`

Mutable asynchronous contract:

- inherits `IReadonlyAsyncBind<T>`;
- inherits `IAsyncValueContainer<T>`.

#### `IValueContainer<T>`

- `T V { get; set; }`
- `void SetValue(T value)`

#### `IAsyncValueContainer<T>`

- `T V { get; }`
- `Task SetValueAsync(T value, CancellationTokenSource cancellationTokenSource = null)`

## Change notification semantics

This section matters because it defines the exact runtime behavior.

### Default mode: every assignment emits

By default `Bind<T>` and `AsyncBind<T>` are created with `equalityCheck: false`.

That means:

- assigning a value emits every time;
- reassigning the same reference still emits;
- this behavior is useful for force-refresh scenarios and mutable collection wrappers.

### Equality-aware mode

If `equalityCheck: true` is enabled:

- `Bind<T>` compares the previously stored value with the new one before emitting;
- you can provide a custom comparer through `Func<T, T, bool> equalityComparer`;
- otherwise regular `.Equals(...)` is used when both values are non-null.

This is useful for noisy or high-frequency values where duplicate emissions should be suppressed.

Example:

```csharp
private readonly Bind<bool> _active = new(equalityCheck: true);
```

### Equality order caveat in `AsyncBind<T>`

`Bind<T>` and `AsyncBind<T>` do not currently evaluate equality in the same order:

- `Bind<T>` checks equality against the previous stored value and then writes the new value;
- `AsyncBind<T>` writes `V` first and only then calls `Equal(value)`.

In practice, `Bind<T>` behaves as expected for duplicate suppression, while `AsyncBind<T>` should be treated carefully when `equalityCheck: true` is enabled.

## Extension methods

`BindExtensions.cs` adds convenience helpers for recurring subscription patterns.

### For synchronous binds

- `SafeSubscribe(...)`
  Removes the listener first, then adds it back. Useful when `OnEnable` or setup code can run multiple times.
- `SafeSubscribeAndSet(...)`
  Performs `SafeSubscribe(...)` and immediately invokes the callback with the current value.
- `UnSubscribe(...)`
  Removes the listener.
- `SafeBindInvoke(...)`
  Bridges a bind directly to `UnityEvent<T>` or `Action<T>`.

### For asynchronous binds

- `SafeSubscribe(...)`
- `SafeSubscribeAndSetAsync(...)`
- `UnSubscribe(...)`
- `SafeBindAsyncInvoke(...)`

### For `UnityEvent`

- `SafeSubscribe(this UnityEvent<T>, UnityAction<T>)`
- `SafeSubscribe(this UnityEvent, UnityAction)`
- matching `UnSubscribe(...)` overloads

These helpers are one of the strongest practical advantages of the package for Unity UI code because they eliminate duplicate subscriptions and make "subscribe plus push current state" a one-liner.

## Recommended usage patterns

### Keep writable binds private

Expose `IReadonlyBind<T>` from models and let only the owning model mutate state.

### Use processed binds instead of duplicated flags

If a value can be computed from another bind, prefer `BindProcessed` to manually synchronizing multiple fields.

### For UI, prefer `SafeSubscribeAndSet(...)`

This ensures:

- the view subscribes once;
- the current state is applied immediately;
- there is no separate initial refresh branch.

### For mutable collections, mutate then reassign

`Dictionary` and `List` binds are most effective when you want:

- low allocation cost;
- explicit refresh points;
- dictionary-driven stores or view collections.

### Unsubscribe explicitly

The library keeps subscriptions explicit on purpose. In Unity lifecycle code, unsubscribe in `OnDisable`, `OnDestroy`, disposal paths, or equivalent teardown points.

## Technical advantages in Unity projects

### Small surface area, low mental overhead

The library is small enough that the whole implementation can be read quickly. That matters when you want confidence in runtime behavior and do not want a large reactive framework just to observe a few model values.

### Decouples model logic from Unity objects

The core types are plain C# classes. State can live in app models, services, and runtime stores without being tied to `MonoBehaviour`, scene hierarchy, or inspector state.

### Strong readonly boundaries

`IReadonlyBind<T>` makes it easy to publish observable state without exposing write access to the rest of the codebase.

### Good fit for gameplay, runtime, and tool models

State such as flags, selections, loading markers, view state, and store contents map naturally to `Bind<T>` and `BindProcessed<TIn, TOut>`.

### Good fit for store and dictionary architectures

`BindDict<TKey, TValue>` works well when systems care that "the collection changed", but the data itself should remain mutable and allocation-light.

### Better view synchronization

The `SafeSubscribeAndSet(...)` family removes a lot of repetitive UI glue:

- subscribe;
- avoid duplicate handler registration;
- immediately reflect the current model state.

### Easy adoption path

Because the code is small and explicit, teams can:

- keep it as a submodule;
- vendor it directly into `Assets`;
- extend it locally if the project needs custom helpers.

## Limitations and caveats

This package is intentionally simple, so several behaviors are explicit trade-offs rather than hidden framework magic.

### Collection changes are not observed automatically

Mutating `Dictionary<TKey, TValue>` or `List<T>` contents does not emit unless you reassign `V`.

### `BindProcessed` requires manual teardown

`BindProcessed` subscribes to the source bind in the constructor and exposes a `Dispose()` method, but it does not implement `IDisposable`. Call `Dispose()` yourself when the derived bind should stop listening.

### No scheduler or thread marshalling

The package does not move callbacks onto the Unity main thread or any synchronization context. Listeners run on the thread from which the value was set.

### `AsyncBind<T>` follows multicast delegate semantics

If multiple async listeners are attached, the combined delegate follows standard C# multicast behavior. Do not assume that all returned tasks are awaited together; only the task returned by the last delegate is awaited by `SetValueAsync(...)`.

### `AsyncBind<T>` equality-aware mode has a current implementation caveat

Because `AsyncBind<T>.SetValueAsync(...)` assigns `V` before calling `Equal(value)`, enabling `equalityCheck: true` can suppress notifications unexpectedly. If you rely on async binds today, prefer `equalityCheck: false` unless you also revise the implementation.

### No built-in replay history

Only the current value is stored. There is no buffering, throttling, combining, or stream history like in larger reactive libraries.

## Example patterns

### Model state

```csharp
public class PanelModel
{
    private readonly Bind<bool> _opened = new();
    public IReadonlyBind<bool> Opened => _opened;

    public void Open() => _opened.V = true;
    public void Close() => _opened.V = false;
}
```

### Derived state

```csharp
private readonly Bind<int> _selectedCount = new();
private readonly BindProcessed<int, bool> _hasSelection;

public IReadonlyBind<bool> HasSelection => _hasSelection;

public SelectionModel()
{
    _hasSelection = new BindProcessed<int, bool>(
        _selectedCount,
        count => count > 0,
        equalityCheck: true);
}
```

### View synchronization

```csharp
private void OnEnable()
{
    _viewModel.Opened.SafeSubscribeAndSet(UpdateView);
}

private void OnDisable()
{
    _viewModel.Opened.UnSubscribe(UpdateView);
}

private void UpdateView(bool opened)
{
    gameObject.SetActive(opened);
}
```

### Mutable dictionary store

```csharp
private readonly BindDict<long, RuntimeItem> _items = new();
public IReadonlyBind<IReadOnlyDictionary<long, RuntimeItem>> Items => _items;

public void Upsert(RuntimeItem item)
{
    _items.V[item.Id] = item;
    _items.V = _items.V;
}
```

## When to choose `UnityBindVariables`

Choose this library when you want:

- a lightweight observable state primitive instead of a full reactive framework;
- explicit data flow that is easy to debug;
- readonly public and writable private model boundaries;
- easy UI synchronization without repetitive boilerplate;
- mutable dictionary or list stores with manual refresh control.

If you need declarative stream composition, buffering, scheduling, operator chains, or coordinated async orchestration across many listeners, you will probably want a larger reactive stack on top.

## Summary

`UnityBindVariables` is a small but practical foundation for state propagation in Unity projects. Its main strengths are explicitness, minimalism, readonly contracts, derived values, and very low integration cost. For teams that want clean model-to-view data flow without bringing in a heavyweight framework, it provides a strong middle ground between raw events and full reactive ecosystems.
