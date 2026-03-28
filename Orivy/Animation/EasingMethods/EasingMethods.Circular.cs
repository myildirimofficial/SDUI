using System;

namespace Orivy.Animation;

public static partial class EasingMethods
{
    private static readonly EasingMethod circularEaseInOut = Chain(CircularEaseIn, CircularEaseOut);

    /// <summary>
    ///     <para>An easing method that accelerates from 0 to infinite velocity.</para>
    ///     <para>Function: f(p) = -(sqrt(1 - p ^ 2) - 1)</para>
    ///     <para>Derivative: f'(p) = p / sqrt(1 - p ^ 2)</para>
    /// </summary>
    /// <param name="progress">The time progress of the animation.</param>
    /// <returns>The value progress of the animation.</returns>
    public static double CircularEaseIn(double progress)
    {
        return progress <= 0 ? 0 : progress >= 1 ? 1 : -(Math.Sqrt(1 - progress * progress) - 1);
    }

    /// <summary>
    ///     <para>An easing method that decelerates from infinite to a velocity of 0.</para>
    ///     <para>Function: f(p) = sqrt(1 - (p - 1) ^ 2)</para>
    ///     <para>Derivative: f'(p) = (1 - p) / sqrt(-(p - 2) * p)</para>
    /// </summary>
    /// <param name="progress">The time progress of the animation.</param>
    /// <returns>The value progress of the animation.</returns>
    public static double CircularEaseOut(double progress)
    {
        return progress <= 0 ? 0 : progress >= 1 ? 1 : Math.Sqrt(1 - Math.Pow(progress - 1, 2));
    }

    /// <summary>
    ///     <para>
    ///         A combination of the <see cref="EasingMethods.CircularEaseIn" /> and
    ///         <see cref="EasingMethods.CircularEaseOut" /> methods.
    ///     </para>
    ///     <para>It accelerates from 0 to infinite velocity and then decelerates back to a velocity of 0.</para>
    /// </summary>
    /// <param name="progress">The time progress of the animation.</param>
    /// <returns>The value progress of the animation.</returns>
    public static double CircularEaseInOut(double progress)
    {
        return circularEaseInOut(progress);
    }
}