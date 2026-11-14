namespace Maestro.Core.Errors;

public static class ErrorReporterExtensions
{
    /// <summary>
    /// Starts <paramref name="action"/> without waiting for it, and reports it if it fails.
    /// Use this for everything the UI starts (button clicks etc.) instead of <c>_ = SomethingAsync()</c>,
    /// so no exception gets lost and ViewModels don't need their own try/catch.
    /// </summary>
    /// <param name="reporter">Where a failure is reported.</param>
    /// <param name="action">The work to do.</param>
    /// <param name="severity">How a failure is shown.</param>
    /// <param name="retryable">If true, the error offers a Retry that runs <paramref name="action"/> again.</param>
    public static void Run(
        this IErrorReporter reporter,
        Func<Task> action,
        ErrorSeverity severity = ErrorSeverity.Error,
        bool retryable = false)
        => _ = RunAsync(reporter, action, severity, retryable);

    /// <summary>Like <see cref="Run"/>, but can be awaited. Never throws.</summary>
    public static async Task RunAsync(
        this IErrorReporter reporter,
        Func<Task> action,
        ErrorSeverity severity = ErrorSeverity.Error,
        bool retryable = false)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Func<Task>? retry = retryable ? () => RunAsync(reporter, action, severity, retryable) : null;
            reporter.Report(ex, severity, retry);
        }
    }
}
