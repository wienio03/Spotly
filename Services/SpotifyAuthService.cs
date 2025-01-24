using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

public class SpotifyAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private string _accessToken;


    public SpotifyAuthService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public string GetAuthorizationUrl()
    {
        var clientId = _config["Spotify:ClientId"];
        var redirectUri = _config["Spotify:RedirectUri"];
        var scopes = "playlist-modify-public playlist-modify-private";

        return $"https://accounts.spotify.com/authorize?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&scope={scopes}";
    }

    public async Task<string> GetAccessToken(string code)
    {
        var clientId = _config["Spotify:ClientId"];
        var clientSecret = _config["Spotify:ClientSecret"];
        var redirectUri = _config["Spotify:RedirectUri"];

        var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization-code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            }
        );

        var response = await _httpClient.PostAsync("https://account.spotify.com/api/token", requestBody);
        var responseString = await response.Content.ReadAsStringAsync();
        var jsonResponse = JObject.Parse(responseString);

        _accessToken = jsonResponse["access_token"]?.ToString();

        return _accessToken;
    }

    public string GetAccessToken() => _accessToken;

    public async Task<List<string>> GetUserTopTracks()
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        var response = await _httpClient.GetStringAsync("https://api.spotify.com/v1/me/top/tracks?limit=10");
        var jsonResponse = JObject.Parse(response);

        return jsonResponse["items"]
            .Select(track => track["name"]?.ToString())
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();
    }

}