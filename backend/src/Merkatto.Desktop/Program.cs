using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Merkatto.Api.Auth;
using Merkatto.Api.Common;
using Merkatto.Api.Filters;
using Merkatto.Api.Middleware;
using Merkatto.Application;
using Merkatto.Application.Auth;
using Merkatto.Application.Common;
using Merkatto.Infrastructure;
using Merkatto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Photino.NET;

// ── Data directory ──────────────────────────────────────────────────────────────
// All persistent data lives outside the install directory so that Velopack
// updates (which replace the app folder) never touch user data.
// Windows → C:\ProgramData\Merkatto  (shared between all users on the PC)
// macOS/Linux → ~/.local/share/Merkatto  (per-user, used only during dev)
var dataDir = OperatingSystem.IsWindows()
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Merkatto")
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Merkatto");
Directory.CreateDirectory(dataDir);

// ── Stable JWT signing key (generated once, persisted to disk) ──────────────────
var keyFile = Path.Combine(dataDir, "signing.key");
string signingKey;
if (File.Exists(keyFile))
{
    signingKey = File.ReadAllText(keyFile).Trim();
}
else
{
    signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    File.WriteAllText(keyFile, signingKey);
}

// ── Pick a free HTTPS port ───────────────────────────────────────────────────────
var port = GetFreePort();
var appUrl = $"https://localhost:{port}";

// ── Web host setup ───────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// Inject runtime config overrides before any other reads
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

builder.Configuration["Auth:SigningKey"] = signingKey;
builder.Configuration["ConnectionStrings:Default"] =
    $"Data Source={Path.Combine(dataDir, "merkatto.db")}";

// HTTPS on the chosen port using the ASP.NET developer certificate
// (run `dotnet dev-certs https --trust` once on a new machine)
builder.WebHost.ConfigureKestrel(options =>
    options.Listen(IPAddress.Loopback, port,
        listenOptions => listenOptions.UseHttps()));

// Same service registrations as Merkatto.Api — Desktop reuses all controllers and middleware
var authSettings = builder.Configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>()
                   ?? new AuthSettings();

builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Include controllers from Merkatto.Api (Desktop has none of its own)
builder.Services.AddControllers(opts => opts.Filters.Add<ValidationFilter>())
    .AddApplicationPart(typeof(Merkatto.Api.Controllers.AuthController).Assembly);
builder.Services.AddScoped<ValidationFilter>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authSettings.Issuer,
            ValidAudience = authSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(authSettings.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrator", p => p.RequireRole("Administrator"));
    options.AddPolicy("Collaborator", p => p.RequireRole("Administrator", "Collaborator"));
});

// No CORS needed — SPA and API share the same origin in desktop mode
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Rate limiting (same as cloud, mostly a no-op locally but keeps the policy consistent)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

// ── HTTP pipeline ────────────────────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html"); // SPA catch-all

// ── DB initialisation (before accepting requests) ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.RunAsync();
}

// ── Start the web host, then open the Photino window ────────────────────────────
await app.StartAsync();

var window = new PhotinoWindow()
    .SetTitle("Merkatto")
    .SetUseOsDefaultSize(true)
    .SetContextMenuEnabled(false)
    .SetDevToolsEnabled(false)
    .Load(new Uri(appUrl));

window.WaitForClose(); // native message loop; blocks until user closes the window

await app.StopAsync();

// ── Helpers ──────────────────────────────────────────────────────────────────────
static int GetFreePort()
{
    var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
