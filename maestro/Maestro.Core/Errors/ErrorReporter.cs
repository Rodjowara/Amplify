namespace Maestro.Core.Errors;

public sealed class ErrorReporter
{
    public event Action<MaestroException>? Reported;
    public event Action<MaestroException>? ErrorPageRequested;

    public void Report(Exception exception)
    {
        if (exception is not OperationCanceledException)
            Reported?.Invoke(ToMaestroException(exception));
    }

    private static MaestroException ToMaestroException(Exception exception)
    {
        Console.Error.WriteLine(exception);
        return exception as MaestroException ?? new MaestroException(exception);
    }

    public void ShowErrorPage(Exception exception) => ErrorPageRequested?.Invoke(ToMaestroException(exception));

// Convenience method to use instead of try/catch blocks in async methods
    public void Run(Func<Task> action) => _ = RunAsync(action);

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            Report(exception);
        }
    }
}
