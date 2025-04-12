using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SpotifyWebAPI_Intro.Services;

namespace SpotifyWebAPI_Intro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly OptionsService _options;
        private readonly HttpService _httpService;

        public AuthController(OptionsService options, HttpService httpService)
        {
            _options = options;
            _httpService = httpService;
        }



        [HttpGet("Login")]
        public IActionResult Login()
        {
            string ClientID = _options.SpotifyClientId;

            // Set Response Type
            const string ResponseType = "code";

            // Set the scope value
            const string SCOPE = "user-read-private user-read-email";

            // Set Redirect URI
            string RedirectURI = _options.SpotifyRedirectUri;

            // Set Auth URL
            string AuthURL = _options.SpotifyAuthUrl;

            // Querry Parameters
            var queryParameters = new
            {
                client_id = ClientID,
                response_type = ResponseType,
                scope = SCOPE,
                redirect_uri = RedirectURI,
                show_dialog = true
            };

            // Build the query string from the parameters
            var queryString = ToQueryString(queryParameters);

            // Form the authorization URL
            var auth_url = $"{AuthURL}?{queryString}";

            // Redirect to the authorization URL
            return Redirect(auth_url);
        }



        [HttpGet("Callback")]
        public async Task<IActionResult> Callback(HttpContext context)
        {
            // Set webpage content type
            context.Response.ContentType = "text/html";

            // Check if "error" exists in the query string and not null
            if (context.Request.Query.ContainsKey("error"))
            {
                // Return the JSON error message if not exists
                await context.Response.WriteAsJsonAsync(new { error = context.Request.Query["error"] });

                return BadRequest("error");
            }

            // Check if "code" exists in the query string
            if (context.Request.Query.ContainsKey("code"))
            {
                // Set and Check if the Response Type value exists in query string is not null
                string ResponseType = context.Request.Query["code"].ToString() ?? throw new InvalidOperationException("The 'code' parameter is missing in the query string.");

                // Set the Grant Type
                string GrantType = "authorization_code";

                // Set Redirect URI
                string RedirectURI = _options.SpotifyRedirectUri;

                // Set Client ID
                string ClientID = _options.SpotifyClientId;

                // Set Client Secret
                string ClientSecret = _options.SpotifyClientSecret;

                // Set Token URL
                string TokenURL = _options.SpotifyTokenUrl;

                // Initialize the rquest body
                var RequestBody = new Dictionary<string, string>
                {
                  { "code", ResponseType },
                  { "grant_type", GrantType },
                  { "redirect_url", RedirectURI },
                  { "client_id", ClientID },
                  { "client_secret", ClientSecret }
                };

                // Set Token Info
                var TokenInfo = await _httpService.PostFormUrlEncodedContentAsync(TokenURL, RequestBody);

                // Set and check access_token is not null
                string AccessToken = TokenInfo.GetString("access_token") ?? throw new InvalidOperationException("No 'access_token' found");

                // Set and check refresh_token is not null
                string RefreshToken = TokenInfo.GetString("refresh_token") ?? throw new InvalidOperationException("No 'refresh_token' found");

                // Set and check expires_in is not null
                string StrExpiresIn = TokenInfo.GetString("expires_in") ?? throw new InvalidOperationException("No 'refresh_token' found");

                // Set ExpiresIn
                string ExpiresIn = ToTimeStamp(StrExpiresIn).ToString();

                // Store AccessToken in the session
                context.Session.SetString("access_token", AccessToken);

                // Store RefreshToken in the session
                context.Session.SetString("refresh_token", RefreshToken);

                // Store ExpiresIn in the session
                context.Session.SetString("expires_in", ExpiresIn);

                // Redirect to playlists
                context.Response.Redirect("/playlists");

                return Ok("callback route");
            }

            else
            {
                // Handle the case where neither "error" nor "code" is present
                await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });

                return BadRequest("Invalid request");
            }
        }



        [HttpGet("RefreshToken")]
        public async Task<IActionResult> RefreshToken(HttpContext context)
        {
            // Set webpage content type
            context.Response.ContentType = "text/html";

            // Set and Check if access_token exists in the session and is not null
            if (string.IsNullOrEmpty(context.Session.GetString("access_token")))
            {
                // The access token is not exist
                context.Response.Redirect("/login");

                // Redirect back to Spotify login page
                return BadRequest("login route");
            }

            // Set current time
            long CurrentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Check If the access_token is expired
            if (IsTokenExpired(context, CurrentTime))
            {
                // Console prompt for debugging
                Console.WriteLine("TOKEN EXPIRED. REFRESHING...");

                // Set the grant_type
                string GrantType = "refresh_token";

                // Check refresh_token value exists in query string and is not null
                string OldRefreshToken = context.Request.Query["refresh_token"].ToString() ?? throw new InvalidOperationException("The 'refresh_token' parameter is missing in the query string.");

                // Set Client ID
                string ClientID = _options.SpotifyClientId;

                // Set Client Secret
                string ClientSecret = _options.SpotifyClientSecret;

                // Set Token URL
                string TokenURL = _options.SpotifyTokenUrl;

                // Initialize request body
                var RequestBody = new Dictionary<string, string>
                {
                  { "grant_type", GrantType },
                  { "refresh_token", OldRefreshToken },
                  { "client_id", ClientID },
                  { "client_secret", ClientSecret }
                };

                // Set Token Info
                var TokenInfo = await _httpService.PostFormUrlEncodedContentAsync(TokenURL, RequestBody);

                // Set and check access_token is not null
                string AccessToken = TokenInfo.GetString("access_token") ?? throw new InvalidOperationException("No 'access_token' found");

                // Set and check refresh_token is not null
                string RefreshToken = TokenInfo.GetString("refresh_token") ?? throw new InvalidOperationException("No 'refresh_token' found");

                // Set and check expires_in is not null
                string StrExpiresIn = TokenInfo.GetString("expires_in") ?? throw new InvalidOperationException("No 'refresh_token' found");

                // Set NewExpiresIn
                long _ExpiresIn = ToTimeStamp(StrExpiresIn);

                // Calculate total expires_in
                string ExpiresIn = (CurrentTime + _ExpiresIn).ToString();

                // Store access_token in session
                context.Session.SetString("access_token", AccessToken);

                // Store refresh_token in session
                context.Session.SetString("refresh_token", RefreshToken);

                // Update session with the new expiration date
                context.Session.SetString("expires_in", ExpiresIn);

                // Redirect to playlists
                context.Response.Redirect("/playlists");

                // Redirect back to playlists route
                return Ok("playlists route");
            }

            // Access token is not expired
            context.Response.Redirect("/playlists");

            // Redirect back to playlists route
            return Ok("playlists route");
        }

        // Helper function for building query strings
        public static string ToQueryString(object queryParameters)
        {
            return string.Join("&",
            queryParameters.GetType().GetProperties()
            .Select(prop => $"{prop.Name}={Uri.EscapeDataString(prop.GetValue(queryParameters)?.ToString() ?? string.Empty)}"));
        }

        // Helper function for creating time stamp token expiration date
        public static long ToTimeStamp(string strExpiresIn)
        {
            // Set ExpiresIn
            long ExpiresIn = long.Parse(strExpiresIn);

            // Converting expiration date to Unix time stamp
            return DateTimeOffset.UtcNow.AddSeconds(ExpiresIn).ToUnixTimeSeconds(); ;
        }

        // Helper function to check token expiry
        private static bool IsTokenExpired(HttpContext context, long CurrentTime)
        {
            // Set and check expires_in is not null
            string OldStrExpiresIn = context.Session.GetString("expires_in") ?? throw new InvalidOperationException("No 'refresh_token' found");

            // Set OldExpiresIn
            long ExpiresIn = long.Parse(OldStrExpiresIn);

            // Check if the token is expired
            return CurrentTime > ExpiresIn;
        }
    }
}