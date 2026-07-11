using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PrioritizationApp.Services;
using PrioritizationApp.Web.Auth;
using PrioritizationApp.Web.Components;
using PrioritizationApp.Web.Configuration;
using PrioritizationApp.Web.Data;
using PrioritizationApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));

var dataDirectory = builder.Configuration[$"{StorageOptions.SectionName}:DataDirectory"] ?? "/data";
Directory.CreateDirectory(dataDirectory);
var dbPath = Path.Combine(dataDirectory, "priorities.db");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IAppRepository, SqliteAppRepository>();
builder.Services.AddScoped<PrioritizationService>();
builder.Services.AddScoped<ComparisonSessionHost>();
builder.Services.AddScoped<PriorityAppService>();

var allowedEmails = builder.Configuration.GetSection("Auth:AllowedEmails").Get<string[]>() ?? [];
var allowlist = new EmailAllowlist(allowedEmails);
builder.Services.AddSingleton(allowlist);

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleAuthEnabled =
    !string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret);

var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        if (googleAuthEnabled)
            options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options => options.AccessDeniedPath = "/access-denied");

if (googleAuthEnabled)
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
    });
}

builder.Services.AddAuthorization(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(context =>
        {
            var email = context.User.FindFirstValue(ClaimTypes.Email);
            return allowlist.IsAllowed(email);
        })
        .Build();

    options.DefaultPolicy = policy;

    if (!allowlist.IsEmpty)
        options.FallbackPolicy = policy;
});

var app = builder.Build();

await InitialiseDatabaseAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

MapAuthEndpoints(app, googleAuthEnabled);

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static void MapAuthEndpoints(WebApplication app, bool googleAuthEnabled)
{
    app.MapGet("/login", (string? returnUrl) =>
        {
            if (!googleAuthEnabled)
            {
                return Results.Content(
                    "Google sign-in is not configured for this environment.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
                [GoogleDefaults.AuthenticationScheme]);
        })
        .AllowAnonymous();

    app.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/");
        })
        .AllowAnonymous();

    app.MapGet("/access-denied", () => Results.Content(
            """
            <!doctype html>
            <html><head><title>Access denied</title></head>
            <body style="font-family:sans-serif;padding:2rem">
              <h1>Access denied</h1>
              <p>That Google account isn't on the allow list for this app.</p>
              <p><a href="/logout">Sign out</a> and try a different account.</p>
            </body></html>
            """,
            "text/html"))
        .AllowAnonymous();
}

static async Task InitialiseDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();

    if (!await db.Settings.AnyAsync())
    {
        db.Settings.Add(new AppSettingsRecord());
        await db.SaveChangesAsync();
    }
}

public partial class Program;
