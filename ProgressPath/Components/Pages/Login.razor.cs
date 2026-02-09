using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProgressPath.Components.Pages;

/// <summary>
/// Code-behind for the Login page.
/// Handles authentication state checking and redirection for already-authenticated users.
/// </summary>
public partial class Login : ComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    [Inject]
    private IConfiguration Configuration { get; set; } = default!;

    /// <summary>
    /// Whether the dev login button should be shown.
    /// Checked server-side — when false, the button HTML is never rendered to the client.
    /// </summary>
    private bool IsDevAuthEnabled => Configuration.GetValue<bool>("DevAuth:Enabled");

    /// <summary>
    /// Whether Google OAuth is configured (ClientId and ClientSecret are set).
    /// When false, the Google sign-in button is hidden to avoid auth handler errors.
    /// </summary>
    private bool IsGoogleAuthConfigured =>
        !string.IsNullOrEmpty(Configuration["GoogleAuth:ClientId"]) &&
        !string.IsNullOrEmpty(Configuration["GoogleAuth:ClientSecret"]);

    protected override async Task OnInitializedAsync()
    {
        // Check if user is already authenticated
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            // Redirect to home if already logged in
            NavigationManager.NavigateTo("/", forceLoad: false);
        }
    }
}
