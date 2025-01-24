using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public class SpotifyAuthService : ISpotifyAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SpotifyAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string GetAuthorizationUrl()
    {
        var clientId = _configuration["Spotify:ClientId"];
        var redirectUri = _configuration["RedirectUri"];
        var scopes = "playlist-modify-public play-list-modify-private user-top-read";

        return $"https://accounts.spotify.com/authorize?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&scope={scopes}";
    }

    public async Task<string> GetAccessToken(string code)
    {
        var clientId = _configuration["Spotify:ClientId"] ?? throw new Exception("clientId is null");
        var clientSecret = _configuration["Spotify:ClientSecret"] ?? throw new Exception("clientSecret is null");
        var redirectUri = _configuration["Spotify:RedirectUri"] ?? throw new Exception("redirectUri is null");

        var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("grant_type", "authorization_code"),
                new KeyValuePair<string,string>("code", code),
                new KeyValuePair<string,string>("redirect_uri", redirectUri),
                new KeyValuePair<string,string>("client_id", clientId),
                new KeyValuePair<string,string>("client_secret", clientSecret)
            }
        );

        var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", requestBody);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

        return jsonResponse.GetProperty("access_token").GetString() ?? "Access token is null";
    }
}