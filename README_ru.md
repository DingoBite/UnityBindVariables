# UnityBindVariables

[English version](README.md)

Минималистичные реактивные примитивы для Unity и обычных C# application model.

`UnityBindVariables` это небольшая библиотека, построенная вокруг одной идеи: хранить состояние в простых model-классах, отдавать его наружу через readonly-контракты и уведомлять заинтересованные системы при изменении значения. Она намеренно легче полноценных reactive-фреймворков и хорошо подходит для проектов, где нужен явный поток данных без тяжелого стека зависимостей.

Решение особенно полезно для:

- application и gameplay model;
- синхронизации UI и view-model;
- runtime store на базе словарей;
- производных флагов и вычисляемых read model;
- легковесных async state transition.

## Почему это решение полезно

По сравнению с разрозненными `event`-полями по всему проекту `UnityBindVariables` дает единый контракт для наблюдаемого изменяемого состояния:

- одна модель контейнера значения для обычных значений, async-flow, словарей и списков;
- readonly-публичная выдача через интерфейсы вроде `IReadonlyBind<T>`;
- явные производные значения через `BindProcessed<TIn, TOut>`;
- удобная первичная синхронизация UI через `SafeSubscribeAndSet(...)`;
- отсутствие зависимости от `MonoBehaviour`, `ScriptableObject`, UniRx, editor tooling или кастомного runtime-framework;
- очень маленький кодовый объем, который легко аудировать, вендорить в проект или держать как git submodule.

Для Unity-проектов это обычно означает меньше glue-кода, меньше дублирующих флагов и более чистые границы между model, runtime-системами и view-слоем.

## Состав пакета

Сейчас репозиторий содержит:

- `Bind.cs`
  Синхронную реализацию `Bind<T>` и асинхронную `AsyncBind<T>`.
- `BindInterfaces.cs`
  Readonly и mutable интерфейсы для sync- и async-bind.
- `BindProcessed.cs`
  Readonly bind, вычисляющий значение из другого bind.
- `BindDict.cs`
  Упрощенные обертки для bind-словаря и bind-списка.
- `BindExtensions.cs`
  Хелперы безопасной подписки и мосты к `UnityEvent`.
- `BindAssembly.asmdef`
  Assembly definition с именем `BindAssembly`.

Namespace:

```csharp
using Bind;
```

## Зависимости

### Прямые кодовые зависимости

`UnityBindVariables` специально держится максимально легким по зависимостям:

- базовая библиотека C# / .NET;
- Unity runtime types;
- `UnityEngine.Events` для helper-методов из `BindExtensions.cs`.

Жесткой репозиторной зависимости на соседние модули вроде input-system, app-framework, ECS-слоя или project-specific gameplay-кода у пакета нет.

### Связанные репозитории в конфигурации сабмодулей HotelSnake

Ниже перечислены репозитории из `.gitmodules` хост-проекта. Они не обязательны для использования `UnityBindVariables`, но входят в экосистему, внутри которой этот пакет применяется.

| Репозиторий | URL | Ветка в `.gitmodules` |
| --- | --- | --- |
| `AppStructure` | `https://github.com/DingoBite/AppStructure` | `string-as-key-refactor` |
| `DingoProjectAppStructure` | `https://github.com/DingoBite/DingoProjectAppStructure.git` | default branch не зафиксирована |
| `DingoLevelBasedInputSystem` | `https://github.com/DingoBite/DingoLevelBasedInputSystem.git` | default branch не зафиксирована |
| `DingoECSUtils` | `https://github.com/DingoBite/DingoECSUtils` | default branch не зафиксирована |
| `DingoAssetsLoadSystem` | `https://github.com/DingoBite/DingoAssetsLoadSystem` | default branch не зафиксирована |
| `DingoUnityExtensions` | `https://github.com/DingoBite/DingoUnityExtensions` | `dev` |
| `DingoGameObjectsCMS` | `https://github.com/DingoBite/DingoGameObjectsCMS` | default branch не зафиксирована |

## Установка

### Вариант 1. Git submodule

```bash
git submodule add https://github.com/DingoBite/UnityBindVariables.git Assets/AppSDK/UnityBindVariables
```

### Вариант 2. Копирование в проект

Скопируйте `.cs`-файлы и `BindAssembly.asmdef` в любую папку внутри `Assets/`, например:

```text
Assets/Plugins/UnityBindVariables/
```

Дополнительный package manifest или специальная настройка не нужны.

## Базовые концепции

### 1. `Bind<T>`

`Bind<T>` хранит значение и эмитит `OnValueChange` всякий раз, когда состояние меняется.

```csharp
private readonly Bind<int> _health = new(100, equalityCheck: true);
public IReadonlyBind<int> Health => _health;

public void Damage(int amount)
{
    _health.V -= amount;
}
```

Основные моменты:

- `V` это основное свойство для чтения и записи значения;
- `SetValue(T value)` и `GetValue()` это удобные алиасы вокруг `V`;
- слушатели могут подписываться через `AddListener`, `RemoveListener` или напрямую через событие;
- у `Bind<T>` есть `RemoveAllListeners()` для сценариев жёсткого сброса.

### 2. Readonly-экспозиция

Рекомендуемый паттерн такой:

- хранить `Bind<T>` приватно;
- наружу отдавать `IReadonlyBind<T>`.

```csharp
private readonly Bind<bool> _isOpen = new();
public IReadonlyBind<bool> IsOpen => _isOpen;
```

Так право на запись остается у владельца model, а остальной код может только читать и подписываться.

### 3. `AsyncBind<T>`

`AsyncBind<T>` это асинхронный эквивалент для сценариев, где изменение состояния должно триггерить асинхронных слушателей.

```csharp
private readonly AsyncBind<string> _status = new();

public Task SetStatusAsync(string value, CancellationTokenSource cts)
{
    return _status.SetValueAsync(value, cts);
}
```

Сигнатура слушателя:

```csharp
Func<T, CancellationTokenSource, Task>
```

Это удобно для loading-flow, отложенных реакций и async side effect, которые все еще моделируются как изменение состояния.

### 4. `BindProcessed<TIn, TOut>`

`BindProcessed` создает readonly bind, вычисляемый из другого bind.

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

Важно:

- начальное значение вычисляется сразу в конструкторе;
- processed-bind подписывается на source-bind во время создания;
- `BindProcessed` по дизайну readonly;
- когда derived-bind больше не нужен, нужно вручную вызвать `Dispose()`.

### 5. `BindDict<TKey, TValue>` и `BindList<TValue>`

Это удобные обертки поверх изменяемых коллекций:

- `BindDict<TKey, TValue>` создает пустой `Dictionary<TKey, TValue>`;
- `BindList<TValue>` создает пустой `List<TValue>`.

Они не отслеживают внутренние мутации коллекции автоматически. Обертка отправляет событие только при assignment в `V`.

Рекомендуемый паттерн:

```csharp
private readonly BindDict<string, PlayerData> _players = new();

public void AddPlayer(string id, PlayerData player)
{
    _players.V[id] = player;
    _players.V = _players.V;
}
```

Такой явный reassignment сделан специально и полезен, когда нужна mutable-коллекция без аллокации нового словаря или списка на каждом обновлении.

## Справочник по API

### Интерфейсы

#### `IReadonlyBind<out T>`

Readonly sync-контракт:

- `event Action<T> OnValueChange`
- `T V { get; }`
- `T GetValue()`
- `AddListener(Action<T>)`
- `RemoveListener(Action<T>)`

#### `IBind<T>`

Mutable sync-контракт:

- наследует `IReadonlyBind<T>`;
- наследует `IValueContainer<T>`.

#### `IReadonlyAsyncBind<out T>`

Readonly async-контракт:

- `event Func<T, CancellationTokenSource, Task> OnValueChangeAsync`
- `T V { get; }`
- `T GetValue()`
- `AddListener(Func<T, CancellationTokenSource, Task>)`
- `RemoveListener(Func<T, CancellationTokenSource, Task>)`

#### `IAsyncBind<T>`

Mutable async-контракт:

- наследует `IReadonlyAsyncBind<T>`;
- наследует `IAsyncValueContainer<T>`.

#### `IValueContainer<T>`

- `T V { get; set; }`
- `void SetValue(T value)`

#### `IAsyncValueContainer<T>`

- `T V { get; }`
- `Task SetValueAsync(T value, CancellationTokenSource cancellationTokenSource = null)`

## Семантика уведомлений об изменении

Этот раздел важен, потому что он описывает точное runtime-поведение.

### Режим по умолчанию: каждое присваивание эмитит событие

По умолчанию `Bind<T>` и `AsyncBind<T>` создаются с `equalityCheck: false`.

Это означает:

- любое присваивание эмитит событие;
- повторное присваивание той же ссылки тоже эмитит;
- это поведение удобно для force-refresh-сценариев и mutable-коллекций.

### Режим с проверкой равенства

Если включен `equalityCheck: true`:

- `Bind<T>` сравнивает ранее сохраненное значение с новым до эмита события;
- можно передать кастомный comparer через `Func<T, T, bool> equalityComparer`;
- иначе используется обычный `.Equals(...)`, когда оба значения не `null`.

Это полезно для шумных или высокочастотных значений, где дубликаты нужно подавлять.

Пример:

```csharp
private readonly Bind<bool> _active = new(equalityCheck: true);
```

### Нюанс порядка equality-check в `AsyncBind<T>`

`Bind<T>` и `AsyncBind<T>` сейчас проверяют равенство не в одинаковом порядке:

- `Bind<T>` сначала сравнивает с предыдущим сохраненным значением, потом записывает новое;
- `AsyncBind<T>` сначала записывает `V`, и только потом вызывает `Equal(value)`.

На практике это значит, что `Bind<T>` ожидаемо подавляет дубликаты, а `AsyncBind<T>` нужно использовать осторожно, если включен `equalityCheck: true`.

## Extension methods

`BindExtensions.cs` добавляет удобные helper-методы для повторяющихся паттернов подписки.

### Для sync-bind

- `SafeSubscribe(...)`
  Сначала удаляет слушателя, потом добавляет заново. Полезно, если `OnEnable` или setup-код могут вызываться несколько раз.
- `SafeSubscribeAndSet(...)`
  Выполняет `SafeSubscribe(...)` и сразу вызывает callback с текущим значением.
- `UnSubscribe(...)`
  Удаляет слушателя.
- `SafeBindInvoke(...)`
  Делает мост от bind напрямую к `UnityEvent<T>` или `Action<T>`.

### Для async-bind

- `SafeSubscribe(...)`
- `SafeSubscribeAndSetAsync(...)`
- `UnSubscribe(...)`
- `SafeBindAsyncInvoke(...)`

### Для `UnityEvent`

- `SafeSubscribe(this UnityEvent<T>, UnityAction<T>)`
- `SafeSubscribe(this UnityEvent, UnityAction)`
- соответствующие перегрузки `UnSubscribe(...)`

Это одно из самых практичных преимуществ пакета для Unity UI-кода: helper-методы убирают дублирующиеся подписки и превращают связку "подписаться и сразу протолкнуть текущее состояние" в одну строку.

## Рекомендуемые паттерны использования

### Хранить writable-bind приватно

Публикуйте из model `IReadonlyBind<T>`, а менять состояние пусть может только владелец model.

### Использовать processed-bind вместо дублирующих флагов

Если значение можно вычислить из другого bind, лучше использовать `BindProcessed`, чем вручную синхронизировать несколько полей.

### Для UI предпочитать `SafeSubscribeAndSet(...)`

Это гарантирует:

- view подпишется один раз;
- текущее состояние применится сразу;
- не понадобится отдельная ветка логики для первичного refresh.

### Для mutable-коллекций: mutate, затем reassign

`Dictionary`- и `List`-bind особенно полезны, если нужны:

- низкие аллокации;
- явные точки refresh;
- store- или view-архитектура на базе словаря.

### Явно отписываться

Библиотека специально делает жизненный цикл подписок явным. В Unity-коде стоит отписываться в `OnDisable`, `OnDestroy`, dispose-path или аналогичных teardown-точках.

## Технические преимущества для Unity-проектов

### Маленькая поверхность API и низкая когнитивная нагрузка

Библиотека настолько компактна, что её реализацию можно быстро прочитать целиком. Это важно, когда нужна уверенность в runtime-поведении без подключения большого reactive-фреймворка ради нескольких model-значений.

### Развязывает model-логику от Unity-объектов

Базовые типы это обычные C#-классы. Состояние может жить в app model, сервисах и runtime store без жесткой привязки к `MonoBehaviour`, сценовой иерархии или inspector-state.

### Сильные readonly-границы

`IReadonlyBind<T>` позволяет публиковать наблюдаемое состояние, не открывая остальному коду право на запись.

### Хорошо подходит для gameplay-, runtime- и tool-model

Флаги, selection-state, loading-маркеры, view-state и содержимое store естественно ложатся на `Bind<T>` и `BindProcessed<TIn, TOut>`.

### Подходит для store-архитектур на словарях

`BindDict<TKey, TValue>` хорошо работает там, где системам важно знать, что "коллекция изменилась", но сами данные при этом должны оставаться mutable и легкими по аллокациям.

### Улучшает синхронизацию view

Семейство `SafeSubscribeAndSet(...)` убирает значительный объем повторяющегося UI glue-кода:

- подписаться;
- избежать дублирующей регистрации handler;
- сразу применить текущее состояние model.

### Простой путь внедрения

Из-за компактности и явности кода команда может:

- держать пакет как submodule;
- вендорить его прямо в `Assets`;
- локально расширять при необходимости.

## Ограничения и caveats

Библиотека намеренно простая, поэтому часть поведения является явным trade-off, а не скрытой магией фреймворка.

### Изменения коллекций не отслеживаются автоматически

Мутация `Dictionary<TKey, TValue>` или `List<T>` не эмитит событие, пока вы явно не переприсвоите `V`.

### `BindProcessed` требует ручного teardown

`BindProcessed` подписывается на source-bind в конструкторе и имеет метод `Dispose()`, но не реализует `IDisposable`. Если derived-bind должен перестать слушать источник, `Dispose()` нужно вызвать вручную.

### Нет scheduler и thread-marshalling

Пакет не переносит callback на Unity main thread и не работает через отдельный synchronization context. Слушатели выполняются на том потоке, с которого значение было установлено.

### `AsyncBind<T>` следует семантике multicast-delegate

Если к async-bind привязано несколько async-listener, совмещенный delegate ведет себя по стандартным правилам C# multicast. Не стоит считать, что все возвращенные `Task` будут ожидаться совместно: `SetValueAsync(...)` ожидает только `Task`, возвращенный последним delegate в списке вызова.

### У `AsyncBind<T>` есть текущий caveat в equality-aware режиме

Так как `AsyncBind<T>.SetValueAsync(...)` присваивает `V` до вызова `Equal(value)`, включение `equalityCheck: true` может неожиданно подавлять уведомления. Если вы используете async-bind в текущем виде, безопаснее оставлять `equalityCheck: false`, если вы отдельно не правите реализацию.

### Нет встроенной истории значений

Хранится только текущее значение. Буферизации, throttling, combine-операторов и stream-history, как в больших reactive-библиотеках, здесь нет.

## Примеры паттернов

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

### Производное состояние

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

### Синхронизация view

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

## Когда стоит выбирать `UnityBindVariables`

Выбирайте эту библиотеку, если вам нужен:

- легковесный примитив наблюдаемого состояния вместо полного reactive-framework;
- явный data-flow, который легко дебажить;
- разделение readonly public и writable private границ model;
- удобная синхронизация UI без повторяющегося boilerplate;
- mutable dictionary- или list-store с ручным контролем refresh.

Если вам нужны декларативная композиция потоков, буферизация, scheduling, operator-chain или координация сложной async-оркестрации между множеством слушателей, поверх этого решения лучше использовать более крупный reactive-стек.

## Итог

`UnityBindVariables` это маленькая, но практичная основа для распространения состояния в Unity-проектах. Главные сильные стороны решения это явность поведения, минимализм, readonly-контракты, поддержка derived-value и очень низкая стоимость интеграции. Для команд, которым нужен чистый model-to-view data flow без подключения тяжеловесного фреймворка, библиотека дает сильный компромисс между сырыми `event` и полноценной reactive-экосистемой.
