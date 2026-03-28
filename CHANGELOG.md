# Changelog

All notable changes to Orivy will be documented in this file.

## [Unreleased]

### Added
- **Measure/Arrange Layout API**: Implemented two-phase layout pipeline for improved performance and predictability
  - `Measure(Size)` method for controls to calculate desired size
  - `Arrange(Rectangle)` method for final positioning
  - Automatic measurement caching per layout pass
  - Cache invalidation on content changes (Text, Font, Image, Padding, Margin)
  - DPI-aware layout re-measurement on display changes
  - Backward compatible with existing `GetPreferredSize()` implementations

- **LayoutEngine Improvements**:
  - Refactored `LayoutEngine.Perform()` to use Measure/Arrange instead of direct property manipulation
  - Added `MeasureChild()` and `ArrangeChild()` helpers for margin-aware layout
  - Centralized MinimumSize/MaximumSize constraint enforcement in `Measure()` method

- **Demo/Test Pages**:
  - Added comprehensive `LayoutTestPage` demonstrating Dock, Anchor, AutoSize, and Measure/Arrange API usage
  - Interactive tests for Text/Font/Padding changes triggering layout updates

### Changed
- Layout system now uses global layout pass ID for measurement caching
- Content-affecting property setters (Text, Font, Image, Padding, Margin) now call `InvalidateMeasure()`
- DPI change events now trigger `PerformLayout()` to ensure proper layout update

### Technical Details
- See [DOCS/LayoutDesign.md](DOCS/LayoutDesign.md) for full design specification
- Affects: `UIElementBase.cs`, `LayoutEngine.cs`, all controls inheriting layout behavior

## [Previous Versions]
- TreeView Control
- Layout compatibility improvements
- Font caching and DPI handling
- Various UI controls and theming features
