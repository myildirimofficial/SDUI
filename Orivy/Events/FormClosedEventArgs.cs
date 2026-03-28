using System;

namespace Orivy;

/// <summary>
/// Provides data for the event that occurs when a form is closed.
/// </summary>
/// <remarks>The FormClosedEventArgs class supplies information about the reason a form was closed, which can be
/// used in event handlers to determine appropriate actions after the form has closed.</remarks>
public class FormClosedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the FormClosedEventArgs class with the specified reason for the form closure.
    /// </summary>
    /// <param name="closeReason">The reason why the form was closed. This value indicates whether the closure was user-initiated, caused by the
    /// application, or due to another event.</param>
    public FormClosedEventArgs(CloseReason closeReason)
    {
        CloseReason = closeReason;
    }

    /// <summary>
    /// Gets the reason why the connection was closed.
    /// </summary>
    public CloseReason CloseReason { get; }
}
