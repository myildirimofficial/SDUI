using Orivy;
using Orivy.Animation;
using Orivy.Binding;
using Orivy.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Orivy.Example
{
    internal partial class MainWindow : Window
    {
        private enum GridListIconKind
        {
            Healthy,
            Warning,
            Locked,
            Pulse
        }

        private readonly Dictionary<WindowPageTransitionEffect, List<MenuItem>> _transitionMenuItems = new();
        private readonly Dictionary<AnimationType, List<MenuItem>> _transitionEasingMenuItems = new();
        private readonly Dictionary<int, List<MenuItem>> _transitionSpeedMenuItems = new();
        private readonly BindingDemoViewModel _bindingDemoViewModel = new();
        private readonly List<SKImage> _gridListImages = new();
        private Container? _bindingPanel;
        private bool _dangerModeEnabled;
        private int _transitionDurationPreset = 350;

        internal MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeTransitionMenu(MenuItem rootItem)
        {
            // ── Effects ────────────────────────────────────────────────────────────
            RegisterEffectItem(rootItem.AddMenuItem("None",             (_, _) => SetTransitionEffect(WindowPageTransitionEffect.None)),             WindowPageTransitionEffect.None);
            RegisterEffectItem(rootItem.AddMenuItem("Fade",             (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Fade)),             WindowPageTransitionEffect.Fade);
            RegisterEffectItem(rootItem.AddMenuItem("Slide Horizontal", (_, _) => SetTransitionEffect(WindowPageTransitionEffect.SlideHorizontal)), WindowPageTransitionEffect.SlideHorizontal);
            RegisterEffectItem(rootItem.AddMenuItem("Slide Vertical",   (_, _) => SetTransitionEffect(WindowPageTransitionEffect.SlideVertical)),   WindowPageTransitionEffect.SlideVertical);
            RegisterEffectItem(rootItem.AddMenuItem("Scale Fade",       (_, _) => SetTransitionEffect(WindowPageTransitionEffect.ScaleFade)),       WindowPageTransitionEffect.ScaleFade);
            RegisterEffectItem(rootItem.AddMenuItem("Push",             (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Push)),             WindowPageTransitionEffect.Push);
            RegisterEffectItem(rootItem.AddMenuItem("Cover",            (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Cover)),            WindowPageTransitionEffect.Cover);
            RegisterEffectItem(rootItem.AddMenuItem("Reveal",           (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Reveal)),           WindowPageTransitionEffect.Reveal);
            RegisterEffectItem(rootItem.AddMenuItem("Uncover",          (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Uncover)),          WindowPageTransitionEffect.Uncover);
            RegisterEffectItem(rootItem.AddMenuItem("Flip",             (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Flip)),             WindowPageTransitionEffect.Flip);
            RegisterEffectItem(rootItem.AddMenuItem("Iris",             (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Iris)),             WindowPageTransitionEffect.Iris);

            rootItem.AddSeparator();

            // ── Easing ─────────────────────────────────────────────────────────────
            var easingMenu = rootItem.AddMenuItem("Easing");
            RegisterEasingItem(easingMenu.AddMenuItem("Linear",              (_, _) => SetTransitionAnimationType(AnimationType.Linear)),              AnimationType.Linear);
            RegisterEasingItem(easingMenu.AddMenuItem("Ease In",             (_, _) => SetTransitionAnimationType(AnimationType.EaseIn)),              AnimationType.EaseIn);
            RegisterEasingItem(easingMenu.AddMenuItem("Ease Out",            (_, _) => SetTransitionAnimationType(AnimationType.EaseOut)),             AnimationType.EaseOut);
            RegisterEasingItem(easingMenu.AddMenuItem("Ease In \u00b7 Out",  (_, _) => SetTransitionAnimationType(AnimationType.EaseInOut)),           AnimationType.EaseInOut);
            RegisterEasingItem(easingMenu.AddMenuItem("Cubic Ease In",       (_, _) => SetTransitionAnimationType(AnimationType.CubicEaseIn)),         AnimationType.CubicEaseIn);
            RegisterEasingItem(easingMenu.AddMenuItem("Cubic Ease Out",      (_, _) => SetTransitionAnimationType(AnimationType.CubicEaseOut)),        AnimationType.CubicEaseOut);
            RegisterEasingItem(easingMenu.AddMenuItem("Cubic In \u00b7 Out", (_, _) => SetTransitionAnimationType(AnimationType.CubicEaseInOut)),      AnimationType.CubicEaseInOut);
            RegisterEasingItem(easingMenu.AddMenuItem("Quartic In",          (_, _) => SetTransitionAnimationType(AnimationType.QuarticEaseIn)),       AnimationType.QuarticEaseIn);
            RegisterEasingItem(easingMenu.AddMenuItem("Quartic Out",         (_, _) => SetTransitionAnimationType(AnimationType.QuarticEaseOut)),      AnimationType.QuarticEaseOut);
            RegisterEasingItem(easingMenu.AddMenuItem("Quartic In \u00b7 Out", (_, _) => SetTransitionAnimationType(AnimationType.QuarticEaseInOut)), AnimationType.QuarticEaseInOut);

            rootItem.AddSeparator();

            // ── Speed ──────────────────────────────────────────────────────────────
            var speedMenu = rootItem.AddMenuItem("Speed");
            RegisterSpeedItem(speedMenu.AddMenuItem("Fast  (100 ms)",        (_, _) => SetTransitionDuration(100)),   100);
            RegisterSpeedItem(speedMenu.AddMenuItem("Normal  (250 ms)",      (_, _) => SetTransitionDuration(250)),   250);
            RegisterSpeedItem(speedMenu.AddMenuItem("Comfortable  (350 ms)", (_, _) => SetTransitionDuration(350)),   350);
            RegisterSpeedItem(speedMenu.AddMenuItem("Relaxed  (500 ms)",     (_, _) => SetTransitionDuration(500)),   500);
            RegisterSpeedItem(speedMenu.AddMenuItem("Cinematic  (1 s)",      (_, _) => SetTransitionDuration(1000)), 1000);

            RefreshTransitionMenuChecks();
            RefreshTransitionEasingMenuChecks();
            RefreshTransitionSpeedMenuChecks();
        }

        private void RegisterEffectItem(MenuItem item, WindowPageTransitionEffect effect)
        {
            item.CheckOnClick = false;
            if (!_transitionMenuItems.TryGetValue(effect, out var list)) { list = new List<MenuItem>(); _transitionMenuItems[effect] = list; }
            list.Add(item);
        }

        private void RegisterEasingItem(MenuItem item, AnimationType type)
        {
            item.CheckOnClick = false;
            if (!_transitionEasingMenuItems.TryGetValue(type, out var list)) { list = new List<MenuItem>(); _transitionEasingMenuItems[type] = list; }
            list.Add(item);
        }

        private void RegisterSpeedItem(MenuItem item, int ms)
        {
            item.CheckOnClick = false;
            if (!_transitionSpeedMenuItems.TryGetValue(ms, out var list)) { list = new List<MenuItem>(); _transitionSpeedMenuItems[ms] = list; }
            list.Add(item);
        }

        internal void SetTransitionEffect(WindowPageTransitionEffect effect)
        {
            windowPageControl.TransitionEffect = effect;
            RefreshTransitionMenuChecks();
        }

        internal void SetTransitionAnimationType(AnimationType type)
        {
            windowPageControl.TransitionAnimationType = type;
            RefreshTransitionEasingMenuChecks();
        }

        internal void SetTransitionDuration(int ms)
        {
            _transitionDurationPreset = ms;
            windowPageControl.TransitionDurationMs = ms;
            RefreshTransitionSpeedMenuChecks();
        }

        private void RefreshTransitionMenuChecks()
        {
            foreach (var item in _transitionMenuItems)
            {
                var isSelected = item.Key == windowPageControl.TransitionEffect;
                for (var i = 0; i < item.Value.Count; i++)
                    item.Value[i].Checked = isSelected;
            }
        }

        private void RefreshTransitionEasingMenuChecks()
        {
            var current = windowPageControl.TransitionAnimationType;
            foreach (var pair in _transitionEasingMenuItems)
            {
                var isSelected = pair.Key == current;
                for (var i = 0; i < pair.Value.Count; i++)
                    pair.Value[i].Checked = isSelected;
            }
        }

        private void RefreshTransitionSpeedMenuChecks()
        {
            foreach (var pair in _transitionSpeedMenuItems)
            {
                var isSelected = pair.Key == _transitionDurationPreset;
                for (var i = 0; i < pair.Value.Count; i++)
                    pair.Value[i].Checked = isSelected;
            }
        }

        private void ButtonDirectX_Click(object sender, EventArgs e)
        {
            this.RenderBackend = Orivy.Rendering.RenderBackend.DirectX11;
        }

        private void ButtonSoftware_Click(object sender, EventArgs e)
        {
            this.RenderBackend = Orivy.Rendering.RenderBackend.Software;
        }

        private void ButtonOpenGL_Click(object sender, EventArgs e)
        {
            this.RenderBackend = Orivy.Rendering.RenderBackend.OpenGL;
        }

        private void ButtonDarkMode_Click(object sender, EventArgs e)
        {
            ColorScheme.IsDarkMode = !ColorScheme.IsDarkMode;
        }

        private void VisualStyleDangerToggle_Click(object sender, EventArgs e)
        {
            _dangerModeEnabled = !_dangerModeEnabled;
            visualStyleDangerCard.Tag = _dangerModeEnabled ? "danger" : "normal";
            visualStyleDangerCard.Text = _dangerModeEnabled
                ? "Predicate Card\nDanger mode active. Click again to revert."
                : "Predicate Card\nClick to toggle a custom predicate state.";
            visualStyleDangerCard.ReevaluateVisualStyles();
        }

        private void VisualStyleEnableDisabled_Click(object sender, EventArgs e)
        {
            visualStyleDisabledCard.Enabled = !visualStyleDisabledCard.Enabled;
            visualStyleDisabledCard.Text = visualStyleDisabledCard.Enabled
                ? "Disabled State Card\nEnabled again. Click the footer action to disable it."
                : "Disabled State Card\nThis card is disabled and styled by OnDisabled.";
        }

        private void VisualStylePrimaryButton_Click(object sender, EventArgs e)
        {
            visualStyleGhostButton.AccentMotionEnabled = !visualStyleGhostButton.AccentMotionEnabled;
            visualStyleGhostButton.Text = visualStyleGhostButton.AccentMotionEnabled
                ? "Secondary Button - Accent Motion On"
                : "Secondary Button - Accent Motion Off";

            visualStyleScrollProbe.Text = visualStyleGhostButton.AccentMotionEnabled
                ? "Scroll Probe\nSecondary button motion is now enabled. If you can still reach this block, AutoScroll and the new Button control are both working together."
                : "Scroll Probe\nSecondary button motion is now disabled. If you can reach this block, AutoScroll is now measuring content after dock layout. The two Button controls above also prove the new control works inside the example page.";
        }

        private void InitializeGridListDemo()
        {
            var healthyIcon = CreateGridListIcon(new SKColor(34, 197, 94), GridListIconKind.Healthy);
            var warningIcon = CreateGridListIcon(new SKColor(245, 158, 11), GridListIconKind.Warning);
            var lockedIcon  = CreateGridListIcon(new SKColor(239, 68, 68),  GridListIconKind.Locked);
            var pulseIcon   = CreateGridListIcon(new SKColor(59, 130, 246), GridListIconKind.Pulse);

            ConfigurePrimaryGridList(healthyIcon, warningIcon, lockedIcon, pulseIcon);
            ConfigureCompactGridList(healthyIcon, pulseIcon, warningIcon);

            gridListPrimary.SelectedIndex = 0;
            gridListCompact.SelectedIndex = 0;
            UpdateGridListButtons();
            UpdateGridListStatus("Ready", "Primary grid now has enough rows to test sticky header, animated group collapse and optional row resizing in-place.");
        }

        private void ConfigurePrimaryGridList(SKImage healthyIcon, SKImage warningIcon, SKImage lockedIcon, SKImage pulseIcon)
        {
            gridListPrimary.Columns.Clear();
            gridListPrimary.Items.Clear();

            gridListPrimary.Columns.Add(new GridListColumn { Name = "workload", HeaderText = "Workload", Width = 220f, MinWidth = 150f, SizeMode = GridListColumnSizeMode.Auto });
            gridListPrimary.Columns.Add(new GridListColumn { Name = "live",     HeaderText = "Live",     Width = 92f,  MinWidth = 72f,  MaxWidth = 108f, ShowCheckBox = true, CellTextAlign = ContentAlignment.MiddleCenter, SizeMode = GridListColumnSizeMode.Auto });
            gridListPrimary.Columns.Add(new GridListColumn { Name = "owner",    HeaderText = "Owner",    Width = 138f, MinWidth = 110f, SizeMode = GridListColumnSizeMode.Auto });
            gridListPrimary.Columns.Add(new GridListColumn { Name = "latency",  HeaderText = "Latency",  Width = 110f, MinWidth = 88f,  CellTextAlign = ContentAlignment.MiddleRight, SizeMode = GridListColumnSizeMode.Auto });
            gridListPrimary.Columns.Add(new GridListColumn { Name = "summary",  HeaderText = "Summary",  Width = 320f, MinWidth = 220f, Sortable = false, SizeMode = GridListColumnSizeMode.Fill, FillWeight = 1.65f });

            AddPrimaryRow("core",   "Core Systems",    healthyIcon, "Renderer",     true,  "Graphics",   "14 ms", "DirectX11 path is stable; cache hit ratio above target.");
            AddPrimaryRow("core",   "Core Systems",    pulseIcon,   "Layout",       true,  "UI",         "18 ms", "Measure/arrange pass includes nested cards and sticky regions.");
            AddPrimaryRow("core",   "Core Systems",    healthyIcon, "Input Hub",    true,  "Platform",   "16 ms", "Pointer capture and wheel routing stay deterministic through overlays.");
            AddPrimaryRow("core",   "Core Systems",    pulseIcon,   "Theme Engine", true,  "Design",     "19 ms", "Palette interpolation is synchronized with visual-state transitions.");
            AddPrimaryRow("diag",   "Diagnostics",     warningIcon, "Telemetry",    false, "Platform",   "41 ms", "Event batcher is backpressured; investigate queue saturation.");
            AddPrimaryRow("diag",   "Diagnostics",     pulseIcon,   "Scroll Lab",   true,  "QA",         "22 ms", "Wheel routing and thumb drag stay stable under nested hosts.");
            AddPrimaryRow("diag",   "Diagnostics",     warningIcon, "Frame Trace",  true,  "Rendering",  "27 ms", "GPU timings are sampled, but capture export is still warming the pipeline.");
            AddPrimaryRow("diag",   "Diagnostics",     healthyIcon, "Crash Watch",  true,  "Ops",        "13 ms", "Guard rails are live and no fatal exceptions were observed in the last pass.");
            AddPrimaryRow("secure", "Security",        lockedIcon,  "Session Guard",true,  "Identity",   "11 ms", "Lock escalation rules loaded and group policy sync is complete.");
            AddPrimaryRow("secure", "Security",        warningIcon, "Audit Trail",  false, "Compliance", "35 ms", "Retention sweep delayed because archive lane is warming up.");
            AddPrimaryRow("secure", "Security",        lockedIcon,  "Vault Mirror", true,  "Storage",    "17 ms", "Encrypted snapshots are mirrored and signature verification passed.");
            AddPrimaryRow("secure", "Security",        pulseIcon,   "Access Review",true,  "Risk",       "24 ms", "Review queue is active and staged approvals refresh every minute.");
            AddPrimaryRow("ship",   "Release Channel", pulseIcon,   "Preview Ring", true,  "Release",    "21 ms", "Preview users received the latest package and rollback marker is set.");
            AddPrimaryRow("ship",   "Release Channel", warningIcon, "Canary Ring",  false, "Release",    "38 ms", "Canary deployment paused because health probes dipped below threshold.");
            AddPrimaryRow("ship",   "Release Channel", healthyIcon, "Stable Ring",  true,  "Release",    "12 ms", "Stable channel remains green with no pending incidents.");

            gridListPrimary.SortByColumn(0, GridListSortDirection.Ascending);
            gridListPrimary.SelectionChanged += GridListPrimary_SelectionChanged;
            gridListPrimary.CellCheckChanged += GridListPrimary_CellCheckChanged;
            gridListPrimary.ColumnClick      += GridListPrimary_ColumnClick;
            gridListPrimary.CellClick        += GridListPrimary_CellClick;
        }

        private void ConfigureCompactGridList(SKImage healthyIcon, SKImage pulseIcon, SKImage warningIcon)
        {
            gridListCompact.Columns.Clear();
            gridListCompact.Items.Clear();

            gridListCompact.Columns.Add(new GridListColumn { Name = "stream", HeaderText = "Stream", Width = 220f, MinWidth = 150f, SizeMode = GridListColumnSizeMode.Auto });
            gridListCompact.Columns.Add(new GridListColumn { Name = "state",  HeaderText = "State",  Width = 100f, MinWidth = 80f,  CellTextAlign = ContentAlignment.MiddleCenter, SizeMode = GridListColumnSizeMode.Auto });
            gridListCompact.Columns.Add(new GridListColumn { Name = "note",   HeaderText = "Note",   Width = 420f, MinWidth = 220f, Sortable = false, SizeMode = GridListColumnSizeMode.Fill, FillWeight = 1.4f });

            AddCompactRow(healthyIcon, "Commit Watcher", "Live", "High-frequency feed without a header bar.");
            AddCompactRow(pulseIcon,   "Animation Bus",  "Sync", "Transition snapshots update while list selection remains stable.");
            AddCompactRow(warningIcon, "Alert Stream",   "Warn", "Compact list mode still paints icons and supports selection.");
        }

        private void AddPrimaryRow(string groupKey, string groupText, SKImage icon, string workload,
            bool isLive, string owner, string latency, string summary)
        {
            var item = new GridListItem { GroupKey = groupKey, GroupText = groupText, Icon = icon };
            item.Cells.Add(new GridListCell { Text = workload, Icon = icon });
            item.Cells.Add(new GridListCell { CheckState = isLive ? CheckState.Checked : CheckState.Unchecked, Text = isLive ? "On" : "Off" });
            item.Cells.Add(new GridListCell { Text = owner });
            item.Cells.Add(new GridListCell { Text = latency });
            item.Cells.Add(new GridListCell { Text = summary });
            gridListPrimary.Items.Add(item);
        }

        private void AddCompactRow(SKImage icon, string stream, string state, string note)
        {
            var item = new GridListItem { Icon = icon };
            item.Cells.Add(new GridListCell { Text = stream, Icon = icon });
            item.Cells.Add(new GridListCell { Text = state });
            item.Cells.Add(new GridListCell { Text = note });
            gridListCompact.Items.Add(item);
        }

        private SKImage CreateGridListIcon(SKColor accent, GridListIconKind kind)
        {
            var info = new SKImageInfo(18, 18);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            using var fill   = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = accent };
            using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.6f, Color = SKColors.White.WithAlpha(220) };

            switch (kind)
            {
                case GridListIconKind.Healthy:
                    canvas.DrawCircle(9f, 9f, 7f, fill);
                    using (var path = new SKPath())
                    {
                        path.MoveTo(5.4f, 9.3f);
                        path.LineTo(7.7f, 11.7f);
                        path.LineTo(12.8f, 6.3f);
                        canvas.DrawPath(path, stroke);
                    }
                    break;

                case GridListIconKind.Warning:
                    using (var path = new SKPath())
                    {
                        path.MoveTo(9f, 2.2f);
                        path.LineTo(15.2f, 14.8f);
                        path.LineTo(2.8f, 14.8f);
                        path.Close();
                        canvas.DrawPath(path, fill);
                    }
                    canvas.DrawLine(9f, 6f, 9f, 10f, stroke);
                    using (var dotPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = 1.8f, Color = SKColors.White.WithAlpha(220) })
                        canvas.DrawPoint(9f, 12.7f, dotPaint);
                    break;

                case GridListIconKind.Locked:
                    canvas.DrawRoundRect(new SKRect(4.2f, 8f, 13.8f, 15f), 2.2f, 2.2f, fill);
                    using (var path = new SKPath())
                    {
                        path.MoveTo(5.8f, 8f);
                        path.ArcTo(new SKRect(5.8f, 3.2f, 12.2f, 9.8f), 180f, -180f, false);
                        canvas.DrawPath(path, stroke);
                    }
                    break;

                default:
                    canvas.DrawCircle(9f, 9f, 7f, fill);
                    using (var pulse = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.6f, Color = SKColors.White.WithAlpha(220) })
                    using (var path = new SKPath())
                    {
                        path.MoveTo(3.2f, 9.1f);
                        path.LineTo(6.2f, 9.1f);
                        path.LineTo(7.8f, 6.2f);
                        path.LineTo(10.1f, 12.1f);
                        path.LineTo(11.6f, 8.6f);
                        path.LineTo(14.8f, 8.6f);
                        canvas.DrawPath(path, pulse);
                    }
                    break;
            }

            var image = surface.Snapshot();
            _gridListImages.Add(image);
            return image;
        }

        private void GridListPrimary_SelectionChanged(object? sender, GridListSelectionChangedEventArgs e)
        {
            var selected = gridListPrimary.SelectedItem;
            var workload = selected?.Cells.Count > 0 ? selected.Cells[0].Text : "None";
            UpdateGridListStatus("Selection", $"Active row: {workload}. Selected index: {e.SelectedIndex}. Multi-select count: {gridListPrimary.SelectedIndices.Count}.");
        }

        private void GridListPrimary_CellCheckChanged(object? sender, GridListCellCheckChangedEventArgs e)
        {
            UpdateGridListStatus("Checkbox", $"{e.Item.Cells[0].Text} changed from {e.PreviousState} to {e.CurrentState}.");
        }

        private void GridListPrimary_ColumnClick(object? sender, GridListColumnClickEventArgs e)
        {
            UpdateGridListStatus("Sort", $"Column '{e.Column.HeaderText}' clicked. Direction: {e.SortDirection}.");
        }

        private void GridListPrimary_CellClick(object? sender, GridListCellEventArgs e)
        {
            UpdateGridListStatus("Cell Click", $"Row '{e.Item.Cells[0].Text}', column '{e.Column.HeaderText}' was activated.");
        }

        private void GridListToggleHeaderButton_Click(object sender, EventArgs e)
        {
            gridListPrimary.HeaderVisible = !gridListPrimary.HeaderVisible;
            if (!gridListPrimary.HeaderVisible)
                gridListPrimary.StickyHeader = false;
            UpdateGridListButtons();
            UpdateGridListStatus("Display", gridListPrimary.HeaderVisible ? "Primary grid header is visible again." : "Primary grid is now in headerless mode.");
        }

        private void GridListToggleStickyButton_Click(object sender, EventArgs e)
        {
            if (!gridListPrimary.HeaderVisible)
                gridListPrimary.HeaderVisible = true;
            gridListPrimary.StickyHeader = !gridListPrimary.StickyHeader;
            UpdateGridListButtons();
            UpdateGridListStatus("Display", gridListPrimary.StickyHeader ? "Sticky header enabled for the primary grid." : "Sticky header disabled; header scrolls with content.");
        }

        private void GridListToggleGroupingButton_Click(object sender, EventArgs e)
        {
            gridListPrimary.GroupingEnabled = !gridListPrimary.GroupingEnabled;
            UpdateGridListButtons();
            UpdateGridListStatus("Grouping", gridListPrimary.GroupingEnabled ? "Group headers are enabled. Click a group row to collapse it." : "Grouping disabled; rows now render as a flat sorted list.");
        }

        private void GridListToggleGridLinesButton_Click(object sender, EventArgs e)
        {
            var next = !gridListPrimary.ShowGridLines;
            gridListPrimary.ShowGridLines  = next;
            gridListCompact.ShowGridLines  = next;
            UpdateGridListButtons();
            UpdateGridListStatus("Grid Lines", next ? "Row and column separators are visible." : "Grid lines hidden for a cleaner card-like presentation.");
        }

        private void GridListToggleRowResizeButton_Click(object sender, EventArgs e)
        {
            var next = !gridListPrimary.AllowRowResize;
            gridListPrimary.AllowRowResize = next;
            gridListCompact.AllowRowResize = next;
            UpdateGridListButtons();
            UpdateGridListStatus("Row Density", next ? "Row resize enabled. Drag the lower edge of a visible row to change row height." : "Row resize disabled and the grid returns to a fixed rhythm.");
        }

        private void UpdateGridListButtons()
        {
            gridListToggleHeaderButton.Text    = gridListPrimary.HeaderVisible   ? "Header: On"     : "Header: Off";
            gridListToggleStickyButton.Text    = gridListPrimary.StickyHeader    ? "Sticky: On"     : "Sticky: Off";
            gridListToggleGroupingButton.Text  = gridListPrimary.GroupingEnabled ? "Grouping: On"   : "Grouping: Off";
            gridListToggleGridLinesButton.Text = gridListPrimary.ShowGridLines   ? "Grid Lines: On" : "Grid Lines: Off";
            gridListToggleRowResizeButton.Text = gridListPrimary.AllowRowResize  ? "Row Resize: On" : "Row Resize: Off";
        }

        private void UpdateGridListStatus(string title, string body)
        {
            gridListStatus.Text = $"{title}\n{body}";
        }

        private void BindingValidationSubmitButton_Click(object? sender, EventArgs e)
        {
            var isTeamValid = bindingValidationTeamCombo.ValidateNow();
            var isPresetValid = bindingValidationPresetCombo.ValidateNow();

            bindingValidationStatusCard.Text = isTeamValid && isPresetValid
                ? "Validation Submit\nAll rules passed. Existing ValidationRule infrastructure is now gating this small workflow."
                : "Validation Submit\nFix the highlighted rule breaches before continuing. The cards above are bound directly to ValidationText and HasValidationError.";

            bindingValidationStatusCard.BackColor = isTeamValid && isPresetValid
                ? ColorScheme.Primary.WithAlpha(196)
                : new SKColor(185, 28, 28);
            bindingValidationStatusCard.ForeColor = SKColors.White;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (var i = 0; i < _gridListImages.Count; i++)
                    _gridListImages[i].Dispose();
                _gridListImages.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
