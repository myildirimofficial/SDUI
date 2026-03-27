# Orivy Data Binding System

Orivy has a first-class data binding system built on expression trees, `INotifyPropertyChanged`, and `INotifyCollectionChanged`. It is a retained-mode system — bindings are wired once and react to source changes automatically, with no polling.

---

## Table of Contents

1. [Architecture](#1-architecture)
2. [Core Concepts](#2-core-concepts)
3. [ViewModel Base Class](#3-viewmodel-base-class)
4. [Property Binding — `Link(...).From(...)`](#4-property-binding--linkfrom)
5. [DataContext Binding — `Link(...).FromData(...)`](#5-datacontext-binding--linkfromdata)
6. [Binding Modes — OneWay and TwoWay](#6-binding-modes--oneway-and-twoway)
7. [Collection Binding](#7-collection-binding)
8. [Event Interaction — `When(...)`](#8-event-interaction--when)
9. [Lifecycle and BindingHandle](#9-lifecycle-and-bindinghandle)
10. [Conversion Rules](#10-conversion-rules)
11. [GridList Collection Binding Deep Dive](#11-gridlist-collection-binding-deep-dive)
12. [Full Real-World Example](#12-full-real-world-example)
13. [Error Reference](#13-error-reference)
14. [File Reference](#14-file-reference)

---

## 1. Architecture

```
                          ┌─────────────────────┐
                          │   ViewModel          │
                          │  INotifyPropertyChanged│
                          │  ObservableCollection │
                          └─────────┬───────────┘
                                    │ PropertyChanged / CollectionChanged
                                    ▼
                        ┌───────────────────────┐
                        │   PropertyBinding<>    │
                        │  (internal wiring)     │
                        │  - subscribes events   │
                        │  - converts values     │
                        │  - calls setter        │
                        └─────────┬─────────────┘
                                  │
                                  ▼
                        ┌─────────────────────┐
                        │    UI Control        │
                        │  Label.Text          │
                        │  GridList.Items      │
                        │  Button.Enabled      │
                        └─────────────────────┘
```

Binding is built through two fluent builders:

- `BindingTargetBuilder<TTarget, TValue>` — created by calling `Link(view => view.Property)` on any `ElementBase`.
- `BindingSourceBuilder<TTarget, TSource, TTargetValue, TSourceValue>` — created by calling `.From(source, vm => vm.Property)` or `.FromData(vm => vm.Property)`.

All bindings return a `BindingHandle` (an `IDisposable`) for explicit teardown. Controls track their own handles automatically via `TrackBinding` — you only need to hold handles manually when you want early disposal.

---

## 2. Core Concepts

| Concept | Description |
|---|---|
| **DataContext** | An arbitrary object set on a control via `SetDataContext(vm)`. Propagates to child controls. |
| **Link** | Extension method that starts a binding expression on any `ElementBase`. |
| **From** | Binds to a concrete source object you already have in hand. |
| **FromData** | Binds to the control's DataContext (resolved at runtime, re-bound on DataContext change). |
| **OneWay** | Source → Target. Source changes are pushed to the target. Default mode. |
| **TwoWay** | Source ↔ Target. Changes on either side propagate to the other. |
| **BindingHandle** | Disposable token representing a live binding. Disposing it unsubscribes all events. |
| **ObservableObject** | Orivy-provided base class implementing `INotifyPropertyChanged` cleanly. |

---

## 3. ViewModel Base Class

`ObservableObject` is the recommended base for all view-models. It implements `INotifyPropertyChanged` and provides a `SetProperty` helper that suppresses redundant change events.

```csharp
using Orivy.Binding;

public class CounterViewModel : ObservableObject
{
    private int _count;
    private string _status = "Idle";

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public void Increment()
    {
        Count++;
        Status = Count switch
        {
            < 10 => "Low",
            < 50 => "Medium",
            _    => "High"
        };
    }
}
```

`SetProperty` only fires `PropertyChanged` when the new value is actually different — no redundant redraws.

You can also notify multiple properties at once or signal a dependent computed property:

```csharp
private string _firstName = "";
private string _lastName = "";

public string FirstName
{
    get => _firstName;
    set
    {
        SetProperty(ref _firstName, value);
        OnPropertyChanged(nameof(FullName));   // notify computed property too
    }
}

public string LastName
{
    get => _lastName;
    set
    {
        SetProperty(ref _lastName, value);
        OnPropertyChanged(nameof(FullName));
    }
}

public string FullName => $"{_firstName} {_lastName}";
```

---

## 4. Property Binding — `Link(...).From(...)`

Use `From` when you already hold a concrete reference to the source object and it will not change.

### Syntax

```csharp
target.Link(t => t.TargetProperty)
      .From(source, s => s.SourceProperty)
      .OneWay();   // or .TwoWay()
```

### Example — Label text driven by a model

```csharp
var vm = new CounterViewModel();
var label = new Label { Name = "counterLabel" };

label.Link(lbl => lbl.Text)
     .From(vm, m => m.Count.ToString())
     .OneWay();

// Clicking a button increments the count; the label updates automatically.
var btn = new Button { Text = "Increment" };
btn.When("Click", vm, (model, _) => model.Increment());
```

### Example — Button enabled state

```csharp
var vm = new LoginViewModel();
var loginBtn = new Button { Text = "Login" };

loginBtn.Link(btn => btn.Enabled)
        .From(vm, m => m.IsFormValid)
        .OneWay();
```

### Example — Two properties mapped from the same source

```csharp
var vm = new FileViewModel();
var nameLabel = new Label();
var sizeLabel = new Label();

nameLabel.Link(l => l.Text).From(vm, m => m.FileName).OneWay();
sizeLabel.Link(l => l.Text).From(vm, m => m.FileSizeDisplay).OneWay();
```

---

## 5. DataContext Binding — `Link(...).FromData(...)`

Use `FromData` when the view-model is set at a higher level via `SetDataContext`. The binding engine resolves the DataContext at runtime and automatically re-binds whenever it changes.

### Setting DataContext

```csharp
// Set on a container — all child controls inherit it.
panel.SetDataContext(new UserViewModel());
```

### Syntax

```csharp
target.Link(t => t.TargetProperty)
      .FromData((MyViewModel vm) => vm.SourceProperty)
      .OneWay();
```

The lambda type annotation `(MyViewModel vm)` tells the compiler the expected DataContext type. Alternatively use the explicit generic overload:

```csharp
target.Link(t => t.TargetProperty)
      .FromData<MyViewModel, string>(vm => vm.SourceProperty)
      .OneWay();
```

### Example — Form fields bound to DataContext

```csharp
// In InitializeComponent or page setup:
var vm = new ProfileViewModel { Name = "Alice", Age = 30 };
formPanel.SetDataContext(vm);

nameInput.Link(tb => tb.Text)
         .FromData((ProfileViewModel vm) => vm.Name)
         .TwoWay();

ageLabel.Link(lbl => lbl.Text)
        .FromData((ProfileViewModel vm) => vm.AgeDisplay)
        .OneWay();
```

### Example — DataContext changes re-bind automatically

```csharp
// Switch the whole page ViewModel — all FromData bindings rebind to the new object.
formPanel.SetDataContext(new ProfileViewModel { Name = "Bob", Age = 25 });
```

All `FromData` bindings on `formPanel` and its children re-evaluate instantly.

---

## 6. Binding Modes — OneWay and TwoWay

### OneWay (default)

Source → Target only. Use for read-only display.

```csharp
statusLabel.Link(l => l.Text)
           .From(vm, m => m.ConnectionStatus)
           .OneWay();
```

### TwoWay

Source ↔ Target. For editable inputs. The binding engine looks for a `{PropertyName}Changed` or `{PropertyName}ValueChanged` event on both sides to know when each end changes.

```csharp
// TextBox.Text changes → updates vm.SearchQuery
// vm.SearchQuery changes → updates TextBox.Text
searchBox.Link(tb => tb.Text)
         .From(vm, m => m.SearchQuery)
         .TwoWay();
```

For TwoWay to work on a custom control, expose a `{PropertyName}Changed` event:

```csharp
public class RatingControl : ElementBase
{
    private int _value;
    public event EventHandler? ValueChanged;

    public int Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            ValueChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }
}

// TwoWay binding works automatically:
rating.Link(r => r.Value).From(vm, m => m.UserRating).TwoWay();
```

---

## 7. Collection Binding

Collection binding wires a source `IEnumerable` (typically `ObservableCollection<T>`) to a target `IList` property. Full replacement occurs on every `CollectionChanged` event — there is no item diffing.

### OneWay collection binding with DataContext

```csharp
listBox.Link(lb => lb.Items)
       .FromData((ProductViewModel vm) => vm.Products)
       .OneWay();
```

When `vm.Products` fires `CollectionChanged`, the binding engine clears `listBox.Items` and refills it by converting each source item.

### Explicit item factory (full control over conversion)

```csharp
taskGrid.Link(g => g.Items)
        .FromData<TaskViewModel, ObservableCollection<TaskRow>, TaskRow>(
            vm => vm.Tasks,
            row =>
            {
                var item = new GridListItem { Tag = row };
                item.Cells.Add(new GridListCell { Text = row.Title });
                item.Cells.Add(new GridListCell { Text = row.AssignedTo });
                item.Cells.Add(new GridListCell { Text = row.Priority.ToString() });
                return item;
            });
```

The factory `Func<TSourceItem, object>` returns `object` — return any type compatible with the target collection element type.

### Auto-conversion without a factory

If no factory is provided and the target is a `GridListItemCollection`, the engine calls `ConvertToGridListItem`. See [GridList Collection Binding Deep Dive](#11-gridlist-collection-binding-deep-dive) for full rules.

---

## 8. Event Interaction — `When(...)`

`When` attaches an action to any named event on a control. It is the preferred way to wire UI actions back to the ViewModel.

### Syntax variants

```csharp
// No source — action only uses the target
target.When("EventName", target => { ... });

// Explicit source object
target.When("EventName", source, (src, target) => { ... });

// DataContext source (resolved at call time from target.DataContext)
target.When<TSource>("EventName", (src, target) => { ... });
```

### Example — Button click calls ViewModel method

```csharp
var saveBtn = new Button { Text = "Save" };
saveBtn.When<ProfileViewModel>("Click", (vm, btn) => vm.Save());
```

### Example — ComboBox selection change updates ViewModel

```csharp
filterCombo.When<DashboardViewModel>("SelectedIndexChanged",
    (vm, combo) => vm.ApplyFilter(combo.SelectedIndex));
```

### Example — Chaining multiple event wires in fluent style

```csharp
var addBtn = new Button { Text = "Add Task" };
addBtn
    .When<TaskViewModel>("Click", (vm, _) => vm.AddTask())
    .When("MouseEnter", btn => btn.Cursor = Cursors.Hand);
```

`When` returns the target, enabling chaining.

### Using `When` with explicit source (no DataContext required)

```csharp
var vm = new SettingsViewModel();
var resetBtn = new Button { Text = "Reset" };

resetBtn.When("Click", vm, (model, _) => model.ResetToDefaults());
```

---

## 9. Lifecycle and BindingHandle

Every binding call returns a `BindingHandle` which is an `IDisposable`. Disposing it tears down all event subscriptions cleanly.

### Automatic tracking

Controls that are `ElementBase` subclasses automatically track their own binding handles via `TrackBinding`. When a control is disposed, all its bindings are disposed too — you do not need to hold handles manually for the normal case.

```csharp
// Handle is automatically tracked by nameLabel.
nameLabel.Link(l => l.Text).From(vm, m => m.Name).OneWay();
```

### Explicit handle for early disposal

```csharp
BindingHandle handle = counterLabel.Link(l => l.Text)
                                   .From(vm, m => m.Count.ToString())
                                   .OneWay();

// Later, you can detach this specific binding:
handle.Dispose();
```

### Rebinding on DataContext change

`FromData` bindings rebind automatically when `DataContext` changes — the old binding is disposed and a new one is created targeting the new DataContext object. The control's UI immediately reflects the new source.

```csharp
// First context — shows Alice's data
userPane.SetDataContext(new UserViewModel { Name = "Alice" });

// Later — shows Bob's data with zero manual rewiring
userPane.SetDataContext(new UserViewModel { Name = "Bob" });
```

---

## 10. Conversion Rules

When the source type differs from the target property type, the engine applies the following conversion chain:

1. **Direct cast** — If the value is already the target type, use it as-is.
2. **Null handling** — Null maps to `null` for reference types, `default` for nullable value types.
3. **`Convert.ChangeType`** — For primitive conversions (`int` → `string`, `double` → `float`, etc.).
4. **Throws `InvalidOperationException`** — If none of the above applies.

For collection elements, additional rules apply — see [GridList Collection Binding Deep Dive](#11-gridlist-collection-binding-deep-dive).

### Converting an int source to a string label

```csharp
// Even though Count is int and Text is string, the engine converts via Convert.ChangeType.
label.Link(l => l.Text).From(vm, m => m.Count.ToString()).OneWay();
```

Prefer explicit `.ToString()` in the lambda — it is clearer, zero-cost, and avoids reflection.

---

## 11. GridList Collection Binding Deep Dive

`GridList.Items` is of type `GridListItemCollection` (inherits `Collection<GridListItem>`). When a source collection is bound to it, the engine uses a three-step conversion per item:

### Conversion priority order

```
1. itemConverter provided?  →  call it
           ↓ no
2. model is GridListItem?   →  pass-through
           ↓ no
3. model has ToGridListItem() method?  →  call it
           ↓ no
4. Fallback: reflect all public readable properties → one cell per property
```

### Step 1 — Explicit factory (most control)

```csharp
taskGrid.Link(g => g.Items)
        .FromData<TaskViewModel, ObservableCollection<TaskRow>, TaskRow>(
            vm => vm.Tasks,
            row =>
            {
                var item = new GridListItem { Tag = row };
                item.Cells.Add(new GridListCell { Text = row.Title,       ForeColor = SKColors.White });
                item.Cells.Add(new GridListCell { Text = row.AssignedTo                              });
                item.Cells.Add(new GridListCell { Text = row.Status,      ForeColor = StatusColor(row.Status) });
                return item;
            });
```

### Step 2 — pass-through (source is already GridListItem)

```csharp
var items = new ObservableCollection<GridListItem>
{
    new GridListItem { Tag = "row1" }
};
items[0].Cells.Add(new GridListCell { Text = "Direct item" });

grid.Link(g => g.Items).From(source, s => s.Items).OneWay();
```

### Step 3 — `ToGridListItem()` on the model (recommended pattern)

Add a `ToGridListItem()` method directly to your model record or class. The engine detects it via reflection and calls it automatically — no factory lambda needed at the call site.

```csharp
internal sealed record TaskRow(string Title, string AssignedTo, string Status)
{
    public GridListItem ToGridListItem()
    {
        var item = new GridListItem { Tag = this };
        item.Cells.Add(new GridListCell { Text = Title      });
        item.Cells.Add(new GridListCell { Text = AssignedTo });
        item.Cells.Add(new GridListCell { Text = Status     });
        return item;
    }
}
```

Call site becomes a one-liner:

```csharp
taskGrid.Link(g => g.Items)
        .FromData((TaskViewModel vm) => vm.Tasks)
        .OneWay();
```

Column order in the grid **must match** the cell order inside `ToGridListItem`. Define columns first:

```csharp
taskGrid.Columns.Add(new GridListColumn { Text = "Title",       Width = 200 });
taskGrid.Columns.Add(new GridListColumn { Text = "Assigned To", Width = 120 });
taskGrid.Columns.Add(new GridListColumn { Text = "Status",      Width = 100 });
```

### Step 4 — Reflection fallback (avoid in production)

If `ToGridListItem()` is absent, the engine reflects all public readable non-indexer properties in declaration order and adds one cell per property. Column order is then determined by CLR property order (declaration order for `record` types, but not guaranteed for arbitrary classes).

```csharp
// Works but column order may be unexpected.
record SimpleRow(string Name, int Count, bool Active);

// Produces cells: Name → Count → Active (in that order for records)
grid.Link(g => g.Items).FromData((VM vm) => vm.Rows).OneWay();
```

Avoid this for anything beyond quick prototypes.

---

## 12. Full Real-World Example

This section shows a complete page setup — ViewModel, controls, bindings, and interactions wired together.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using Orivy.Binding;

internal sealed class TaskBoardViewModel : ObservableObject
{
    private string _filter = "";
    private int _selectedIndex = -1;

    public ObservableCollection<TaskRow> Tasks { get; } = new();

    public string Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetProperty(ref _selectedIndex, value);
    }

    public string Summary => $"{Tasks.Count} tasks";

    public void AddTask(string title)
    {
        Tasks.Add(new TaskRow(title, "Unassigned", "Open"));
        OnPropertyChanged(nameof(Summary));
    }

    public void RemoveSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= Tasks.Count)
            return;

        Tasks.RemoveAt(_selectedIndex);
        SelectedIndex = -1;
        OnPropertyChanged(nameof(Summary));
    }
}

internal sealed record TaskRow(string Title, string AssignedTo, string Status)
{
    public GridListItem ToGridListItem()
    {
        var item = new GridListItem { Tag = this };
        item.Cells.Add(new GridListCell { Text = Title      });
        item.Cells.Add(new GridListCell { Text = AssignedTo });
        item.Cells.Add(new GridListCell { Text = Status     });
        return item;
    }
}
```

### UI setup (InitializeComponent equivalent)

```csharp
var vm = new TaskBoardViewModel();
vm.AddTask("Design login screen");
vm.AddTask("Implement auth token refresh");
vm.AddTask("Write unit tests for parser");

// Container — sets DataContext for all children
var page = new Panel { Dock = DockStyle.Fill, Padding = new(16) };
page.SetDataContext(vm);

// Summary label at top
var summaryLabel = new Label { Dock = DockStyle.Top, Height = 30 };
page.Controls.Add(summaryLabel);

// GridList for tasks
var taskGrid = new GridList { Dock = DockStyle.Fill };
taskGrid.Columns.Add(new GridListColumn { Text = "Title",       Width = 240 });
taskGrid.Columns.Add(new GridListColumn { Text = "Assigned To", Width = 120 });
taskGrid.Columns.Add(new GridListColumn { Text = "Status",      Width = 100 });
page.Controls.Add(taskGrid);

// Toolbar at bottom
var toolbar   = new Panel { Dock = DockStyle.Bottom, Height = 40 };
var addBtn    = new Button { Text = "Add",    Dock = DockStyle.Left,  Width = 80 };
var removeBtn = new Button { Text = "Remove", Dock = DockStyle.Left,  Width = 80 };
toolbar.Controls.Add(addBtn);
toolbar.Controls.Add(removeBtn);
page.Controls.Add(toolbar);

// ── Bindings ───────────────────────────────────────────────────────────

// Summary label ← vm.Summary
summaryLabel.Link(l => l.Text)
            .FromData((TaskBoardViewModel vm) => vm.Summary)
            .OneWay();

// GridList.Items ← vm.Tasks (auto-converts via TaskRow.ToGridListItem)
taskGrid.Link(g => g.Items)
        .FromData((TaskBoardViewModel vm) => vm.Tasks)
        .OneWay();

// GridList.SelectedIndex ↔ vm.SelectedIndex (two-way)
taskGrid.Link(g => g.SelectedIndex)
        .FromData((TaskBoardViewModel vm) => vm.SelectedIndex)
        .TwoWay();

// Remove button enabled ← vm.SelectedIndex >= 0
removeBtn.Link(b => b.Enabled)
         .FromData((TaskBoardViewModel vm) => vm.SelectedIndex >= 0)
         .OneWay();

// ── Interactions ───────────────────────────────────────────────────────

addBtn.When<TaskBoardViewModel>("Click",
    (vm, _) => vm.AddTask($"Task {vm.Tasks.Count + 1}"));

removeBtn.When<TaskBoardViewModel>("Click",
    (vm, _) => vm.RemoveSelected());
```

---

## 13. Error Reference

| Exception | Cause | Fix |
|---|---|---|
| `InvalidOperationException: Property '...' is read-only` | Binding target property has no setter and is not a collection | Use a collection property or make the property writable |
| `InvalidOperationException: Automatic collection binding requires IList` | Target collection does not implement `IList` | Use a list-based collection property |
| `InvalidOperationException: Null item mapped for target collection` | Item converter or `ToGridListItem()` returned null | Never return null from a converter |
| `InvalidOperationException: Collection binding cannot convert item` | No converter provided and `Convert.ChangeType` failed | Add a `ToGridListItem()` method or provide an explicit factory |
| `InvalidOperationException: Type '...' requires a DataContext of type '...'` | `When<TSource>` called but DataContext is not of that type | Set the correct DataContext before attaching the interaction |
| `InvalidOperationException: Type '...' does not expose event named '...'` | Wrong event name passed to `When` | Check the event name — it is case-sensitive |

---

## 14. File Reference

| File | Responsibility |
|---|---|
| `Orivy/Binding/BindingExtensions.cs` | Core binding wiring — `Link`, `CreatePropertyBinding`, `ReplaceTargetCollection`, `ConvertToGridListItem` |
| `Orivy/Binding/BindingTargetBuilder.cs` | Fluent API — `BindingTargetBuilder`, `BindingSourceBuilder`, `FromData` overloads |
| `Orivy/Binding/ObservableObject.cs` | Base ViewModel class — `SetProperty`, `OnPropertyChanged`, `OnPropertiesChanged` |
| `Orivy/Binding/InteractionExtensions.cs` | `When(...)` event wiring — attaches delegates to named events via reflection |
| `Orivy/Binding/BindingHandle.cs` | Disposable binding token |
| `Orivy/Binding/BindingMode.cs` | `OneWay` / `TwoWay` enum |

