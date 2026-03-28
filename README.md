# Orivy
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FSDClowen%2FOrivy.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FSDClowen%2FOrivy?ref=badge_shield)

A fluently windows forms theme library.

## New: TreeView Control
A simple, stylish `TreeView` control has been added under `Orivy/Controls/TreeView.cs` with a supporting `TreeNode` model. It supports:

- Expand/collapse with a clean chevron triangle UI ✅
- Selection with keyboard support (arrow keys) and mouse clicks ✅
- Options: `FullRowSelect` (selects full row) and `HideSelection` (hide highlight when unfocused) ✅
- `NodeMouseClick`, `NodeMouseDoubleClick`, and `NodeMouseRightClick` events are available for node click handling ✅
- Icons: nodes support an optional `IconColor` for a small color icon in the tree ✅
- Multi-selection: select multiple nodes with Ctrl/Shift; use `SelectedNodes` and `SelectedNodesChanged` ✅
- Simple chevron rotation animation for expand/collapse is added ✅
- Demo page with sample data: `Orivy.Test/TreeViewPage.cs` (accessible from the main demo window) ✅

Usage example:
```csharp
var tv = new Orivy.Controls.TreeView();
var root = new Orivy.Controls.TreeNode("Root");
root.Nodes.Add(new Orivy.Controls.TreeNode("Child 1"));
root.Nodes.Add(new Orivy.Controls.TreeNode("Child 2"));
tv.Nodes.Add(root);
tv.SetSelectedNode(root);
```

## Layout compatibility (WinForms) ⚖️
Orivy's layout engine tries to mimic WinForms Dock/AutoSize/Anchor behavior to make porting easier. Key points:

- **Child controls are laid out in the same order they appear in the container's `Controls` collection (the Controls.Add / z-order), matching WinForms behavior**. This ensures controls that rely on add-order docking reproduce WinForms layouts.
- AutoSize controls docked to Top/Bottom/Left/Right use `GetPreferredSize` and respect `AutoSizeMode`, `MinimumSize` and `MaximumSize` when sizing.
- Anchored controls keep their distances to the anchored edges and resize when both opposite anchors are set (e.g., Left+Right or Top+Bottom).

## NEW: Measure/Arrange Layout API 📐

Orivy now implements a **two-phase layout pipeline** similar to WPF/Avalonia for improved layout performance and predictability:

### How It Works

**Phase 1: Measure** - Controls calculate their desired size given available space:
```csharp
Size desiredSize = control.Measure(availableSize);
```

**Phase 2: Arrange** - Controls are positioned and sized to their final bounds:
```csharp
control.Arrange(new Rectangle(x, y, width, height));
```

### Features

- **Measurement Caching**: Results are cached per layout pass to avoid redundant calculations
- **Automatic Cache Invalidation**: Content changes (Text, Font, Image, Padding, Margin) automatically clear cache
- **DPI-Aware**: Layout automatically re-measures on DPI changes
- **Backward Compatible**: Falls back to `GetPreferredSize()` if not overridden

### For Control Authors

Override `Measure()` to provide custom sizing logic:
```csharp
public override Size Measure(Size availableSize)
{
    // Calculate desired size based on content
    var contentSize = MeasureContent(availableSize);
    
    // Apply padding
    contentSize.Width += Padding.Horizontal;
    contentSize.Height += Padding.Vertical;
    
    // Let base class apply MinimumSize/MaximumSize constraints
    return base.Measure(availableSize);
}
```

The layout engine handles:
- Calling `Measure()` before `Arrange()` 
- Respecting `MinimumSize` / `MaximumSize` constraints
- Caching results within the same layout pass
- Invalidating cache when content properties change

See [DOCS/LayoutDesign.md](DOCS/LayoutDesign.md) for full design details.

Demo scenarios added (open Demo app and choose "Layout Test Page"):

- **Dock Test**: All dock styles (Top/Bottom/Left/Right/Fill) with proper measure/arrange flow
- **Anchor Test**: All anchor combinations with window resize
- **AutoSize Test**: Interactive tests showing Text/Font/Padding changes triggering layout
- **Measure API Test**: Direct API usage demonstration

If you rely on unit tests, please note: we run layout verification through the demo app and interactive manual tests. If you see a mismatch with WinForms, open an issue with repro steps.


## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FSDClowen%2FOrivy.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FSDClowen%2FOrivy?ref=badge_large)