using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MusicTransify.src.Services.Session;
using MusicTransify.src.Utilities.Token;
using MusicTransify.src.Configurations.Spotify;
using Microsoft.Extensions.Options;
using MusicTransify.src.Services.Cache;
using MusicTransify.src.Contracts.Services.Http.Spotify;

namespace MusicTransify.src.Controllers.Playlists.Spotify
{
    [ApiController]
    [Route("/spotify/playlist")] // Route: "/spotify/playlist"
    public class SpotifyPlaylistController : Controller
    {
        private readonly SpotifyOptions _spotifyOptions;
        private readonly SessionService _sessionService;
        private readonly ISpotifyHttpService _spotifyHttpService;
        private readonly TokenHelper _tokenHelper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SpotifyPlaylistController> _logger;

        public SpotifyPlaylistController(
            IOptions<SpotifyOptions> spotifyOptions,
            SessionService sessionService,
            ISpotifyHttpService spotifyHttpService,
            TokenHelper token,
            ICacheService cacheService,
            ILogger<SpotifyPlaylistController> logger
        )
        {
            _spotifyOptions = spotifyOptions.Value;
            _sessionService = sessionService;
            _spotifyHttpService = spotifyHttpService;
            _tokenHelper = token;
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet("")] // Route: "/spotify/playlist"
        public async Task<IActionResult> GetPlaylistsAsync()
        {
            var clientName = _spotifyOptions.ClientName;
            string apiBaseUri = _spotifyOptions.ApiBaseUri;
            string endPoint = _spotifyOptions.PlaylistEndpoind;

            // Use the log information
            _logger.LogInformation("Playlist controller accessed");

            // Get access token
            var accessToken = _sessionService.GetTokenInfo("access_token");

            // Check if access token exists in the session and is not null
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Missing 'accessToken' parameter from the session.");

                // Redirect back to Spotify login page
                return Redirect("/spotify/login");
            }

            // Get token expiration time
            var strExpiresIn = _sessionService.GetTokenInfo("expires_in");

            if (string.IsNullOrEmpty(strExpiresIn))
            {
                _logger.LogWarning("Missing 'expires_in' parameter in the callback request.");
                return BadRequest("expires_in is null or empty");
            }

            // Set expiresIn
            long expiresIn = _tokenHelper.ParseToLong(strExpiresIn);

            // Check If the token is expired
            if (_tokenHelper.IsExpired(expiresIn))
            {
                _logger.LogWarning("Token expired, need to refresh.");

                // Redirect to refresh token
                return Redirect("/spotify/refresh_token"); // route "/spotify/refresh_token"
            }

            // Caching
            var cacheKey = $"Spotify:User:{accessToken}:Playlists";
            var cachedPlaylists = await _cacheService.GetAsync<JsonElement>(cacheKey);

            if (cachedPlaylists.ValueKind != JsonValueKind.Undefined)
            {
                _logger.LogInformation("Serving playlists from cache.");
                return Ok(cachedPlaylists);
            }

            try
            {
                using (var response = await _spotifyHttpService.GetHttpResponseAsync(
                    clientName,
                    accessToken,
                    apiBaseUri,
                    endPoint
                ))
                {
                    if (response is null)
                    {
                        _logger.LogError("Null response received from HTTP service");
                        return StatusCode(500, "Internal server error");
                    }

                    // Handling response error
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Error fetching playlists: {StatusCode}", response.StatusCode);
                        return StatusCode((int)response.StatusCode, $"Error fetching playlists: {response.StatusCode}");
                    }

                    // Getting result as a response
                    var result = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // Deserialize playlist
                        var playlists = JsonSerializer.Deserialize<JsonElement>(result);

                        // Cache the response for 10 minutes
                        await _cacheService.SetAsync(cacheKey, playlists, TimeSpan.FromMinutes(10));

                        // Returning the playlists
                        return Ok(playlists);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse Spotify playlists response: {Response}", result);
                        return StatusCode(500, "Failed to parse playlists response from Spotify.");
                    }
                }
            }
            catch (System.Exception)
            {
                _logger.LogError("An error occurred while fetching playlists from Spotify.");
                return StatusCode(500, "Internal server error while fetching playlists.");
            }
        }
    }
}