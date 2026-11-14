using Maestro.Core.Errors;

namespace Maestro.Core.Tests.Errors;

public class ErrorReporterTests
{
    private readonly ManualTimeProvider _time = new();
    private readonly ErrorReporter _reporter;

    public ErrorReporterTests() => _reporter = new ErrorReporter(new EnglishErrorMessages(), _time);

    private static MaestroException Error(ErrorCode code) => new(code, "technical");

    [Fact]
    public void Maestro_exception_uses_its_code_and_args()
    {
        _reporter.Report(new MaestroException(ErrorCode.FileNotFound, "technical", MaestroException.Arg("path", "a.mp3")));

        Assert.Equal(ErrorCode.FileNotFound, _reporter.Current?.Code);
        Assert.Equal("The file \"a.mp3\" could not be found.", _reporter.Current?.Message);
    }

    [Fact]
    public void Other_exceptions_become_unknown_and_never_show_their_message()
    {
        _reporter.Report(new InvalidOperationException("secret internal detail"));

        Assert.Equal(ErrorCode.Unknown, _reporter.Current?.Code);
        Assert.DoesNotContain("secret", _reporter.Current?.Message);
    }

    [Fact]
    public void Cancellation_is_ignored()
    {
        _reporter.Report(new TaskCanceledException());

        Assert.Null(_reporter.Current);
    }

    [Fact]
    public void Single_aggregate_exception_is_unwrapped()
    {
        _reporter.Report(new AggregateException(Error(ErrorCode.AmpTimeout)));

        Assert.Equal(ErrorCode.AmpTimeout, _reporter.Current?.Code);
    }

    [Fact]
    public void Same_code_shortly_after_is_not_shown_again()
    {
        _reporter.Report(Error(ErrorCode.StreamInterrupted));
        var first = _reporter.Current;

        _time.Advance(ErrorReporter.RepeatWindow / 2);
        _reporter.Report(Error(ErrorCode.StreamInterrupted));

        Assert.Same(first, _reporter.Current);
    }

    [Fact]
    public void Same_code_after_the_repeat_window_is_shown_again()
    {
        _reporter.Report(Error(ErrorCode.StreamInterrupted));
        var first = _reporter.Current;

        _time.Advance(ErrorReporter.RepeatWindow + TimeSpan.FromSeconds(1));
        _reporter.Report(Error(ErrorCode.StreamInterrupted));

        Assert.NotSame(first, _reporter.Current);
    }

    [Fact]
    public void Same_code_after_dismiss_is_shown_again()
    {
        _reporter.Report(Error(ErrorCode.FileNotFound));
        _reporter.Dismiss(_reporter.Current!);

        _reporter.Report(Error(ErrorCode.FileNotFound));

        Assert.Equal(ErrorCode.FileNotFound, _reporter.Current?.Code);
    }

    [Fact]
    public void Fatal_error_is_not_replaced_by_a_lower_severity()
    {
        _reporter.Report(Error(ErrorCode.DockerNotAvailable), ErrorSeverity.Fatal);

        _reporter.Report(Error(ErrorCode.AmpUnreachable));

        Assert.Equal(ErrorCode.DockerNotAvailable, _reporter.Current?.Code);
    }

    [Fact]
    public void Dismissing_an_old_error_keeps_a_newer_one()
    {
        _reporter.Report(Error(ErrorCode.FileNotFound));
        var old = _reporter.Current!;
        _reporter.Report(Error(ErrorCode.AmpUnreachable));

        _reporter.Dismiss(old);

        Assert.Equal(ErrorCode.AmpUnreachable, _reporter.Current?.Code);
    }

    [Fact]
    public void Raises_current_changed_on_report_and_dismiss()
    {
        var seen = new List<UserError?>();
        _reporter.CurrentChanged += seen.Add;

        _reporter.Report(Error(ErrorCode.FileNotFound));
        _reporter.Dismiss(_reporter.Current!);

        Assert.Equal(2, seen.Count);
        Assert.Equal(ErrorCode.FileNotFound, seen[0]?.Code);
        Assert.Null(seen[1]);
    }
}
