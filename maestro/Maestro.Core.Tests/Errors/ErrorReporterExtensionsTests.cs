using Maestro.Core.Errors;

namespace Maestro.Core.Tests.Errors;

public class ErrorReporterExtensionsTests
{
    private readonly ErrorReporter _reporter = new(new EnglishErrorMessages());

    [Fact]
    public async Task Success_reports_nothing()
    {
        await _reporter.RunAsync(() => Task.CompletedTask);

        Assert.Null(_reporter.Current);
    }

    [Fact]
    public async Task Failure_is_reported_with_the_given_severity()
    {
        await _reporter.RunAsync(
            () => throw new MaestroException(ErrorCode.AmpTimeout, "technical"),
            ErrorSeverity.Warning);

        Assert.Equal(ErrorCode.AmpTimeout, _reporter.Current?.Code);
        Assert.Equal(ErrorSeverity.Warning, _reporter.Current?.Severity);
        Assert.Null(_reporter.Current?.Retry);
    }

    [Fact]
    public async Task Retry_runs_the_action_again()
    {
        var calls = 0;
        Task Action()
        {
            calls++;
            return calls == 1
                ? throw new MaestroException(ErrorCode.DockerNotAvailable, "technical")
                : Task.CompletedTask;
        }

        await _reporter.RunAsync(Action, ErrorSeverity.Fatal, retryable: true);
        var error = _reporter.Current!;
        _reporter.Dismiss(error);
        await error.Retry!();

        Assert.Equal(2, calls);
        Assert.Null(_reporter.Current);
    }

    [Fact]
    public async Task Cancellation_is_not_reported()
    {
        await _reporter.RunAsync(() => throw new OperationCanceledException());

        Assert.Null(_reporter.Current);
    }
}
