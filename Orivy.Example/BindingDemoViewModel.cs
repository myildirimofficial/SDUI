using Orivy.Binding;
using Orivy.Controls;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;

namespace Orivy.Example;

internal sealed class BindingDemoViewModel : ObservableObject
{
    private SKColor _pickerAccentColor;
    private BindingPreset? _selectedPreset;
    private string? _selectedTeam;
    private BindingTaskRow? _selectedTask;
    private int _selectedTaskIndex;
    private bool _alertVisible;
    private string _deploymentNote = "Live object graph is synchronized. Change the preset or team and every bound surface updates together.";

    public BindingDemoViewModel()
    {
        Presets = new ObservableCollection<BindingPreset>
        {
            new("Zero-Latency Control", "Tight control plane for dense operator workflows.", new SKColor(14, 116, 144), true),
            new("Studio Review Surface", "Calmer collaboration preset with slower decision pacing.", new SKColor(30, 64, 175), true),
            new("Incident Bridge", "High-attention preset that blocks deployment until the signal clears.", new SKColor(185, 28, 28), false)
        };

        Teams = new ObservableCollection<string>
        {
            "Core UI",
            "Motion Lab",
            "Release Ops"
        };

        Tasks = new ObservableCollection<BindingTaskRow>
        {
            new("Render Cache", "Ready", "GPU", "Frame cache is warm and swapchain reuse is within target."),
            new("Inspector Sync", "Active", "Design", "Selection overlays and property surfaces are synchronized."),
            new("Deploy Gate", "Blocked", "Release", "Deployment waits for the alert signal to clear before arming."),
        };

        _selectedPreset = Presets[0];
        _selectedTeam = Teams[0];
        _selectedTask = Tasks[0];
        _selectedTaskIndex = 0;
        _alertVisible = false;
        _pickerAccentColor = _selectedPreset.AccentColor;

        RaiseDerivedState();
    }

    public ObservableCollection<BindingPreset> Presets { get; }
    public ObservableCollection<string> Teams { get; }
    public ObservableCollection<BindingTaskRow> Tasks { get; }

    public BindingPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!SetProperty(ref _selectedPreset, value))
                return;

            if (_selectedPreset?.AllowsDeployment == false)
                _alertVisible = true;

            RaiseDerivedState();
        }
    }

    public string? SelectedTeam
    {
        get => _selectedTeam;
        set
        {
            if (!SetProperty(ref _selectedTeam, value))
                return;

            _deploymentNote = $"Selection moved to {value}. Bound cards, buttons and the deployment CTA now reflect the same view-model state.";
            RaiseDerivedState();
        }
    }

    public bool AlertVisible
    {
        get => _alertVisible;
        set
        {
            if (!SetProperty(ref _alertVisible, value))
                return;

            RaiseDerivedState();
        }
    }

    public int SelectedTaskIndex
    {
        get => _selectedTaskIndex;
        set
        {
            var normalized = Tasks.Count == 0 ? -1 : Math.Clamp(value, -1, Tasks.Count - 1);
            if (!SetProperty(ref _selectedTaskIndex, normalized))
                return;

            _selectedTask = GetSelectedTask();
            OnPropertyChanged(nameof(SelectedTask));

            RaiseDerivedState();
        }
    }

    public BindingTaskRow? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (!SetProperty(ref _selectedTask, value))
                return;

            var index = value == null ? -1 : Tasks.IndexOf(value);
            if (_selectedTaskIndex != index)
            {
                _selectedTaskIndex = index;
                OnPropertyChanged(nameof(SelectedTaskIndex));
            }

            RaiseDerivedState();
        }
    }

    public SKColor PickerAccentColor
    {
        get => _pickerAccentColor;
        set
        {
            if (!SetProperty(ref _pickerAccentColor, value))
                return;

            RaiseDerivedState();
        }
    }

    public string HeroText => $"{SelectedPreset?.Name ?? "No preset selected"}\n{SelectedPreset?.Description ?? "Pick a preset to drive the page."}";

    public string SummaryText => $"DataContext is inherited by the entire page. The two combo boxes and the GridList all bind through the same Link(...) surface, including live collection targets. Buttons stay on the native event system.\n\nCurrent accent owner: {SelectedTeam ?? "None"}";

    public string AlertText => AlertVisible
        ? "Attention\nIncident Bridge is active or a manual signal was raised. Deployment stays disabled until the state clears."
        : string.Empty;

    public string StatusText => $"Selected Team\n{SelectedTeam ?? "Unassigned"}\n\nCoordination\n{_deploymentNote}";

    public string TaskDetailText
    {
        get
        {
            var task = GetSelectedTask();
            if (task == null)
                return "Task Detail\nSelect a row to inspect the bound master-detail projection.";

            return $"Task Detail\n{task.Name}\n\nLane\n{task.Lane}\n\nState\n{task.State}\n\nSummary\n{task.Summary}";
        }
    }

    public string TaskFooterText => $"Task Feed\n{Tasks.Count} live rows are bound into GridList through the generic Items binding path. Add or advance a task to see collection and selection state collaborate.";

    public string DeployActionText => CanDeploy
        ? $"Deploy {SelectedPreset?.Name}"
        : "Resolve Signal Before Deploy";

    public bool CanDeploy => !AlertVisible && SelectedPreset?.AllowsDeployment == true && !string.IsNullOrWhiteSpace(SelectedTeam);

    public string DeploymentPayload => $"{SelectedTeam ?? "Unassigned"}::{SelectedPreset?.Name ?? "Preset"}";

    public SKColor AccentColor => PickerAccentColor;

    public void CyclePreset()
    {
        if (Presets.Count == 0)
            return;

        var currentIndex = _selectedPreset == null ? -1 : Presets.IndexOf(_selectedPreset);
        var nextIndex = (currentIndex + 1) % Presets.Count;
        SelectedPreset = Presets[nextIndex];
        PickerAccentColor = SelectedPreset?.AccentColor ?? PickerAccentColor;
        _deploymentNote = $"Preset switched to {SelectedPreset?.Name}. Any bound control listening to the same object graph updated without manual wiring.";
        RaiseDerivedState();
    }

    public void ToggleAlert()
    {
        AlertVisible = !AlertVisible;
        _deploymentNote = AlertVisible
            ? "Signal raised manually. The deploy button and warning surface now react from the same boolean source."
            : "Signal cleared. The action surface is available again when the selected preset allows deployment.";
        RaiseDerivedState();
    }

    public void AddTeam()
    {
        var nextTeam = $"Ops Pod {Teams.Count + 1:00}";
        Teams.Add(nextTeam);
        SelectedTeam = nextTeam;
        _deploymentNote = $"{nextTeam} was appended to the bound collection. The combo box repainted from collection change notifications only.";
        RaiseDerivedState();
    }

    public void AddTask()
    {
        var task = new BindingTaskRow(
            $"Bound Task {Tasks.Count + 1:00}",
            "Queued",
            SelectedTeam ?? "Ops",
            $"Spawned from the event pipeline while '{SelectedPreset?.Name ?? "No preset"}' is active.");

        Tasks.Add(task);
        SelectedTask = task;
        _deploymentNote = $"{task.Name} entered the bound collection. GridList updated through the generic collection mapper without extra TaskItems property.";
        RaiseDerivedState();
    }

    public void AdvanceSelectedTask()
    {
        var task = GetSelectedTask();
        if (task == null)
            return;

        var nextState = task.State switch
        {
            "Queued" => "Active",
            "Active" => "Ready",
            "Ready" => "Complete",
            "Blocked" => "Active",
            _ => "Queued"
        };

        var updatedTask = task with
        {
            State = nextState,
            Summary = $"State advanced to {nextState} while preserving the same selected index binding."
        };
        Tasks[SelectedTaskIndex] = updatedTask;
        SelectedTask = updatedTask;

        _deploymentNote = $"{task.Name} moved to {nextState}. GridList refreshed from collection state, not from manual row patching.";
        RaiseDerivedState();
    }

    public void MarkDeployed(string? payload)
    {
        if (!CanDeploy)
            return;

        _deploymentNote = $"Deployment armed for {payload ?? DeploymentPayload}. The CTA executed from the control event surface.";
        RaiseDerivedState();
    }

    private BindingTaskRow? GetSelectedTask()
    {
        return SelectedTaskIndex >= 0 && SelectedTaskIndex < Tasks.Count
            ? Tasks[SelectedTaskIndex]
            : null;
    }

    private static GridListItem CreateTaskItem(BindingTaskRow row)
    {
        var item = new GridListItem
        {
            Tag = row
        };

        item.Cells.Add(new GridListCell { Text = row.Name });
        item.Cells.Add(new GridListCell { Text = row.State });
        item.Cells.Add(new GridListCell { Text = row.Lane });
        item.Cells.Add(new GridListCell { Text = row.Summary });
        return item;
    }

    private void RaiseDerivedState()
    {
        OnPropertiesChanged(
            nameof(HeroText),
            nameof(SummaryText),
            nameof(AlertText),
            nameof(StatusText),
            nameof(TaskDetailText),
            nameof(TaskFooterText),
            nameof(DeployActionText),
            nameof(DeploymentPayload),
            nameof(CanDeploy),
            nameof(AlertVisible),
            nameof(AccentColor));
    }
}

internal sealed record BindingPreset(string Name, string Description, SKColor AccentColor, bool AllowsDeployment)
{
    public override string ToString()
    {
        return Name;
    }
}

internal sealed record BindingTaskRow(string Name, string State, string Lane, string Summary);