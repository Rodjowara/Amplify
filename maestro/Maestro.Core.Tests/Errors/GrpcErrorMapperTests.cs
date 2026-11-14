using Grpc.Core;
using Maestro.Core.Errors;

namespace Maestro.Core.Tests.Errors;

public class GrpcErrorMapperTests
{
    [Theory]
    [InlineData(StatusCode.Unavailable, ErrorCode.AmpUnreachable)]
    [InlineData(StatusCode.DeadlineExceeded, ErrorCode.AmpTimeout)]
    [InlineData(StatusCode.InvalidArgument, ErrorCode.AmpRejectedRequest)]
    [InlineData(StatusCode.FailedPrecondition, ErrorCode.AmpRejectedRequest)]
    [InlineData(StatusCode.OutOfRange, ErrorCode.AmpRejectedRequest)]
    [InlineData(StatusCode.Internal, ErrorCode.Unknown)]
    public void Maps_status_codes(StatusCode status, ErrorCode expected)
    {
        var rpc = new RpcException(new Status(status, "detail"));

        var mapped = GrpcErrorMapper.Map(rpc, "http://amp:50051");

        Assert.Equal(expected, mapped.Code);
        Assert.Equal("http://amp:50051", mapped.Args["amp"]);
        Assert.Equal("detail", mapped.Args["detail"]);
        Assert.Same(rpc, mapped.InnerException);
    }
}
