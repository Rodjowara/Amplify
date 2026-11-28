using Maestro.Core.Errors;

namespace Maestro.Core.Tests.Errors;

public class ErrorReporterTests
{
    private static readonly TimeSpan ReportTimeout = TimeSpan.FromSeconds(5);
    private readonly ErrorReporter _reporter = new();
    private readonly List<MaestroException> _reported = [];

    public ErrorReporterTests() => _reporter.Reported += _reported.Add;

    [Fact]
    public void Report_Cancellation_IsIgnored()
    {
        _reporter.Report(new OperationCanceledException());
        _reporter.Report(new TaskCanceledException());

        Assert.Empty(_reported);
    }

    [Fact]
    public void Report_PlainException_IsWrappedAsUnknown()
    {
        _reporter.Report(new InvalidOperationException("boom"));

        var reported = Assert.Single(_reported);
        Assert.Equal(ErrorCode.Unknown, reported.Code);
    }

    [Fact]
    public void Report_MaestroException_IsPassedThrough()
    {
        var exception = new MaestroException(ErrorCode.FileNotFound);

        _reporter.Report(exception);

        Assert.Same(exception, Assert.Single(_reported));
    }

    [Fact]
    public async Task Run_AsyncActionThrows_ReportsException()
    {
        var reported = new TaskCompletionSource<MaestroException>();
        _reporter.Reported += exception => reported.TrySetResult(exception);
        var exception = new MaestroException(ErrorCode.StreamInterrupted);

        _reporter.Run(async () =>
        {
            await Task.Yield();
            throw exception;
        });

        Assert.Same(exception, await reported.Task.WaitAsync(ReportTimeout, TestContext.Current.CancellationToken));
    }
}