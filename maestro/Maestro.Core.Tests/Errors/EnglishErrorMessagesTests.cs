using Maestro.Core.Errors;

namespace Maestro.Core.Tests.Errors;

public class EnglishErrorMessagesTests
{
    private readonly EnglishErrorMessages _messages = new();

    public static TheoryData<ErrorCode> AllCodes => new(Enum.GetValues<ErrorCode>());

    // Fails when a code is added without a message.
    [Theory]
    [MemberData(nameof(AllCodes))]
    public void Every_code_has_a_message(ErrorCode code)
    {
        Assert.True(EnglishErrorMessages.Templates.ContainsKey(code), $"No English message for {code}.");
        Assert.False(string.IsNullOrWhiteSpace(EnglishErrorMessages.Templates[code]));
    }

    [Fact]
    public void Fills_in_placeholders()
    {
        var text = _messages.GetMessage(ErrorCode.FileNotFound, MaestroException.Arg("path", "/music/song.mp3"));

        Assert.Equal("The file \"/music/song.mp3\" could not be found.", text);
    }

    [Fact]
    public void Leaves_missing_placeholders_as_they_are()
    {
        var text = _messages.GetMessage(ErrorCode.FileNotFound, new Dictionary<string, object?>());

        Assert.Equal("The file \"{path}\" could not be found.", text);
    }
}
