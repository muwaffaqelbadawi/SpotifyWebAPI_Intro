using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using SpotifyWebAPI_Intro.utilities;

namespace SpotifyWebAPI_Intro.Services
{
    public class SessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Check query string existence
        public (string AccessToken, string RefreshToken, string ExpiresIn) CheckAssets(JsonElement TokenInfo)
        {
            // Set and check access_token is not null
            string AccessToken = TokenInfo.GetString("access_token") ?? throw new InvalidOperationException("No 'access_token' found");

            // Set and check refresh_token is not null
            string RefreshToken = TokenInfo.GetString("refresh_token") ?? throw new InvalidOperationException("No 'refresh_token' found");

            // Set and check expires_in is not null
            string ExpiresIn = TokenInfo.GetString("expires_in") ?? throw new InvalidOperationException("No 'refresh_token' found");

            return (AccessToken, RefreshToken, ExpiresIn);
        }

        // Store Token Info in session
        public void StoreAssetes(string AccessToken, string RefreshToken, string ExpiresIn)
        {
            var Session = _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("HttpContext or Session is null.");

            // Store access token in session
            Session.SetString("access_token", AccessToken);

            // Store refresh token in session
            Session.SetString("refresh_token", RefreshToken);

            // Store expiration date in session
            Session.SetString("expires_in", ExpiresIn);
        }

        // Expose token information from session
        public string RevealAssete(string asset)
        {
            var Session = _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("HttpContext or Session is null.");

            // Check the requested asset and return the appropriate session value
            return asset switch
            {
                "AccessToken" => Session.GetString("access_token") ?? "Access token not found.",
                "RefreshToken" => Session.GetString("refresh_token") ?? "Refresh token not found.",
                "ExpiresIn" => Session.GetString("expires_in") ?? "Expiration time not found.",
                _ => throw new ArgumentException($"Invalid asset: {asset}", nameof(asset))
            };
        }
    }
}
