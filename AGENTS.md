# AGENTS.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Orivy is a **custom Skia-based UI framework** for .NET that implements a retained-mode UI system with WinForms-like semantics. It is NOT a WinForms wrapper — all layout, input, animation, and rendering is custom-built on SkiaSharp. The rendering pipeline supports DirectX11 (with Vortice) with fallback to CPU rendering.

## Build Commands

```powershell
# Build entire solution
dotnet build Orivy.sln

# Build specific project
dotnet build Orivy/Orivy.csproj
dotnet build Orivy.Test/Orivy.Demo.csproj
dotnet build Orivy.Designer/Orivy.Designer.csproj

# Build for release
dotnet build Orivy.sln -c Release
```

## Run the Demo App

```powershell
dotnet run --project Orivy.Test/Orivy.Demo.csproj
```

The demo app contains interactive test pages for Dock, Anchor, AutoSize, TreeView, and the Measure/Arrange API.

## Architecture

### Core Components

- **Orivy/** — Main library with all UI controls and framework code
  - `Controls/UIElementBase.cs` — Base class for all UI elements; implements `IArrangedElement`
  - `Controls/UIWindowBase.cs` — Base class for windows; handles Win32 message loop via `WndProc`
  - `Layout/LayoutEngine.cs` — Abstract layout engine; `DefaultLayout.cs` implements WinForms-like Dock/Anchor/AutoSize
  - `Rendering/` — DirectX11 and OpenGL window renderers; `DirectX11WindowRenderer.cs` is the primary GPU path
  - `ColorScheme.cs` — Centralized theme system with light/dark mode and animated transitions
  - `Animation/` — Delta-time driven animation system with easing functions

- **Orivy.Test/** (Orivy.Demo) — Demo/test application showcasing controls and layout
- **Orivy.Designer/** — Visual designer tool (WIP)

### Layout System (Two-Phase Pipeline)

The layout follows WinForms semantics with a WPF-like Measure/Arrange API:

1. **Measure phase**: `control.Measure(availableSize)` calculates desired size (cached per layout pass)
2. **Arrange phase**: `control.Arrange(finalRect)` positions and sizes control to final bounds

Key files: `Orivy/Layout/DefaultLayout.cs`, `Orivy/Controls/UIElementBase.Layout.cs`

Child controls are laid out in **z-order** (Controls.Add order) matching WinForms behavior.

### Rendering Pipeline

- All drawing uses SkiaSharp (`SKCanvas`)
- GPU rendering via DirectX11 swapchain when available; CPU fallback via texture upload
- No system UI components or OS-native widgets

### Control Lifecycle

Each control has explicit lifecycle methods:
- `Measure(Size)` — Calculate desired size
- `Arrange(Rectangle)` — Final positioning  
- `OnPaint(SKCanvas)` — Rendering via Skia
- Input handling through `OnMouseDown`, `OnKeyDown`, etc.

## Coding Guidelines (from .github/copilot-instructions.md)

### Mandatory Rules

- **Retained-mode UI** — Do not mix with immediate-mode
- All rendering through `SKCanvas` — No system/native widgets
- Explicit layout engine — No auto-layout magic
- Prefer composition over inheritance

### Performance Rules

- Avoid allocations in render loop
- Reuse paint objects, paths, and cached layouts
- Avoid LINQ in hot paths
- Cache text measurements

### Code Quality

- No TODOs, commented-out code, or placeholder implementations
- No empty catch blocks or silent failures
- Every method must have clear responsibility and explicit I/O

### Styling

- No hardcoded colors inside logic — use `ColorScheme` centralized theme
- Immutable style definitions
- No global mutable theme state

### Forbidden

- XAML, WPF concepts (DependencyProperty, RoutedEvent)
- MAUI/Avalonia references
- Reflection-based UI generation
- Dynamic typing

## Key Types Reference

| Type | Purpose |
|------|---------|
| `UIElementBase` | Base class for all controls |
| `UIWindowBase` | Base class for top-level windows |
| `IArrangedElement` | Layout contract (Bounds, SetBounds, GetPreferredSize) |
| `DefaultLayout` | WinForms-compatible Dock/Anchor layout engine |
| `ColorScheme` | Theme colors, light/dark mode, transitions |
| `AnimationManager` | Delta-time animation with easing |
| `DirectX11WindowRenderer` | GPU-accelerated Skia rendering |

## Dependencies

- **SkiaSharp 3.x** — Rendering engine
- **Vortice.Direct3D11 / Vortice.DXGI** — DirectX11 interop
- Target: .NET 8.0 (Windows)
