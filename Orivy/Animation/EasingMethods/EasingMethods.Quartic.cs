using System;

namespace Orivy.Animation;

public static partial class EasingMethods
{
    private static readonly EasingMethod quarticEaseInOut = Chain(QuarticEaseIn, QuarticEaseOut);

    /// <summary>
    ///     <para>An easing method that accelerates from 0 to a velocity of 4.</para>
    ///     <para>Function: f(p) = p ^ 4</para>
    ///     <para>Derivative: f'(p) = 4 * p ^ 3</para>
    /// </summary>
    /// <param name="progress">The time progress of the animation.</param>
    /// <returns>The value progress of the animation.</returns>
    public static double QuarticEaseIn(double progress)
    {
        return progress <= 0 ? 0 : progress >= 1 ? 1 : Math.Pow(progress, 4);
    }

    /// <summary>
    ///     <para>An easing method that decelerates from a velocity of 4 to 0.</para>
    ///     <para>Function: f(p) = -((p - 1) ^ 4 - 1)</para>
    ///     <para>Derivative: f'(p) = -4 * (p - 1) ^ 3</para>
    /// </summary>
    /// <param name="progress">The time progress of the animation.</param>
    /// <returns>The value progress of the animation.</returns>
    public static double QuarticEaseOut(double progress)
    {
        return progress <= 0 ? 0 : progress >= 1 ? 1 : -(Math.Pow(progress - 1, 4) - 1);
    }

    /// <summary>
    ///     <para>
    ///         A combination of the <see cref="EasingMethods.QuarticEaseIn" /> and
    ///         <see cref="EasingMethods.QuarticEaseOut" /> methods.
    ///     </para>
    ///     <para>It accelerates from 0 to a velocity of 4 and then decelerates back to a velocity of 0.</para>
    /// </summary>
    /// <param name="progress">The time progress of the animation.</param>
    /// <returns>The value progress of the animation.</returns>
    public static double QuarticEaseInOut(double progress)
    {
        return quarticEaseInOut(progress);
    }
}