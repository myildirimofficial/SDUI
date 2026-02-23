using System;
using SkiaSharp;

namespace SDUI.Helpers;

/// <summary>
/// Produces an <see cref="SKPaint"/> configured with a tiled texture representing
/// the requested <see cref="HatchStyle"/>.  The brush is disposable because
/// it owns a bitmap and shader.
/// </summary>
public sealed class HatchBrush : IDisposable
{
    private const int PatternSize = 8;

    private SKBitmap? _bitmap;
    private SKShader? _shader;

    public HatchStyle Style { get; }
    public SKColor Foreground { get; }
    public SKColor Background { get; }

    public HatchBrush(HatchStyle style, SKColor foreground, SKColor background)
    {
        Style = style;
        Foreground = foreground;
        Background = background;
        GeneratePattern();
    }

    private void GeneratePattern()
    {
        _bitmap = new SKBitmap(PatternSize, PatternSize, true);
        using var canvas = new SKCanvas(_bitmap);
        canvas.Clear(Background);

        using var paint = new SKPaint
        {
            Color = Foreground,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
        };

        switch (Style)
        {
            case HatchStyle.Horizontal:
                canvas.DrawLine(0, PatternSize / 2, PatternSize, PatternSize / 2, paint);
                break;
            case HatchStyle.Vertical:
                canvas.DrawLine(PatternSize / 2, 0, PatternSize / 2, PatternSize, paint);
                break;
            case HatchStyle.ForwardDiagonal:
                canvas.DrawLine(0, PatternSize, PatternSize, 0, paint);
                break;
            case HatchStyle.BackwardDiagonal:
                canvas.DrawLine(0, 0, PatternSize, PatternSize, paint);
                break;
            case HatchStyle.Cross:
                canvas.DrawLine(0, PatternSize / 2, PatternSize, PatternSize / 2, paint);
                canvas.DrawLine(PatternSize / 2, 0, PatternSize / 2, PatternSize, paint);
                break;
            case HatchStyle.DiagonalCross:
                canvas.DrawLine(0, 0, PatternSize, PatternSize, paint);
                canvas.DrawLine(0, PatternSize, PatternSize, 0, paint);
                break;
            case HatchStyle.Percent05:
                FillPercent(canvas, paint, 0.05f);
                break;
            case HatchStyle.Percent10:
                FillPercent(canvas, paint, 0.10f);
                break;
            case HatchStyle.Percent20:
                FillPercent(canvas, paint, 0.20f);
                break;
            case HatchStyle.Percent25:
                FillPercent(canvas, paint, 0.25f);
                break;
            case HatchStyle.Percent30:
                FillPercent(canvas, paint, 0.30f);
                break;
            case HatchStyle.Percent40:
                FillPercent(canvas, paint, 0.40f);
                break;
            case HatchStyle.Percent50:
                FillPercent(canvas, paint, 0.50f);
                break;
            case HatchStyle.Percent60:
                FillPercent(canvas, paint, 0.60f);
                break;
            case HatchStyle.Percent70:
                FillPercent(canvas, paint, 0.70f);
                break;
            case HatchStyle.Percent75:
                FillPercent(canvas, paint, 0.75f);
                break;
            case HatchStyle.Percent80:
                FillPercent(canvas, paint, 0.80f);
                break;
            case HatchStyle.Percent90:
                FillPercent(canvas, paint, 0.90f);
                break;
            case HatchStyle.LightDownwardDiagonal:
                canvas.DrawLine(0, 1, PatternSize, 1, paint);
                canvas.DrawLine(0, PatternSize - 1, PatternSize, PatternSize - 1, paint);
                break;
            case HatchStyle.LightUpwardDiagonal:
                canvas.DrawLine(0, PatternSize - 1, PatternSize, PatternSize - 1, paint);
                canvas.DrawLine(0, 1, PatternSize, 1, paint);
                break;
            case HatchStyle.DarkDownwardDiagonal:
                canvas.DrawLine(0, 0, PatternSize, PatternSize, paint);
                canvas.DrawLine(0, 2, PatternSize, PatternSize + 2, paint);
                break;
            case HatchStyle.DarkUpwardDiagonal:
                canvas.DrawLine(0, PatternSize, PatternSize, 0, paint);
                canvas.DrawLine(0, PatternSize - 2, PatternSize, -2, paint);
                break;
            case HatchStyle.WideDownwardDiagonal:
                paint.StrokeWidth = 2;
                canvas.DrawLine(0, 0, PatternSize, PatternSize, paint);
                break;
            case HatchStyle.WideUpwardDiagonal:
                paint.StrokeWidth = 2;
                canvas.DrawLine(0, PatternSize, PatternSize, 0, paint);
                break;
            case HatchStyle.LightVertical:
                canvas.DrawLine(PatternSize / 4, 0, PatternSize / 4, PatternSize, paint);
                break;
            case HatchStyle.LightHorizontal:
                canvas.DrawLine(0, PatternSize / 4, PatternSize, PatternSize / 4, paint);
                break;
            case HatchStyle.NarrowVertical:
                canvas.DrawLine(PatternSize / 3, 0, PatternSize / 3, PatternSize, paint);
                canvas.DrawLine(2 * PatternSize / 3, 0, 2 * PatternSize / 3, PatternSize, paint);
                break;
            case HatchStyle.NarrowHorizontal:
                canvas.DrawLine(0, PatternSize / 3, PatternSize, PatternSize / 3, paint);
                canvas.DrawLine(0, 2 * PatternSize / 3, PatternSize, 2 * PatternSize / 3, paint);
                break;
            case HatchStyle.DashedDownwardDiagonal:
                DrawDashedLine(canvas, 0, 0, PatternSize, PatternSize, paint);
                break;
            case HatchStyle.DashedUpwardDiagonal:
                DrawDashedLine(canvas, 0, PatternSize, PatternSize, 0, paint);
                break;
            case HatchStyle.DashedHorizontal:
                DrawDashedLine(canvas, 0, PatternSize / 2, PatternSize, PatternSize / 2, paint);
                break;
            case HatchStyle.DashedVertical:
                DrawDashedLine(canvas, PatternSize / 2, 0, PatternSize / 2, PatternSize, paint);
                break;
            case HatchStyle.SmallConfetti:
            case HatchStyle.LargeConfetti:
                DrawConfetti(canvas, paint, Style == HatchStyle.LargeConfetti);
                break;
            case HatchStyle.ZigZag:
                DrawZigZag(canvas, paint);
                break;
            case HatchStyle.Wave:
                DrawWave(canvas, paint);
                break;
            case HatchStyle.DiagonalBrick:
                DrawBrick(canvas, paint, true);
                break;
            case HatchStyle.HorizontalBrick:
                DrawBrick(canvas, paint, false);
                break;
            case HatchStyle.Weave:
                DrawWeave(canvas, paint);
                break;
            case HatchStyle.Plaid:
                DrawPlaid(canvas, paint);
                break;
            case HatchStyle.Divot:
                DrawDivot(canvas, paint);
                break;
            case HatchStyle.DottedGrid:
                DrawDottedGrid(canvas, paint);
                break;
            case HatchStyle.DottedDiamond:
                DrawDottedDiamond(canvas, paint);
                break;
            case HatchStyle.Shingle:
                DrawShingle(canvas, paint);
                break;
            case HatchStyle.Trellis:
                DrawTrellis(canvas, paint);
                break;
            case HatchStyle.Sphere:
                DrawSphere(canvas, paint);
                break;
            case HatchStyle.SmallGrid:
                DrawSmallGrid(canvas, paint);
                break;
            case HatchStyle.SmallCheckerBoard:
            case HatchStyle.LargeCheckerBoard:
                DrawChecker(canvas, paint, Style == HatchStyle.LargeCheckerBoard);
                break;
            case HatchStyle.OutlinedDiamond:
                DrawOutlinedDiamond(canvas, paint);
                break;
            case HatchStyle.SolidDiamond:
                DrawSolidDiamond(canvas, paint);
                break;
            default:
                canvas.DrawCircle(PatternSize / 2, PatternSize / 2, 1, paint);
                break;
        }

        // create shader once
        _shader = SKShader.CreateBitmap(_bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
    }

    // helper methods for various patterns
    private void FillPercent(SKCanvas canvas, SKPaint paint, float density)
    {
        int total = PatternSize * PatternSize;
        int fillCount = (int)(total * density);
        int x = 0, y = 0;
        for (int i = 0; i < fillCount; i++)
        {
            canvas.DrawPoint(x, y, paint);
            x++;
            if (x >= PatternSize) { x = 0; y++; }
        }
    }

    private void DrawDashedLine(SKCanvas canvas, float x1, float y1, float x2, float y2, SKPaint paint)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var length = (float)Math.Sqrt(dx * dx + dy * dy);
        var dash = 2f;
        var gap = 2f;
        var steps = (int)(length / (dash + gap));
        for (int i = 0; i < steps; i++)
        {
            var start = i * (dash + gap) / length;
            var end = start + dash / length;
            canvas.DrawLine(x1 + dx * start, y1 + dy * start, x1 + dx * end, y1 + dy * end, paint);
        }
    }

    private void DrawConfetti(SKCanvas canvas, SKPaint paint, bool large)
    {
        int count = large ? 6 : 3;
        var rand = new Random(Style.GetHashCode());
        for (int i = 0; i < count; i++)
        {
            var px = rand.Next(PatternSize);
            var py = rand.Next(PatternSize);
            canvas.DrawCircle(px, py, 1, paint);
        }
    }

    private void DrawZigZag(SKCanvas canvas, SKPaint paint)
    {
        var points = new SKPoint[PatternSize];
        for (int i = 0; i < PatternSize; i++)
            points[i] = new SKPoint(i, (i % 2 == 0) ? 0 : PatternSize);
        canvas.DrawPoints(SKPointMode.Polygon, points, paint);
    }

    private void DrawWave(SKCanvas canvas, SKPaint paint)
    {
        for (int x = 0; x < PatternSize; x++)
        {
            float y = (float)(PatternSize / 2 + (Math.Sin(x / (float)PatternSize * Math.PI * 2) * PatternSize / 4));
            canvas.DrawPoint(x, y, paint);
        }
    }

    private void DrawBrick(SKCanvas canvas, SKPaint paint, bool diagonal)
    {
        if (diagonal)
            canvas.DrawLine(0, PatternSize / 2, PatternSize, PatternSize / 2, paint);
        else
            canvas.DrawLine(0, PatternSize / 3, PatternSize, PatternSize / 3, paint);
    }

    private void DrawWeave(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawLine(0, 0, PatternSize, PatternSize, paint);
        canvas.DrawLine(PatternSize, 0, 0, PatternSize, paint);
    }

    private void DrawPlaid(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawLine(0, PatternSize/2, PatternSize, PatternSize/2, paint);
        canvas.DrawLine(PatternSize/2, 0, PatternSize/2, PatternSize, paint);
    }

    private void DrawDivot(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawLine(0, PatternSize/2, PatternSize, PatternSize/2, paint);
    }

    private void DrawDottedGrid(SKCanvas canvas, SKPaint paint)
    {
        for (int x=0;x<PatternSize;x+=2)
            for(int y=0;y<PatternSize;y+=2)
                canvas.DrawPoint(x,y,paint);
    }

    private void DrawDottedDiamond(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawPoint(PatternSize/2,0,paint);
        canvas.DrawPoint(0,PatternSize/2,paint);
        canvas.DrawPoint(PatternSize,PatternSize/2,paint);
        canvas.DrawPoint(PatternSize/2,PatternSize,paint);
    }

    private void DrawShingle(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawRect(new SKRect(0,0,PatternSize/2,PatternSize/2), paint);
    }

    private void DrawTrellis(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawLine(0,0,PatternSize,PatternSize, paint);
        canvas.DrawLine(0,PatternSize,PatternSize,0, paint);
    }

    private void DrawSphere(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawCircle(PatternSize/2,PatternSize/2,PatternSize/4, paint);
    }

    private void DrawSmallGrid(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawLine(0,PatternSize/2,PatternSize,PatternSize/2, paint);
        canvas.DrawLine(PatternSize/2,0,PatternSize/2,PatternSize, paint);
    }

    private void DrawChecker(SKCanvas canvas, SKPaint paint, bool large)
    {
        int step = large ? PatternSize/2 : PatternSize/4;
        for(int x=0;x<PatternSize;x+=step)
            for(int y=0;y<PatternSize;y+=step)
                if(((x+y)/step)%2==0)
                    canvas.DrawRect(new SKRect(x,y,x+step,y+step), paint);
    }

    private void DrawOutlinedDiamond(SKCanvas canvas, SKPaint paint)
    {
        canvas.DrawLine(PatternSize/2,0,PatternSize,PatternSize/2,paint);
        canvas.DrawLine(PatternSize,PatternSize/2,PatternSize/2,PatternSize,paint);
        canvas.DrawLine(PatternSize/2,PatternSize,0,PatternSize/2,paint);
        canvas.DrawLine(0,PatternSize/2,PatternSize/2,0,paint);
    }

    private void DrawSolidDiamond(SKCanvas canvas, SKPaint paint)
    {
        DrawOutlinedDiamond(canvas, paint);
        var path = new SKPath { FillType = SKPathFillType.Winding };
        path.MoveTo(PatternSize / 2, 0);
        path.LineTo(PatternSize, PatternSize / 2);
        path.LineTo(PatternSize / 2, PatternSize);
        path.LineTo(0, PatternSize / 2);
        path.Close();
        canvas.DrawPath(path, paint);
        path.Dispose();
    }

    public SKPaint CreatePaint()
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = _shader,
        };
        return paint;
    }

    public void Dispose()
    {
        _shader?.Dispose();
        _shader = null;
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
