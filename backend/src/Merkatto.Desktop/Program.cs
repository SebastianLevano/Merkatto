using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Merkatto.Application.Reports;
using Merkatto.Api.Auth;
using Merkatto.Api.Common;
using Merkatto.Api.Controllers;
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
using Velopack;

// ── Velopack: MUST be the very first executable statement ────────────────────────
// Handles install/uninstall/update hooks sent by the Velopack runtime.
// If this is a special hook invocation, Run() exits the process immediately.
VelopackApp.Build().Run();

// ── Data directory ──────────────────────────────────────────────────────────────
// Windows → C:\ProgramData\Merkatto  (shared between all users on the PC)
// macOS/Linux → ~/Library/Application Support/Merkatto  (per-user, dev only)
var dataDir = OperatingSystem.IsWindows()
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Merkatto")
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Merkatto");
Directory.CreateDirectory(dataDir);

// ── Auto-backup before touching the DB ──────────────────────────────────────────
var dbPath = Path.Combine(dataDir, "merkatto.db");
CreateStartupBackup(dataDir, dbPath);

// ── client.json pre-fill (optional, placed next to the .exe at install time) ───
var prefill = LoadClientJson();

// ── Stable JWT signing key ───────────────────────────────────────────────────────
var keyFile = Path.Combine(dataDir, "signing.key");
var signingKey = File.Exists(keyFile)
    ? File.ReadAllText(keyFile).Trim()
    : GenerateAndSave(keyFile);

// ── Pick a free port ─────────────────────────────────────────────────────────────
var port = GetFreePort();
var appUrl = $"http://localhost:{port}";

// ── Web host setup ───────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

builder.Configuration["Auth:SigningKey"] = signingKey;
builder.Configuration["ConnectionStrings:Default"] = $"Data Source={dbPath}";

builder.WebHost.ConfigureKestrel(opts => opts.Listen(IPAddress.Loopback, port));

var authSettings = builder.Configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>()
                   ?? new AuthSettings();

builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Optional pre-fill from client.json (null if file not present)
if (prefill is not null)
    builder.Services.AddSingleton(prefill);

// Manual backup trigger
builder.Services.AddSingleton(new BackupTrigger(() => CreateOnDemandBackup(dataDir, dbPath)));

// Update state — shared between background checker and /api/v1/app/info endpoint
var updateState = new AppUpdateState();
builder.Services.AddSingleton(updateState);

builder.Services.AddControllers(opts => opts.Filters.Add<ValidationFilter>())
    .AddApplicationPart(typeof(AuthController).Assembly);
builder.Services.AddScoped<ValidationFilter>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = authSettings.Issuer, ValidAudience = authSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("Administrator", p => p.RequireRole("Administrator"));
    opts.AddPolicy("Collaborator",  p => p.RequireRole("Administrator", "Collaborator"));
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

// ── HTTP pipeline ─────────────────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Desktop-only: open report file in the native OS application
app.MapGet("/api/v1/reports/{type}/open", async (
    string type, int? year, int? month, IReportService reports, CancellationToken ct) =>
{
    var today = DateTime.UtcNow;
    var y = year ?? today.Year;
    var m = month ?? today.Month;

    byte[] data;
    string filename;
    switch (type)
    {
        case "closings":
            data = await reports.ClosingsPdfAsync(y, m, ct);
            filename = $"cierres_{y}_{m:D2}.pdf";
            break;
        case "expenses":
            data = await reports.ExpensesExcelAsync(y, m, ct);
            filename = $"gastos_{y}_{m:D2}.xlsx";
            break;
        case "purchases":
            data = await reports.PurchasesExcelAsync(y, m, ct);
            filename = $"compras_{y}_{m:D2}.xlsx";
            break;
        default:
            return Results.NotFound();
    }

    var path = Path.Combine(Path.GetTempPath(), filename);
    await File.WriteAllBytesAsync(path, data, ct);
    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    return Results.NoContent();
}).RequireAuthorization();

// Desktop-only: capability flag consumed by the frontend on startup
app.MapGet("/api/v1/app/mode", () => Results.Ok(new { desktop = true }));

app.MapFallback(async (IWebHostEnvironment env, HttpContext ctx) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    var indexPath = Path.Combine(env.WebRootPath ?? "", "index.html");
    if (File.Exists(indexPath))
    {
        ctx.Response.ContentType = "text/html";
        await ctx.Response.SendFileAsync(indexPath);
    }
    else
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
});

// ── DB initialisation ────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().RunAsync();

// ── Start host ───────────────────────────────────────────────────────────────
await app.StartAsync();

// ── Background update check (non-blocking) ───────────────────────────────────
var feedUrl = builder.Configuration["Updates:FeedUrl"] ?? string.Empty;
Velopack.UpdateInfo? pendingUpdate = null;

if (!string.IsNullOrWhiteSpace(feedUrl))
{
    _ = Task.Run(async () =>
    {
        try
        {
            var mgr = new Velopack.UpdateManager(feedUrl);
            var info = await mgr.CheckForUpdatesAsync();
            if (info is not null)
            {
                await mgr.DownloadUpdatesAsync(info);
                pendingUpdate = info;
                updateState.UpdateAvailable = true;
                updateState.UpdateVersion   = info.TargetFullRelease.Version.ToString();
                app.Logger.LogInformation("Update {V} downloaded — will apply on next restart.",
                    updateState.UpdateVersion);
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Update check failed — continuing without update.");
        }
    });
}

// ── Open Photino window ───────────────────────────────────────────────────────
var window = new PhotinoWindow()
    .SetTitle("Merkatto")
    .SetUseOsDefaultSize(true)
    .SetContextMenuEnabled(false)
    .SetDevToolsEnabled(false)
    .Load(new Uri(appUrl));

window.WaitForClose();
await app.StopAsync();

// ── Apply pending update after window closes ──────────────────────────────────
// Velopack restarts the process automatically with the new version.
if (pendingUpdate is not null && !string.IsNullOrWhiteSpace(feedUrl))
{
    var mgr = new Velopack.UpdateManager(feedUrl);
    mgr.ApplyUpdatesAndRestart(pendingUpdate);
}

// ── Helpers ───────────────────────────────────────────────────────────────────

static int GetFreePort()
{
    var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var p = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return p;
}

static string GenerateAndSave(string path)
{
    var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    File.WriteAllText(path, key);
    return key;
}

static string CreateOnDemandBackup(string dataDir, string dbPath)
{
    var backupDir = Path.Combine(dataDir, "backups");
    Directory.CreateDirectory(backupDir);
    var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    var dest = Path.Combine(backupDir, $"merkatto_{stamp}.db");
    File.Copy(dbPath, dest, overwrite: true);
    var wal = dbPath + "-wal";
    if (File.Exists(wal)) File.Copy(wal, dest + "-wal", overwrite: true);
    return dest;
}

static void CreateStartupBackup(string dataDir, string dbPath)
{
    if (!File.Exists(dbPath)) return;

    var backupDir = Path.Combine(dataDir, "backups");
    Directory.CreateDirectory(backupDir);

    var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    File.Copy(dbPath, Path.Combine(backupDir, $"merkatto_{stamp}.db"), overwrite: true);

    // Also copy WAL so the backup is self-contained (SQLite replays it on open)
    var walPath = dbPath + "-wal";
    if (File.Exists(walPath))
        File.Copy(walPath, Path.Combine(backupDir, $"merkatto_{stamp}.db-wal"), overwrite: true);

    // Rotate: keep the 7 most-recent backups
    foreach (var old in Directory.GetFiles(backupDir, "merkatto_*.db")
                 .OrderByDescending(f => f).Skip(7))
        File.Delete(old);
}

static SetupPrefill? LoadClientJson()
{
    var dir = AppContext.BaseDirectory;
    var path = Path.Combine(dir, "client.json");
    if (!File.Exists(path)) return null;

    try
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var biz   = root.TryGetProperty("businessName", out var b) ? b.GetString() : null;
        var email = root.TryGetProperty("adminEmail",   out var e) ? e.GetString() : null;
        return new SetupPrefill(biz, email);
    }
    catch { return null; }
}
