using SDUI.Animation;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace SDUI.Controls;

public class WindowPageControl : ElementBase
{
    private readonly AnimationManager _transitionAnimation;
    private EventHandler<int>? _onSelectedIndexChanged;
    private bool _isTransitionDirty;
    private int _selectedIndex = -1;
    private int _transitionFromIndex = -1;
    private int _transitionToIndex = -1;
    private SKBitmap? _transitionFromSnapshot;
    private SKBitmap? _transitionToSnapshot;
    private readonly SKPaint _transitionPaint;

    public override SKColor BackColor
    {
        get => SKColors.Transparent;
        set { }
    }

    public WindowPageControl()
    {

        _transitionPaint = new SKPaint
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.SrcOver
        };

        _transitionAnimation = new AnimationManager
        {
            Singular = true,
            InterruptAnimation = true,
            Increment = 0.22,
            SecondaryIncrement = 0.22,
            AnimationType = AnimationType.CubicEaseOut
        };
        _transitionAnimation.OnAnimationProgress += HandleTransitionProgress;
        _transitionAnimation.OnAnimationFinished += HandleTransitionFinished;
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool EnableTransitions { get; set; } = true;

    [Category("Behavior")]
    [DefaultValue(WindowPageTransitionEffect.SlideHorizontal)]
    public WindowPageTransitionEffect TransitionEffect { get; set; } = WindowPageTransitionEffect.SlideHorizontal;

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool LockInputDuringTransition { get; set; } = true;

    [Category("Behavior")]
    [DefaultValue(AnimationType.CubicEaseOut)]
    public AnimationType TransitionAnimationType
    {
        get => _transitionAnimation.AnimationType;
        set => _transitionAnimation.AnimationType = value;
    }

    [Category("Behavior")]
    [DefaultValue(0.18d)]
    public double TransitionIncrement
    {
        get => _transitionAnimation.Increment;
        set => _transitionAnimation.Increment = ValidateIncrement(value);
    }

    [Category("Behavior")]
    [DefaultValue(0.18d)]
    public double TransitionSecondaryIncrement
    {
        get => _transitionAnimation.SecondaryIncrement;
        set => _transitionAnimation.SecondaryIncrement = ValidateIncrement(value);
    }

    [Category("Behavior")]
    [DefaultValue(300)]
    public int TransitionDurationMs
    {
        get => (int)Math.Round(16.0 / _transitionAnimation.Increment);
        set
        {
            var clamped = Math.Max(50, Math.Min(5000, value));
            var inc = 16.0 / clamped;
            _transitionAnimation.Increment = inc;
            _transitionAnimation.SecondaryIncrement = inc;
        }
    }

    [Browsable(false)]
    public bool IsTransitioning => _transitionAnimation.IsAnimating() && _transitionFromSnapshot != null && _transitionToSnapshot != null;

    private bool IsPageControl(ElementBase element)
    {
        return element is Container;
    }

    private int GetPageCount()
    {
        var count = 0;
        for (var i = 0; i < Controls.Count; i++)
        {
            if (Controls[i] is ElementBase element && IsPageControl(element))
                count++;
        }

        return count;
    }

    public ElementBase? GetPageAt(int pageIndex)
    {
        if (pageIndex < 0)
            return null;

        var currentPageIndex = 0;
        for (var i = 0; i < Controls.Count; i++)
        {
            if (Controls[i] is not ElementBase element || !IsPageControl(element))
                continue;

            if (currentPageIndex == pageIndex)
                return element;

            currentPageIndex++;
        }

        return null;
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var sys = Stopwatch.StartNew();

            if (_selectedIndex == value)
                return;

            var pageCount = GetPageCount();
            if (pageCount > 0)
            {
                if (value < 0)
                    value = pageCount - 1;

                if (value > pageCount - 1)
                    value = 0;
            }
            else
            {
                value = -1;
            }

            var previousSelectedIndex = _selectedIndex;
            _selectedIndex = value;

            var transitionStarted = TryStartTransition(previousSelectedIndex, _selectedIndex);
            if (!transitionStarted)
                CommitSelectedPageVisibility();

            _onSelectedIndexChanged?.Invoke(this, previousSelectedIndex);

            InvalidateRenderTree();
            Invalidate();

            Debug.WriteLine($"Index: {_selectedIndex} Finished: {sys.ElapsedMilliseconds} ms");
        }
    }

    public int Count => GetPageCount();

    public event EventHandler<int> SelectedIndexChanged
    {
        add => _onSelectedIndexChanged += value;
        remove => _onSelectedIndexChanged -= value;
    }

    public void StopTransition(bool commitTargetPage = true)
    {
        ReleaseTransitionSnapshots();
        _transitionAnimation.SetProgress(commitTargetPage ? 1 : 0);
        _transitionFromIndex = -1;
        _transitionToIndex = -1;
        _isTransitionDirty = false;

        if (commitTargetPage)
            CommitSelectedPageVisibility();

        InvalidateRenderTree();
        Invalidate();
    }

    internal override void OnControlAdded(ElementEventArgs e)
    {
        base.OnControlAdded(e);

        if (e.Element is not ElementBase element || !IsPageControl(element))
            return;

        element.Dock = DockStyle.Fill;
        SyncPageBounds(element);
        element.Visible = Count == 1;

        if (Count == 1)
            _selectedIndex = 0;
        else if (element.Visible)
            CommitSelectedPageVisibility();

        CancelTransitionPreservingSelection();
    }

    internal override void OnControlRemoved(ElementEventArgs e)
    {
        base.OnControlRemoved(e);

        if (e.Element is not ElementBase element || !IsPageControl(element))
            return;

        if (Count == 0)
            _selectedIndex = -1;
        else if (_selectedIndex >= Count)
            SelectedIndex = Count - 1;

        CancelTransitionPreservingSelection();
    }

    internal override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (!Visible && IsTransitioning)
            StopTransition();
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        SyncAllPageBounds();

        if (IsTransitioning)
            _isTransitionDirty = true;
    }

    protected override bool ShouldIncludeHitTestElement(ElementBase element, bool requireEnabled)
    {
        if (!base.ShouldIncludeHitTestElement(element, requireEnabled))
            return false;

        if (!IsPageControl(element) || !IsTransitioning || !LockInputDuringTransition)
            return true;

        var targetPage = GetPageAt(_transitionToIndex);
        return ReferenceEquals(targetPage, element);
    }

    protected override bool TryRenderChildContent(SKCanvas canvas)
    {
        if (!IsTransitioning)
            return false;

        if (!EnsureTransitionSnapshots())
            return false;

        var viewport = GetTransitionViewport();
        if (viewport.Width <= 0 || viewport.Height <= 0)
            return false;

        var fromSnapshot = _transitionFromSnapshot;
        var toSnapshot = _transitionToSnapshot;
        if (fromSnapshot == null || toSnapshot == null)
            return false;

        var progress = Math.Clamp((float)_transitionAnimation.GetProgress(), 0f, 1f);
        var saved = canvas.Save();
        canvas.ClipRect(viewport);
        DrawTransitionEffect(canvas, fromSnapshot, toSnapshot, viewport, progress);
        canvas.RestoreToCount(saved);
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _transitionAnimation.OnAnimationProgress -= HandleTransitionProgress;
            _transitionAnimation.OnAnimationFinished -= HandleTransitionFinished;
            _transitionAnimation.Dispose();
            _transitionPaint.Dispose();
            ReleaseTransitionSnapshots();
        }

        base.Dispose(disposing);
    }

    private void HandleTransitionProgress(object _)
    {
        Invalidate();
    }

    private void HandleTransitionFinished(object _)
    {
        CommitSelectedPageVisibility();
        ReleaseTransitionSnapshots();
        _transitionFromIndex = -1;
        _transitionToIndex = -1;
        _isTransitionDirty = false;
        InvalidateRenderTree();
        Invalidate();
    }

    private bool TryStartTransition(int previousSelectedIndex, int nextSelectedIndex)
    {
        var carryForwardSnapshot = IsTransitioning ? CaptureActiveTransitionSnapshot() : null;

        if (!ShouldAnimateTransition(previousSelectedIndex, nextSelectedIndex))
        {
            carryForwardSnapshot?.Dispose();
            return false;
        }

        SyncAllPageBounds();

        _transitionFromIndex = previousSelectedIndex;
        _transitionToIndex = nextSelectedIndex;
        _isTransitionDirty = false;

        if (!RebuildTransitionSnapshots(carryForwardSnapshot))
        {
            carryForwardSnapshot?.Dispose();
            ReleaseTransitionSnapshots();
            _transitionFromIndex = -1;
            _transitionToIndex = -1;
            return false;
        }

        CommitTransitionVisibilityState();

        _transitionAnimation.SetProgress(0);
        _transitionAnimation.StartNewAnimation(AnimationDirection.In);
        return true;
    }

    private bool ShouldAnimateTransition(int previousSelectedIndex, int nextSelectedIndex)
    {
        if (!EnableTransitions || TransitionEffect == WindowPageTransitionEffect.None)
            return false;

        if (previousSelectedIndex < 0 || nextSelectedIndex < 0 || previousSelectedIndex == nextSelectedIndex)
            return false;

        var fromPage = GetPageAt(previousSelectedIndex);
        var toPage = GetPageAt(nextSelectedIndex);
        if (fromPage == null || toPage == null)
            return false;

        var viewport = GetTransitionViewport();
        return viewport.Width > 0 && viewport.Height > 0;
    }

    private void CommitSelectedPageVisibility()
    {
        var currentPageIndex = 0;
        for (var i = 0; i < Controls.Count; i++)
        {
            if (Controls[i] is not ElementBase element || !IsPageControl(element))
                continue;

            SyncPageBounds(element);
            element.Visible = currentPageIndex == _selectedIndex;
            currentPageIndex++;
        }
    }

    private void CommitTransitionVisibilityState()
    {
        var targetPage = GetPageAt(_transitionToIndex);

        for (var pageIndex = 0; pageIndex < Count; pageIndex++)
        {
            var page = GetPageAt(pageIndex);
            if (page == null)
                continue;

            SyncPageBounds(page);
            page.Visible = ReferenceEquals(page, targetPage);
        }
    }

    private void CancelTransitionPreservingSelection()
    {
        ReleaseTransitionSnapshots();
        _transitionFromIndex = -1;
        _transitionToIndex = -1;
        _isTransitionDirty = false;
        CommitSelectedPageVisibility();
        InvalidateRenderTree();
        Invalidate();
    }

    private bool EnsureTransitionSnapshots()
    {
        if (_isTransitionDirty)
            return RebuildTransitionSnapshots();

        return _transitionFromSnapshot != null && _transitionToSnapshot != null;
    }

    private bool RebuildTransitionSnapshots(SKBitmap? fromSnapshotOverride = null)
    {
        ReleaseTransitionSnapshots();

        var fromPage = GetPageAt(_transitionFromIndex);
        var toPage = GetPageAt(_transitionToIndex);
        if (fromPage == null || toPage == null)
            return false;

        SyncPageBounds(fromPage);
        SyncPageBounds(toPage);

        var fromSnapshot = fromSnapshotOverride ?? CapturePageSnapshot(fromPage);
        var toSnapshot = CapturePageSnapshot(toPage);
        if (fromSnapshot == null || toSnapshot == null)
        {
            fromSnapshot?.Dispose();
            toSnapshot?.Dispose();
            return false;
        }

        _transitionFromSnapshot = fromSnapshot;
        _transitionToSnapshot = toSnapshot;
        _isTransitionDirty = false;
        return true;
    }

    private SKBitmap? CaptureActiveTransitionSnapshot()
    {
        var fromSnapshot = _transitionFromSnapshot;
        var toSnapshot = _transitionToSnapshot;
        if (fromSnapshot == null || toSnapshot == null)
            return null;

        var viewport = GetTransitionViewport();
        var width = Math.Max(1, (int)Math.Ceiling(viewport.Width));
        var height = Math.Max(1, (int)Math.Ceiling(viewport.Height));
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        using var surface = SKSurface.Create(info, bitmap.GetPixels(), info.RowBytes);
        if (surface == null)
        {
            bitmap.Dispose();
            return null;
        }

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        DrawTransitionEffect(canvas, fromSnapshot, toSnapshot, SKRect.Create(0, 0, width, height),
            Math.Clamp((float)_transitionAnimation.GetProgress(), 0f, 1f));
        surface.Flush();
        return bitmap;
    }

    private SKBitmap? CapturePageSnapshot(ElementBase page)
    {
        var bounds = page.Bounds;
        var width = Math.Max(1, (int)Math.Ceiling(bounds.Width));
        var height = Math.Max(1, (int)Math.Ceiling(bounds.Height));
        if (width <= 0 || height <= 0)
            return null;

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        using var surface = SKSurface.Create(info, bitmap.GetPixels(), info.RowBytes);
        if (surface == null)
        {
            bitmap.Dispose();
            return null;
        }

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var originalVisible = page.Visible;
        var originalLocation = page.Location;
        var originalBounds = page.Bounds;

        page.Visible = true;
        page.Location = SKPoint.Empty;

        try
        {
            page.Render(canvas);
        }
        finally
        {
            page.Bounds = originalBounds;
            page.Location = originalLocation;
            page.Visible = originalVisible;
        }

        surface.Flush();
        return bitmap;
    }

    private void DrawTransitionEffect(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport,
        float progress)
    {
        switch (TransitionEffect)
        {
            case WindowPageTransitionEffect.Fade:
                DrawFade(canvas, fromSnapshot, toSnapshot, viewport, progress);
                break;
            case WindowPageTransitionEffect.SlideHorizontal:
                DrawSlideHorizontal(canvas, fromSnapshot, toSnapshot, viewport, progress, pushExistingPage: false);
                break;
            case WindowPageTransitionEffect.SlideVertical:
                DrawSlideVertical(canvas, fromSnapshot, toSnapshot, viewport, progress);
                break;
            case WindowPageTransitionEffect.ScaleFade:
                DrawScaleFade(canvas, fromSnapshot, toSnapshot, viewport, progress);
                break;
            case WindowPageTransitionEffect.Push:
                DrawSlideHorizontal(canvas, fromSnapshot, toSnapshot, viewport, progress, pushExistingPage: true, drawJunctionShadow: true);
                break;
            case WindowPageTransitionEffect.Cover:
                DrawCover(canvas, fromSnapshot, toSnapshot, viewport, progress);
                break;
            case WindowPageTransitionEffect.Reveal:
                DrawReveal(canvas, fromSnapshot, toSnapshot, viewport, progress);
                break;
            case WindowPageTransitionEffect.Uncover:
                DrawUncover(canvas, fromSnapshot, toSnapshot, viewport, progress);
                break;
            case WindowPageTransitionEffect.Flip:
                DrawFlip(canvas, fromSnapshot, toSnapshot, viewport, progress);
                break;
            case WindowPageTransitionEffect.Iris:
                DrawIris(canvas, fromSnapshot, toSnapshot, viewport, progress);
                break;
            default:
                DrawBitmap(canvas, toSnapshot, viewport, 255);
                break;
        }
    }

    private void DrawFade(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport, float progress)
    {
        DrawBitmap(canvas, fromSnapshot, viewport, (byte)(255f * (1f - progress)));
        DrawBitmap(canvas, toSnapshot, viewport, (byte)(255f * progress));
    }

    private void DrawSlideHorizontal(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport,
        float progress, bool pushExistingPage, bool drawJunctionShadow = false)
    {
        var direction = GetDirectionalSign();
        var offset = viewport.Width * progress * direction;

        var fromRect = viewport;
        fromRect.Offset(pushExistingPage ? -offset : 0, 0);

        var toRect = viewport;
        toRect.Offset((direction > 0 ? viewport.Width : -viewport.Width) - offset, 0);

        DrawBitmap(canvas, fromSnapshot, fromRect, 255);
        DrawBitmap(canvas, toSnapshot, toRect, 255);

        if (drawJunctionShadow)
        {
            var junctionX = direction > 0 ? fromRect.Right : fromRect.Left;
            const float shadowHalfWidth = 14f;
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(junctionX - shadowHalfWidth, 0f),
                new SKPoint(junctionX + shadowHalfWidth, 0f),
                new[] { SKColors.Transparent, SKColors.Black.WithAlpha(55), SKColors.Transparent },
                new[] { 0f, 0.5f, 1f },
                SKShaderTileMode.Clamp);
            using var shadowPaint = new SKPaint { Shader = shader, IsAntialias = true };
            canvas.DrawRect(SKRect.Create(
                junctionX - shadowHalfWidth, viewport.Top, shadowHalfWidth * 2f, viewport.Height), shadowPaint);
        }
    }

    private void DrawSlideVertical(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport,
        float progress)
    {
        var direction = GetDirectionalSign();
        var offset = viewport.Height * progress * direction;

        var fromRect = viewport;
        fromRect.Offset(0, -offset);

        var toRect = viewport;
        toRect.Offset(0, (direction > 0 ? viewport.Height : -viewport.Height) - offset);

        DrawBitmap(canvas, fromSnapshot, fromRect, 255);
        DrawBitmap(canvas, toSnapshot, toRect, 255);

        // horizontal shadow line at the junction between pages
        var junctionY = direction > 0 ? fromRect.Bottom : fromRect.Top;
        const float shadowHalfHeight = 14f;
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0f, junctionY - shadowHalfHeight),
            new SKPoint(0f, junctionY + shadowHalfHeight),
            new[] { SKColors.Transparent, SKColors.Black.WithAlpha(55), SKColors.Transparent },
            new[] { 0f, 0.5f, 1f },
            SKShaderTileMode.Clamp);
        using var shadowPaint = new SKPaint { Shader = shader, IsAntialias = true };
        canvas.DrawRect(SKRect.Create(
            viewport.Left, junctionY - shadowHalfHeight, viewport.Width, shadowHalfHeight * 2f), shadowPaint);
    }

    private void DrawReveal(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport, float progress)
    {
        // from: fades out and scales down slightly, conveying it is leaving
        var fromScale = 1f - 0.08f * progress;
        var fromW = viewport.Width * fromScale;
        var fromH = viewport.Height * fromScale;
        var fromRect = SKRect.Create(
            viewport.MidX - fromW / 2f,
            viewport.MidY - fromH / 2f,
            fromW, fromH);
        DrawBitmap(canvas, fromSnapshot, fromRect, (byte)(255f * (1f - progress)));

        // to: scales up from 0.94 to 1.0 and fades in
        var toScale = 0.94f + 0.06f * progress;
        var toW = viewport.Width * toScale;
        var toH = viewport.Height * toScale;
        var toRect = SKRect.Create(
            viewport.MidX - toW / 2f,
            viewport.MidY - toH / 2f,
            toW, toH);
        DrawBitmap(canvas, toSnapshot, toRect, (byte)(255f * progress));
    }

    private void DrawUncover(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport, float progress)
    {
        // to: sits in place underneath, revealed as from slides away
        DrawBitmap(canvas, toSnapshot, viewport, 255);

        // from: slides out in the transition direction
        var direction = GetDirectionalSign();
        var fromRect = viewport;
        fromRect.Offset(direction * viewport.Width * progress, 0f);
        DrawBitmap(canvas, fromSnapshot, fromRect, 255);

        // shadow on the trailing (inward-facing) edge of the from page
        DrawLeadingEdgeShadow(canvas, fromRect, viewport, direction);
    }

    private void DrawFlip(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport, float progress)
    {
        // Horizontal card flip via horizontal squash
        // 0→0.5: from squashes from full width to zero
        // 0.5→1: to expands from zero to full width
        // dark overlay peaks at the flip midpoint
        var darkness = 1f - Math.Abs(progress - 0.5f) * 2f;

        if (progress < 0.5f)
        {
            var t = progress * 2f;
            var w = Math.Max(0f, viewport.Width * (1f - t));
            var flipRect = SKRect.Create(viewport.MidX - w / 2f, viewport.Top, w, viewport.Height);
            DrawBitmap(canvas, fromSnapshot, flipRect, 255);
        }
        else
        {
            var t = (progress - 0.5f) * 2f;
            var w = Math.Max(0f, viewport.Width * t);
            var flipRect = SKRect.Create(viewport.MidX - w / 2f, viewport.Top, w, viewport.Height);
            DrawBitmap(canvas, toSnapshot, flipRect, 255);
        }

        if (darkness > 0.01f)
        {
            using var darkPaint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha((byte)(88 * darkness)),
                IsAntialias = true
            };
            canvas.DrawRect(viewport, darkPaint);
        }
    }

    private void DrawIris(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport, float progress)
    {
        // from: fades slightly as the iris expands over it
        DrawBitmap(canvas, fromSnapshot, viewport, (byte)(255f * (1f - progress * 0.35f)));

        if (progress <= 0f)
            return;

        var cx = viewport.MidX;
        var cy = viewport.MidY;
        var maxRadius = MathF.Sqrt(
            viewport.Width * viewport.Width + viewport.Height * viewport.Height) / 2f * 1.06f;
        var radius = maxRadius * progress;

        using var clipPath = new SKPath();
        clipPath.AddCircle(cx, cy, radius);

        var saved = canvas.Save();
        canvas.ClipPath(clipPath, antialias: true);
        DrawBitmap(canvas, toSnapshot, viewport, 255);
        canvas.RestoreToCount(saved);
    }

    private void DrawScaleFade(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport,
        float progress)
    {
        DrawBitmap(canvas, fromSnapshot, viewport, (byte)(255f * (1f - progress)));

        var scale = 0.92f + (0.08f * progress);
        var scaledWidth = viewport.Width * scale;
        var scaledHeight = viewport.Height * scale;
        var scaledRect = SKRect.Create(
            viewport.MidX - scaledWidth / 2f,
            viewport.MidY - scaledHeight / 2f,
            scaledWidth,
            scaledHeight);

        DrawBitmap(canvas, toSnapshot, scaledRect, (byte)(255f * progress));
    }

    private void DrawCover(SKCanvas canvas, SKBitmap fromSnapshot, SKBitmap toSnapshot, SKRect viewport, float progress)
    {
        // from: stays in place, scales down slightly to convey it is being covered
        var fromScale = 1f - 0.05f * progress;
        var sw = viewport.Width * fromScale;
        var sh = viewport.Height * fromScale;
        var fromRect = SKRect.Create(
            viewport.MidX - sw / 2f,
            viewport.MidY - sh / 2f,
            sw, sh);
        DrawBitmap(canvas, fromSnapshot, fromRect, (byte)(255f * (1f - progress * 0.3f)));

        // to: slides in from the side, fully opaque, covering the from page
        var direction = GetDirectionalSign();
        var toRect = viewport;
        toRect.Offset((direction > 0 ? viewport.Width : -viewport.Width) * (1f - progress), 0f);
        DrawBitmap(canvas, toSnapshot, toRect, 255);

        // leading-edge shadow on the incoming page to convey depth
        DrawLeadingEdgeShadow(canvas, toRect, viewport, direction);
    }

    private static void DrawLeadingEdgeShadow(SKCanvas canvas, SKRect pageRect, SKRect clip, int direction)
    {
        const float shadowWidth = 22f;
        float shadowLeft;
        SKPoint gradStart, gradEnd;

        if (direction > 0)
        {
            shadowLeft = pageRect.Left;
            gradStart = new SKPoint(shadowLeft, 0f);
            gradEnd = new SKPoint(shadowLeft + shadowWidth, 0f);
        }
        else
        {
            shadowLeft = pageRect.Right - shadowWidth;
            gradStart = new SKPoint(pageRect.Right, 0f);
            gradEnd = new SKPoint(pageRect.Right - shadowWidth, 0f);
        }

        var shadowRect = SKRect.Create(shadowLeft, clip.Top, shadowWidth, clip.Height);
        if (shadowRect.Right <= clip.Left || shadowRect.Left >= clip.Right)
            return;

        using var shader = SKShader.CreateLinearGradient(
            gradStart, gradEnd,
            new[] { SKColors.Black.WithAlpha(75), SKColors.Transparent },
            null,
            SKShaderTileMode.Clamp);
        using var shadowPaint = new SKPaint { Shader = shader, IsAntialias = true };
        canvas.DrawRect(shadowRect, shadowPaint);
    }

    private void DrawBitmap(SKCanvas canvas, SKBitmap bitmap, SKRect destinationRect, byte alpha)
    {
        if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0 || destinationRect.Width <= 0 || destinationRect.Height <= 0)
            return;

        var scaleX = destinationRect.Width / bitmap.Width;
        var scaleY = destinationRect.Height / bitmap.Height;

        if (float.IsInfinity(scaleX) || float.IsNaN(scaleX) || float.IsInfinity(scaleY) || float.IsNaN(scaleY))
            return;

        _transitionPaint.Color = SKColors.White.WithAlpha(alpha);

        var saved = canvas.Save();
        canvas.Translate(destinationRect.Left, destinationRect.Top);
        canvas.Scale(scaleX, scaleY);
        canvas.DrawBitmap(bitmap, 0, 0, _transitionPaint);
        canvas.RestoreToCount(saved);
    }

    private int GetDirectionalSign()
    {
        if (_transitionFromIndex < 0 || _transitionToIndex < 0)
            return 1;

        return _transitionToIndex >= _transitionFromIndex ? 1 : -1;
    }

    private SKRect GetTransitionViewport()
    {
        var selectedPage = GetPageAt(_selectedIndex) ?? GetPageAt(_transitionFromIndex) ?? GetPageAt(_transitionToIndex);
        if (selectedPage != null)
        {
            SyncPageBounds(selectedPage);
            return selectedPage.Bounds;
        }

        return DisplayRectangle;
    }

    private void SyncAllPageBounds()
    {
        for (var pageIndex = 0; pageIndex < Count; pageIndex++)
        {
            var page = GetPageAt(pageIndex);
            if (page != null)
                SyncPageBounds(page);
        }
    }

    private void SyncPageBounds(ElementBase page)
    {
        var viewport = DisplayRectangle;
        if (page.Bounds != viewport)
            page.Arrange(viewport);
    }

    private void ReleaseTransitionSnapshots()
    {
        _transitionFromSnapshot?.Dispose();
        _transitionFromSnapshot = null;

        _transitionToSnapshot?.Dispose();
        _transitionToSnapshot = null;
    }

    private static double ValidateIncrement(double value)
    {
        return value <= 0 ? 0.01 : value;
    }
}