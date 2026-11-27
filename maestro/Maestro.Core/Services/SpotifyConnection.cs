using Maestro.Core.Errors;
using Maestro.Core.Models;
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
// TOOD: Maestro exception
public class SpotifyConnection
{
    private SpotifyConnectionOptions _options = new();
    private SpotifyClient? _client = null;

    public SpotifyClient Client => _client ?? throw new MaestroException(ErrorCode.SPOTIFY_NotConnected);

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            var (verified, challenge) = PKCEUtil.GenerateCodes();
            var code = await GetSpotifyCodeAsync(challenge, ct);
            var request = new PKCETokenRequest(_options.ClientId, code, _options.RedirectUri, verified);
            var token = await new OAuthClient().RequestToken(request);
            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new PKCEAuthenticator(_options.ClientId, token));

            _client = new SpotifyClient(config);
        }
        catch (Exception ex)
        {
            throw new MaestroException(ErrorCode.SPOTIFY_Unexpected, new { message = ex.Message }, ex);
        }
    }

    public async Task<List<Track>> SearchTracksAsync(string query, int limit, CancellationToken ct = default)
    {
        try
        {
            var request = new SearchRequest(SearchRequest.Types.Track, query)
            {
                Limit = limit,
            };

            var response = await Client.Search.Item(request, ct);

            return response.Tracks.Items?
                .Select(x => new Track(x.Id, x.Name, new Uri(x.Uri)))
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            throw new MaestroException(ErrorCode.SPOTIFY_Unexpected, new { message = ex.Message }, ex);
        }
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
            Scopes.PlaylistReadPrivate
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
