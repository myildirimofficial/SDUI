using SkiaSharp;
using System;


namespace Orivy.Animation;

public static class ValueFactories
{
    #region Integer Types

    public static byte ByteFactory(byte startValue, byte targetValue, double progress)
    {
        return (byte)(startValue + (targetValue - startValue) * progress);
    }

    public static sbyte SByteFactory(sbyte startValue, sbyte targetValue, double progress)
    {
        return (sbyte)(startValue + (targetValue - startValue) * progress);
    }

    public static short ShortFactory(short startValue, short targetValue, double progress)
    {
        return (short)(startValue + (targetValue - startValue) * progress);
    }

    public static int IntegerFactory(int startValue, int targetValue, double progress)
    {
        return (int)(startValue + (targetValue - startValue) * progress);
    }

    public static long LongFactory(long startValue, long targetValue, double progress)
    {
        return (long)(startValue + (targetValue - startValue) * progress);
    }

    public static ushort UnsignedShortFactory(ushort startValue, ushort targetValue, double progress)
    {
        return (ushort)(startValue + (targetValue - startValue) * progress);
    }

    public static uint UnsignedIntegerFactory(uint startValue, uint targetValue, double progress)
    {
        return (uint)(startValue + (targetValue - startValue) * progress);
    }

    public static ulong UnsignedLongFactory(ulong startValue, ulong targetValue, double progress)
    {
        return (ulong)(startValue + (targetValue - startValue) * progress);
    }

    #endregion

    #region Floating SKPoint Types

    public static float FloatFactory(float startValue, float targetValue, double progress)
    {
        return (float)(startValue + (targetValue - startValue) * progress);
    }

    public static double DoubleFactory(double startValue, double targetValue, double progress)
    {
        return startValue + (targetValue - startValue) * progress;
    }

    #endregion

    #region Drawing

    public static SKPoint PointFactory(SKPoint startValue, SKPoint targetValue, double progress)
    {
        return new SKPoint(FloatFactory(startValue.X, targetValue.X, progress),
            FloatFactory(startValue.Y, targetValue.Y, progress));
    }

    public static SKPoint PointFFactory(SKPoint startValue, SKPoint targetValue, double progress)
    {
        return new SKPoint(FloatFactory(startValue.X, targetValue.X, progress),
            FloatFactory(startValue.Y, targetValue.Y, progress));
    }

    public static SKSize SizeFactory(SKSize startValue, SKSize targetValue, double progress)
    {
        return new SKSize(FloatFactory(startValue.Width, targetValue.Width, progress),
            FloatFactory(startValue.Height, targetValue.Height, progress));
    }

    public static SKSize SizeFFactory(SKSize startValue, SKSize targetValue, double progress)
    {
        return new SKSize(FloatFactory(startValue.Width, targetValue.Width, progress),
            FloatFactory(startValue.Height, targetValue.Height, progress));
    }

    public static SKRect RectangleFactory(SKRect startValue, SKRect targetValue, double progress)
    {
        return SKRect.Create(
            FloatFactory(startValue.Location.X, targetValue.Location.X, progress),
            FloatFactory(startValue.Location.Y, targetValue.Location.Y, progress),
            FloatFactory(startValue.Width, targetValue.Width, progress),
            FloatFactory(startValue.Height, targetValue.Height, progress));
    }

    public static SKRect RectangleFFactory(SKRect startValue, SKRect targetValue, double progress)
    {
        return SKRect.Create(
            FloatFactory(startValue.Location.X, targetValue.Location.X, progress),
            FloatFactory(startValue.Location.Y, targetValue.Location.Y, progress),
            FloatFactory(startValue.Width, targetValue.Width, progress),
            FloatFactory(startValue.Height, targetValue.Height, progress));
    }

    public static SKColor ColorRgbFactory(SKColor startValue, SKColor targetValue, double progress)
    {
        return new SKColor(
            ByteFactory(startValue.Red, targetValue.Red, progress),
            ByteFactory(startValue.Green, targetValue.Green, progress),
            ByteFactory(startValue.Blue, targetValue.Blue, progress),
            ByteFactory(startValue.Alpha, targetValue.Alpha, progress));
    }

    #endregion

    #region Other

    public static bool BooleanFactory(bool startValue, bool targetValue, double progress)
    {
        return progress < .5 ? startValue : targetValue;
    }

    public static decimal DecimalFactory(decimal startValue, decimal targetValue, double progress)
    {
        return startValue + (targetValue - startValue) * (decimal)progress;
    }

    public static TimeSpan TimeSpanFactory(TimeSpan startValue, TimeSpan targetValue, double progress)
    {
        return startValue.Add(TimeSpan.FromMilliseconds(targetValue.Subtract(startValue).TotalMilliseconds * progress));
    }

    #endregion
}
