namespace Maestro.Core.Errors;

/// <summary>
/// Where errors go that have no caller to throw to (background tasks),
/// and where the UI layer sends the errors it catches.
/// </summary>
public interface IErrorReporter
{
    void Report(Exception exception, ErrorSeverity severity = ErrorSeverity.Error);
}
