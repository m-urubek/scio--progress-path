using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using ProgressPath.Data;
using ProgressPath.Hubs;
using ProgressPath.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

// Add SignalR for real-time communication (REQ-RT-001)
builder.Services.AddSignalR();

// Add controllers for OAuth endpoints
builder.Services.AddControllers();

// Add HttpContextAccessor for accessing HttpContext in Blazor components
builder.Services.AddHttpContextAccessor();

// Register DbContextFactory (also registers DbContext as scoped) for both
// regular injection and multi-threaded access (migrations, seeding, background services)
builder.Services.AddDbContextFactory<ProgressPathDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Configure authentication (REQ-AUTH-001)
var googleClientId = builder.Configuration["GoogleAuth:ClientId"];
var googleClientSecret = builder.Configuration["GoogleAuth:ClientSecret"];
var hasGoogleAuth = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    if (hasGoogleAuth)
    {
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    }
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Only register Google OAuth if credentials are configured
if (hasGoogleAuth)
{
    authBuilder.AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId!;
        googleOptions.ClientSecret = googleClientSecret!;
        googleOptions.Scope.Add("email");
        googleOptions.Scope.Add("profile");
    });
}

// Add cascading authentication state for Blazor components
builder.Services.AddCascadingAuthenticationState();

// Register authentication service
builder.Services.AddScoped<ProgressPath.Services.IAuthenticationService, ProgressPath.Services.AuthenticationService>();

// Configure LLM settings (REQ-LLM-001, REQ-LLM-004)
builder.Services.Configure<LLMSettings>(builder.Configuration.GetSection(LLMSettings.SectionName));

// Register LLM service (REQ-LLM-002)
builder.Services.AddScoped<ILLMService, LLMService>();

// Register Group service (REQ-GROUP-001 through REQ-GROUP-013)
builder.Services.AddScoped<IGroupService, GroupService>();

// Register Student Session service (REQ-GROUP-014 through REQ-GROUP-024)
builder.Services.AddScoped<IStudentSessionService, StudentSessionService>();

// Register Chat service (REQ-CHAT-001 through REQ-CHAT-009)
builder.Services.AddScoped<IChatService, ChatService>();

// Register Alert service (REQ-AI-006 through REQ-AI-020)
builder.Services.AddScoped<IAlertService, AlertService>();

// Register Inactivity Monitor background service (REQ-AI-012 through REQ-AI-016)
builder.Services.AddHostedService<InactivityMonitorService>();

// Register Hub Notification service for SignalR broadcasts (REQ-RT-001 through REQ-RT-005)
builder.Services.AddScoped<IHubNotificationService, HubNotificationService>();

// Register QR code service (REQ-GROUP-012) - singleton since it's stateless
builder.Services.AddSingleton<IQRCodeService, QRCodeService>();

var app = builder.Build();

// Apply pending migrations and seed database in development
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProgressPathDbContext>();

        // Apply pending migrations
        dbContext.Database.Migrate();

        // Seed the database with development data
        DbInitializer.Initialize(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Handle HTTP status code errors with custom error pages
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// Only redirect to HTTPS in production (Docker dev uses HTTP on port 5000)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers for OAuth endpoints
app.MapControllers();

// Map SignalR hub for real-time communication (REQ-RT-001)
app.MapHub<ProgressHub>("/progresshub");

app.MapRazorComponents<ProgressPath.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
