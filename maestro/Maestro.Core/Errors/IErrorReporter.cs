namespace Maestro.Core.Errors;

/// <summary>
/// Where errors go that have no caller to throw to (background tasks), and where the UI layer
/// sends the errors it catches. The implementation turns them into a <see cref="UserError"/> and shows it.
/// </summary>
public interface IErrorReporter
{
    /// <param name="exception">The error. <see cref="OperationCanceledException"/>s are ignored.</param>
    /// <param name="severity">How the error is shown.</param>
    /// <param name="retry">Runs the failed action again. Enables a Retry button in the UI.</param>
    void Report(Exception exception, ErrorSeverity severity = ErrorSeverity.Error, Func<Task>? retry = null);
}
