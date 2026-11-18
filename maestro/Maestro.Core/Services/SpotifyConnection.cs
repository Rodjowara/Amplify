using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace Maestro.Core.Services;

public class SpotifyConnectionOptions
{
    public string ClientId { get; set; } = "todo";
    public int RedirectPort => RedirectUri.Port;
    public Uri RedirectUri { get; set; } = new("http://127.0.0.1:5543/callback");
}

// DOCS: https://johnnycrazy.github.io/SpotifyAPI-NET/
public class SpotifyConnection
{
    private SpotifyConnectionOptions _options = new();
    private SpotifyClient? _client = null;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        var (verified, challenge) = PKCEUtil.GenerateCodes();
        var code = await GetSpotifyCodeAsync(challenge, ct);
        var request = new PKCETokenRequest(_options.ClientId, code, _options.RedirectUri, verified);
        var token = await new OAuthClient().RequestToken(request);
        var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new PKCEAuthenticator(_options.ClientId, token));

        _client = new SpotifyClient(config);
    }

    public async Task TestAsync()
    {
        var profile = await _client!.UserProfile.Current();
    }

    private async Task<string> GetSpotifyCodeAsync(string challenge, CancellationToken ct = default)
    {
        var code = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var server = new EmbedIOAuthServer(_options.RedirectUri, _options.RedirectPort);
        server.AuthorizationCodeReceived += (_, response) =>
        {
            code.TrySetResult(response.Code);
            return Task.CompletedTask;
        };

        await server.Start();

        var scopes = new[]
            {
                Scopes.UserReadPrivate
            };

        var request = new LoginRequest(_options.RedirectUri, _options.ClientId, LoginRequest.ResponseType.Code)
        {
            CodeChallengeMethod = "S256",
            CodeChallenge = challenge,
            Scope = scopes
        };

        BrowserUtil.Open(request.ToUri());

        return await code.Task.WaitAsync(ct);
    }

}
