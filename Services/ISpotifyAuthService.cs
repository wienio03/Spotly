using System.Collections.Generic;
using System.Threading.Tasks;


// an interface to make future testing easier by mocking the interface
public interface ISpotifyAuthService
{
    Task<string> GetAccessToken(string code);

    string GetAuthorizationUrl();
}