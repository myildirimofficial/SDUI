namespace Orivy;

public class FormClosingEventArgs : FormClosedEventArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether the current operation should be canceled.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Initializes a new instance of the FormClosingEventArgs class with the specified reason for the form closure.
    /// </summary>
    /// <param name="closeReason">The reason that the form is being closed. This value determines the cause of the form closure, such as user
    /// action or system shutdown.</param>
    public FormClosingEventArgs(CloseReason closeReason) 
        : base(closeReason)
    {
    }
}
