namespace Maestro.Core.Errors;

/// <summary>
/// Default <see cref="IErrorReporter"/>. Keeps the error currently shown to the user and raises
/// <see cref="CurrentChanged"/> when it changes.
/// </summary>
public sealed class ErrorReporter(IErrorMessageProvider messages, TimeProvider? time = null) : IErrorReporter
{
    // Same error code reported again within this time is not shown again
    internal static readonly TimeSpan RepeatWindow = TimeSpan.FromSeconds(5);

    private static readonly IReadOnlyDictionary<string, object?> NoArgs = new Dictionary<string, object?>();

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly object _lock = new();
    private UserError? _current;

    // Raised with the new current error, or null when it was dismissed. Can be raised on any thread
    public event Action<UserError?>? CurrentChanged;

    // The error currently shown to the user, or null
    public UserError? Current
    {
        get { lock (_lock) return _current; }
    }

    public void Report(Exception exception, ErrorSeverity severity = ErrorSeverity.Error, Func<Task>? retry = null)
    {
        // Unobserved task exceptions arrive wrapped.
        if (exception is AggregateException { InnerExceptions.Count: 1 } aggregate)
            exception = aggregate.InnerExceptions[0];

        if (exception is OperationCanceledException)
            return;

        var (code, args) = exception is MaestroException m ? (m.Code, m.Args) : (ErrorCode.Unknown, NoArgs);

        // TODO: replace with ILogger once logging is added.
        Console.Error.WriteLine($"[Maestro] {severity} {code}: {exception}");

        var now = _time.GetUtcNow();
        UserError error;
        lock (_lock)
        {
            // A fatal error stays until it is retried; nothing else may hide it.
            if (_current is { Severity: ErrorSeverity.Fatal } && severity != ErrorSeverity.Fatal)
                return;

            // The same error again shortly after (e.g. a flapping connection) would only flood the UI.
            if (_current is not null && _current.Code == code && now - _current.At < RepeatWindow)
                return;

            error = new UserError(code, severity, messages.GetMessage(code, args), now, retry);
            _current = error;
        }

        CurrentChanged?.Invoke(error);
    }

    /// <summary>Hides <paramref name="error"/>, unless a newer error has replaced it in the meantime.</summary>
    public void Dismiss(UserError error)
    {
        lock (_lock)
        {
            if (!ReferenceEquals(_current, error))
                return;
            _current = null;
        }

        CurrentChanged?.Invoke(null);
    }
}
