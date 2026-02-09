using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using ProgressPath.Services;

namespace ProgressPath.Controllers;

/// <summary>
/// Controller handling OAuth authentication endpoints.
/// Manages Google OAuth login flow and logout functionality.
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ProgressPath.Services.IAuthenticationService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ProgressPath.Services.IAuthenticationService authService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initiates the Google OAuth login flow.
    /// Redirects to Google's OAuth consent screen.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        // Ensure returnUrl is safe (only allow local URLs)
        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Callback), new { returnUrl }),
            IsPersistent = true
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles the OAuth callback from Google.
    /// Creates or updates the user record and establishes the session.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? returnUrl = "/")
    {
        // Ensure returnUrl is safe
        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        // Authenticate the user from the OAuth callback
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            _logger.LogWarning("Google authentication failed: {Failure}", authenticateResult.Failure?.Message);
            return Redirect("/login?error=auth_failed");
        }

        var principal = authenticateResult.Principal;
        if (principal == null)
        {
            _logger.LogWarning("Authentication succeeded but principal is null");
            return Redirect("/login?error=no_principal");
        }

        // Create or update user in database (REQ-AUTH-002, REQ-AUTH-003)
        var user = await _authService.GetOrCreateUserAsync(principal);
        if (user == null)
        {
            _logger.LogWarning("Failed to create or retrieve user from claims");
            return Redirect("/login?error=user_creation_failed");
        }

        _logger.LogInformation("User {Email} authenticated successfully", user.Email);

        // Sign in with cookie authentication for session persistence
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

        return Redirect(returnUrl);
    }

    /// <summary>
    /// Logs in immediately as a fictional test teacher.
    /// Only available when DevAuth:Enabled is set to "true" in configuration.
    /// When disabled, returns 404 — the endpoint effectively does not exist.
    /// </summary>
    [HttpGet("dev-login")]
    public async Task<IActionResult> DevLogin([FromQuery] string? returnUrl = "/")
    {
        // Hard gate: if not enabled, this endpoint does not exist
        var devAuthEnabled = _configuration.GetValue<bool>("DevAuth:Enabled");
        if (!devAuthEnabled)
        {
            return NotFound();
        }

        // Ensure returnUrl is safe
        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        // Create a ClaimsPrincipal with fictional teacher data
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "dev-teacher-00001"),
            new(ClaimTypes.Email, "dev.teacher@progresspath.test"),
            new(ClaimTypes.Name, "Dev Teacher"),
            new("picture", "https://ui-avatars.com/api/?name=Dev+Teacher&background=22d3d8&color=0f0a1a&bold=true&size=128")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Create or update user in database (same flow as Google OAuth)
        var user = await _authService.GetOrCreateUserAsync(principal);
        if (user == null)
        {
            _logger.LogWarning("Dev login: failed to create or retrieve test user");
            return Redirect("/login?error=user_creation_failed");
        }

        _logger.LogInformation("Dev login: user {Email} authenticated as test teacher", user.Email);

        // Sign in with cookie authentication
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

        return Redirect(returnUrl);
    }

    /// <summary>
    /// Logs out the current user by clearing the authentication cookie.
    /// </summary>
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }
}
