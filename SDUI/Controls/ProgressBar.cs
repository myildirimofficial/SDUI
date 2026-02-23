
using System.Drawing.Drawing2D;
using SDUI.Extensions;
using SDUI.Helpers;
using SkiaSharp;

namespace SDUI.Controls;

public class ProgressBar : UIElementBase
{
    private bool _drawHatch;

    private SKColor[] _gradient = new SKColor[2];

    private HatchStyle _hatchType = HatchStyle.Percent10;

    private long _maximum = 100;

    private float _maxPercentShowValue = 100;

    private int _percentIndices = 2;

    private int _radius = 4;

    private bool _showAsPercent;

    private bool _showValue;
    private long _value;

    private int RadiusScaled => (int)(_radius * ScaleFactor);

    public ProgressBar()
    {
        BackColor = SKColors.Transparent;
        // Modern gradient with Primary color
        _gradient = new[] { ColorScheme.Primary, ColorScheme.PrimaryContainer };
    }

    public long Value
    {
        get => _value;
        set
        {
            _value = value;
            Invalidate();
        }
    }

    public long Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value <= 0 ? 1 : value;
            Invalidate();
        }
    }

    public SKColor[] Gradient
    {
        get => _gradient;
        set
        {
            _gradient = value;
            Invalidate();
        }
    }

    public bool ShowAsPercent
    {
        get => _showAsPercent;
        set
        {
            _showAsPercent = value;
            Invalidate();
        }
    }

    public int PercentIndices
    {
        get => _percentIndices;
        set
        {
            _percentIndices = value;
            Invalidate();
        }
    }

    public float MaxPercentShowValue
    {
        get => _maxPercentShowValue;
        set
        {
            _maxPercentShowValue = value;
            Invalidate();
        }
    }

    public bool ShowValue
    {
        get => _showValue;
        set
        {
            _showValue = value;
            Invalidate();
        }
    }

    public int Radius
    {
        get => _radius;
        set
        {
            _radius = value <= 0 ? 1 : value;
            Invalidate();
        }
    }

    public bool DrawHatch
    {
        get => _drawHatch;
        set
        {
            _drawHatch = value;
            Invalidate();
        }
    }

    public HatchStyle HatchType
    {
        get => _hatchType;
        set
        {
            _hatchType = HatchStyle.Percent10;
            Invalidate();
        }
    }

    public override void OnPaint(SKCanvas canvas)
    {
        base.OnPaint(canvas);

        var intValue = _value / (float)_maximum * Width;
        var percent = 100.0f * Value / Maximum;

        var rect = new SkiaSharp.SKRect(0, 0, Width, Height);

        // Modern background with SurfaceContainerHigh
        using (var paint = new SKPaint
               {
                   Color = ColorScheme.SurfaceContainerHigh,
                   IsAntialias = true,
                   Style = SKPaintStyle.Fill
               })
        {
            canvas.DrawRoundRect(rect, _radius, _radius, paint);
        }

        // İlerleme çubuğu çizimi
        if (intValue > 0)
        {
            var progressRect = new SkiaSharp.SKRect(0, 0, intValue, Height);

            // Gradient oluştur
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, Height),
                new[] { _gradient[0], _gradient[1] },
                null,
                SKShaderTileMode.Clamp);

            using (var paint = new SKPaint
                   {
                       Shader = shader,
                       IsAntialias = true,
                       Style = SKPaintStyle.Fill
                   })
            {
                canvas.DrawRoundRect(progressRect, _radius, _radius, paint);
            }

            // Hatch pattern çizimi
            if (_drawHatch)
            {
                using var brush = new HatchBrush(_hatchType, SKColors.White.WithAlpha(50), SKColors.Transparent);
                using var hatchPaint = brush.CreatePaint();
                canvas.DrawRoundRect(progressRect, _radius, _radius, hatchPaint);
            }
        }

        // Modern subtle border with Outline color
        using (var paint = new SKPaint
               {
                   Color = ColorScheme.OutlineVariant,
                   IsAntialias = true,
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = 1
               })
        {
            canvas.DrawRoundRect(rect, _radius, _radius, paint);
        }

        // Değer metni çizimi
        if (ShowValue)
        {
            string text;
            if (_showAsPercent)
            {
                if (percent == 100)
                    percent = _maxPercentShowValue;

                text = percent.ToString($"0.{"0".PadRight(_percentIndices, '0')}") + "%";
            }
            else
            {
                text = $"{_value} / {_maximum}";
            }

            var textColor = percent > 50 ? SKColors.White : ColorScheme.ForeColor;
            var shadowColor = percent > 50 ? SKColors.Black : ColorScheme.ForeColor.Determine();

            // Gölge metni
            using (var paint = new SKPaint
                   {
                       Color = shadowColor,
                       IsAntialias = true
                   })
            using (var font = new SKFont
                   {
                       Size = Font.Size.Topx(this),
                       Typeface = FontManager.GetSKTypeface(Font)
                   })
            {
                var textY = Height / 2f - (font.Metrics.Ascent + font.Metrics.Descent) / 2f;
                TextRenderingHelper.DrawText(canvas, text, Width / 2f + 1, textY + 1, SKTextAlign.Center, font, paint);
            }

            // Ana metin
            using (var paint = new SKPaint
                   {
                       Color = textColor,
                       IsAntialias = true
                   })
            using (var font = new SKFont
                   {
                       Size = Font.Size.Topx(this),
                       Typeface = FontManager.GetSKTypeface(Font)
                   })
            {
                var textY = Height / 2f - (font.Metrics.Ascent + font.Metrics.Descent) / 2f;
                TextRenderingHelper.DrawText(canvas, text, Width / 2f, textY, SKTextAlign.Center, font, paint);
            }
        }
    }
}