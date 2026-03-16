using SDUI;
using SDUI.Controls;
using System;
using System.Collections.Generic;

namespace Orivy.Example
{
    internal partial class MainWindow : Window
    {
        private readonly Dictionary<WindowPageTransitionEffect, List<MenuItem>> _transitionMenuItems = new();
        private bool _dangerModeEnabled;

        internal MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeTransitionMenu(MenuItem rootItem)
        {
            rootItem.AddMenuItem("None", (_, _) => SetTransitionEffect(WindowPageTransitionEffect.None));
            rootItem.AddMenuItem("Fade", (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Fade));
            rootItem.AddMenuItem("Slide Horizontal", (_, _) => SetTransitionEffect(WindowPageTransitionEffect.SlideHorizontal));
            rootItem.AddMenuItem("Slide Vertical", (_, _) => SetTransitionEffect(WindowPageTransitionEffect.SlideVertical));
            rootItem.AddMenuItem("Scale Fade", (_, _) => SetTransitionEffect(WindowPageTransitionEffect.ScaleFade));
            rootItem.AddMenuItem("Push", (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Push));
            rootItem.AddMenuItem("Cover", (_, _) => SetTransitionEffect(WindowPageTransitionEffect.Cover));

            for (var i = 0; i < rootItem.DropDownItems.Count; i++)
            {
                var menuItem = rootItem.DropDownItems[i];
                menuItem.CheckOnClick = false;

                if (!TryParseTransitionEffect(menuItem.Text, out var effect))
                    continue;

                if (!_transitionMenuItems.TryGetValue(effect, out var items))
                {
                    items = new List<MenuItem>();
                    _transitionMenuItems[effect] = items;
                }

                items.Add(menuItem);
            }

            RefreshTransitionMenuChecks();
        }

        private void SetTransitionEffect(WindowPageTransitionEffect effect)
        {
            windowPageControl.TransitionEffect = effect;
            RefreshTransitionMenuChecks();
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

        private static bool TryParseTransitionEffect(string text, out WindowPageTransitionEffect effect)
        {
            effect = text switch
            {
                "None" => WindowPageTransitionEffect.None,
                "Fade" => WindowPageTransitionEffect.Fade,
                "Slide Horizontal" => WindowPageTransitionEffect.SlideHorizontal,
                "Slide Vertical" => WindowPageTransitionEffect.SlideVertical,
                "Scale Fade" => WindowPageTransitionEffect.ScaleFade,
                "Push" => WindowPageTransitionEffect.Push,
                "Cover" => WindowPageTransitionEffect.Cover,
                _ => WindowPageTransitionEffect.SlideHorizontal
            };

            return text is "None" or "Fade" or "Slide Horizontal" or "Slide Vertical" or "Scale Fade" or "Push" or "Cover";
        }

        private void ButtonDirectX_Click(object sender, EventArgs e)
        {
            this.RenderBackend = SDUI.Rendering.RenderBackend.DirectX11;
        }

        private void ButtonSoftware_Click(object sender, EventArgs e)
        {
            this.RenderBackend = SDUI.Rendering.RenderBackend.Software;
        }

        private void ButtonOpenGL_Click(object sender, EventArgs e)
        {
            this.RenderBackend = SDUI.Rendering.RenderBackend.OpenGL;
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
    }
}
