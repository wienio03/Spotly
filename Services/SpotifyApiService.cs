using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class SpotifyApiService : ISpotifyApiService
{

    private readonly HttpClient _httpClient;

    public SpotifyApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task<T> GetFromSpotifyApi<T>(string url, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var responseString = await _httpClient.GetStringAsync(url);
        return JsonSerializer.Deserialize<T>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new Exception("Error during GET from Spotify API");
    }

    private async Task<T> PostToSpotifyApi<T>(string url, string accessToken, object body)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var jsonBody = JsonSerializer.Serialize(body);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new Exception("Error during POST to Spotify API");
    }

    public async Task<string> GetUserId(string accessToken)
    {
        var url = "https://api.spotify.com/v1/me";
        var response = await GetFromSpotifyApi<JsonElement>(url, accessToken);

        return response.GetProperty("id").GetString() ?? throw new Exception("Error while getting userId");
    }

    public async Task<IEnumerable<string>> GetUserTopTracks(string accessToken)
    {
        var url = "https://api.spotify.com/v1/me/top/tracks?limit=10";
        var response = await GetFromSpotifyApi<JsonElement>(url, accessToken);

        return response.GetProperty("items")
            .EnumerateArray()
            .Select(track => track.GetProperty("name").GetString() ?? String.Empty)
            .Where(name => !string.IsNullOrEmpty(name));
    }

    public async Task<IEnumerable<string>> SearchTracksByPrompt(string prompt, string accessToken)
    {
        var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(prompt)}&type=track&limit=15";
        var response = await GetFromSpotifyApi<JsonElement>(url, accessToken);

        return response.GetProperty("items")
            .EnumerateArray()
            .Select(track => track.GetProperty("uri").GetString() ?? string.Empty)
            .Where(uri => !string.IsNullOrEmpty(uri));
    }

    public async Task<string> CreatePlaylist(string userId, string name, string description, bool isPublic, string accessToken)
    {
        var url = $"https://api.spotify.com/v1/users/{userId}/playlists";
        var body = new
        {
            name = name,
            description = description,
            @public = isPublic
        };

        var response = await PostToSpotifyApi<JsonElement>(url, accessToken, body);

        return response.GetProperty("id").GetString() ?? throw new Exception("Error while creating a playlist");

    }

    public async Task AddTracksToPlaylist(string playlistId, IEnumerable<string> trackUris, string accessToken)
    {
        var url = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks";

        var body = new Dictionary<string, object>
    {
        { "uris", trackUris.ToArray() }
    };

        await PostToSpotifyApi<JsonElement>(url, accessToken, body);
    }

    public async Task<string> CreatePlaylistByPrompt(string name, string description, string prompt, string accessToken, string userId)
    {
        var trackUris = await SearchTracksByPrompt(prompt, accessToken);

        if (!trackUris.Any())
        {
            throw new Exception("No tracks matching the prompt were found");
        }

        var playlistId = await CreatePlaylist(userId, name, description, true, accessToken);

        await AddTracksToPlaylist(playlistId, trackUris, accessToken);

        return playlistId;
    }
}
