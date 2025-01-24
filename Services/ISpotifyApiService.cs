
using System.Collections.Generic;
using System.Threading.Tasks;


// an interface to make future testing easier by mocking the interface
public interface ISpotifyApiService
{
    Task<string> GetUserId(string accessToken);
    Task<IEnumerable<string>> GetUserTopTracks(string accessToken);
    Task<IEnumerable<string>> SearchTracksByPrompt(string prompt, string accessToken);

    Task<string> CreatePlaylist(string userId, string name, string description, bool isPublic, string accessToken);
    Task AddTracksToPlaylist(string playlistId, IEnumerable<string> trackUris, string accessToken);
}