using Grpc.Core;
using Maestro.Core.Errors;

namespace Maestro.Core.Tests.Errors;

public class MaestroExceptionTests
{
    [Theory]
    [InlineData(StatusCode.Unavailable, ErrorCode.AMP_Unreachable)]
    [InlineData(StatusCode.DeadlineExceeded, ErrorCode.AMP_Timeout)]
    [InlineData(StatusCode.Internal, ErrorCode.Unknown)]
    public void Constructor_RpcException_MapsStatusToErrorCode(StatusCode status, ErrorCode expected)
    {
        var exception = new MaestroException(new RpcException(new Status(status, "")));

        Assert.Equal(expected, exception.Code);
    }

    [Fact]
    public void Constructor_AnonymousArguments_BecomeStringDictionary()
    {
        var exception = new MaestroException(ErrorCode.Unknown, new { amp = "10.0.0.1", port = 50051, message = (string?)null });

        Assert.Equal("10.0.0.1", exception.Arguments["amp"]);
        Assert.Equal("50051", exception.Arguments["port"]);
        Assert.Equal("", exception.Arguments["message"]);
    }

    [Fact]
    public void Constructor_NoArguments_HasEmptyArguments()
    {
        var exception = new MaestroException(ErrorCode.Unknown);

        Assert.Empty(exception.Arguments);
    }

    [Fact]
    public void Constructor_PlainException_IsUnknownWithMessage()
    {
        var inner = new InvalidOperationException("boom");

        var exception = new MaestroException(inner);

        Assert.Equal(ErrorCode.Unknown, exception.Code);
        Assert.Equal("boom", exception.Arguments["message"]);
        Assert.Same(inner, exception.InnerException);
    }
}