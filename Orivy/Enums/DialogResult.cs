namespace Orivy;

public enum DialogResult
{
    /// <summary>
    ///  Nothing is returned from the dialog box. This means that the modal dialog continues running.
    /// </summary>
    None = 0,

    /// <summary>
    ///  The dialog box return value is OK (usually sent from a button labeled OK).
    /// </summary>
    OK,

    /// <summary>
    ///  The dialog box return value is Cancel (usually sent from a button labeled Cancel).
    /// </summary>
    Cancel,

    /// <summary>
    ///  The dialog box return value is Abort (usually sent from a button labeled Abort).
    /// </summary>
    Abort,

    /// <summary>
    ///  The dialog box return value is Retry (usually sent from a button labeled Retry).
    /// </summary>
    Retry,

    /// <summary>
    ///  The dialog box return value is Ignore (usually sent from a button labeled Ignore).
    /// </summary>
    Ignore,

    /// <summary>
    ///  The dialog box return value is Yes (usually sent from a button labeled Yes).
    /// </summary>
    Yes,

    /// <summary>
    ///  The dialog box return value is No (usually sent from a button labeled No).
    /// </summary>
    No,

    /// <summary>
    ///  The dialog box return value is Try Again (usually sent from a button labeled Try Again).
    /// </summary>
    TryAgain = 10,

    /// <summary>
    ///  The dialog box return value is Continue (usually sent from a button labeled Continue).
    /// </summary>
    Continue = 11,
}
