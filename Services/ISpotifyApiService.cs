
using System.Collections.Generic;
using System.Threading.Tasks;


public interface ISpotifyApiService
{
    Task<string> GetUserId(string accessToken);

    Task<IEnumerable<TrackModel>> GetUserTopTracks(string accessToken);
    Task<IEnumerable<TrackModel>> SearchTracksByPrompt(string prompt, string accessToken, int offset);
    Task<string> CreatePlaylist(string userId, string name, string description, bool isPublic, string accessToken);
    Task AddTracksToPlaylist(string playlistId, IEnumerable<string> trackUris, string accessToken);
    Task<string> CreatePlaylistByPrompt(string name, string description, string prompt, string accessToken, string userId, int offset);
}
