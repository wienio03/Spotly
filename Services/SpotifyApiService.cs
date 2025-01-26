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
        Console.WriteLine(responseString);
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

    public async Task<IEnumerable<TrackModel>> GetUserTopTracks(string accessToken)
    {
        var url = "https://api.spotify.com/v1/me/top/tracks?time_range=long_term&limit=15";
        var response = await GetFromSpotifyApi<JsonElement>(url, accessToken);

        return response.GetProperty("items")
            .EnumerateArray()
            .Select(track => new TrackModel
            {
                Name = track.GetProperty("name").GetString() ?? string.Empty,
                Uri = track.GetProperty("uri").GetString() ?? string.Empty,
                ImageUri = track.GetProperty("album").GetProperty("images").EnumerateArray().FirstOrDefault().GetProperty("url").GetString() ?? string.Empty,
                ArtistName = track.GetProperty("artists")[0].GetProperty("name").GetString() ?? string.Empty,
            }
            ).Where(t => !string.IsNullOrEmpty(t.Name));
    }

    public async Task<IEnumerable<TrackModel>> SearchTracksByPrompt(string prompt, string accessToken, int offset)
    {
        var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(prompt)}&type=track&limit=15&offset={offset}";
        var response = await GetFromSpotifyApi<JsonElement>(url, accessToken);

        return response.GetProperty("tracks")
            .GetProperty("items")
            .EnumerateArray()

            .Select(track => new TrackModel
            {
                Name = track.GetProperty("name").GetString() ?? string.Empty,
                Uri = track.GetProperty("uri").GetString() ?? string.Empty,
                ImageUri = track.GetProperty("album").GetProperty("images").EnumerateArray().FirstOrDefault().GetProperty("url").GetString() ?? string.Empty,
                ArtistName = track.GetProperty("artists")[0].GetProperty("name").GetString() ?? string.Empty,
            }
            ).Where(t => !string.IsNullOrEmpty(t.Name) && !t.Name.Contains(prompt));
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
            { "uris", trackUris.ToArray() },
            { "position", 0 }
        };

        var jsonBody = JsonSerializer.Serialize(body);
        Console.WriteLine(jsonBody);

        await PostToSpotifyApi<JsonElement>(url, accessToken, body);
    }
    public async Task<string> CreatePlaylistByPrompt(string name, string description, string prompt, string accessToken, string userId, int offset)
    {
        var tracks = await SearchTracksByPrompt(prompt, accessToken, offset);
        var trackUris = tracks.Select(t => t.Uri);

        if (!trackUris.Any())
        {
            throw new Exception("No tracks matching the prompt were found");
        }

        var playlistId = await CreatePlaylist(userId, name, description, true, accessToken);
        await AddTracksToPlaylist(playlistId, trackUris, accessToken);

        return $"https://open.spotify.com/playlist/{playlistId}";
    }
}
