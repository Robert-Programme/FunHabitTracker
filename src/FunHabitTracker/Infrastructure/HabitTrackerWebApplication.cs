using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using FunHabitTracker.Data;
using FunHabitTracker.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FunHabitTracker.Infrastructure;

public sealed class HabitTrackerWebApplication : IAsyncDisposable
{
    private readonly WebApplication _application;

    private HabitTrackerWebApplication(WebApplication application, IAppPaths paths, Exception? startupFailure)
    {
        _application = application;
        Paths = paths;
        StartupFailure = startupFailure;
    }

    public IAppPaths Paths { get; }
    public Exception? StartupFailure { get; }
    public IServiceProvider Services => _application.Services;

    public static async Task<HabitTrackerWebApplication> CreateAsync(
        string[] args,
        string? url = null,
        string? contentRootPath = null,
        CancellationToken cancellationToken = default)
    {
        var webOptions = contentRootPath is null
            ? new WebApplicationOptions { Args = args, ApplicationName = typeof(Program).Assembly.FullName }
            : new WebApplicationOptions { Args = args, ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = contentRootPath };
        var builder = WebApplication.CreateBuilder(webOptions);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        if (!string.IsNullOrWhiteSpace(url)) builder.WebHost.UseUrls(url);

        var appPaths = new AppPaths(builder.Configuration);
        appPaths.EnsureCreated();
        builder.Services.AddSingleton<IAppPaths>(appPaths);
        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddSingleton<ILocalDateProvider, LocalDateProvider>();
        builder.Services.AddControllersWithViews();

        var dataProtectionKeysPath = Path.Combine(appPaths.Root, "DataProtectionKeys");
        Directory.CreateDirectory(dataProtectionKeysPath);
        builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = appPaths.DatabasePath,
            ForeignKeys = true
        }.ToString();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddScoped<DatabaseInitializer>();
        builder.Services.AddScoped<RecurrenceService>();
        builder.Services.AddScoped<StreakCalculator>();
        builder.Services.AddScoped<HabitService>();
        builder.Services.AddScoped<DailyTrackingService>();
        builder.Services.AddScoped<BackupService>();

        var app = builder.Build();
        Exception? startupFailure = null;
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            startupFailure = exception;
            var startupLog = Path.Combine(appPaths.LogsPath, "startup.log");
            await File.AppendAllTextAsync(startupLog, $"[{DateTime.UtcNow:O}] {exception}\n\n", cancellationToken);
            app.Logger.LogCritical(exception, "Database initialization failed. Recovery log: {StartupLog}", startupLog);
        }

        ConfigurePipeline(app, appPaths, startupFailure);
        return new HabitTrackerWebApplication(app, appPaths, startupFailure);
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => _application.StartAsync(cancellationToken);
    public Task RunAsync(CancellationToken cancellationToken = default) => _application.RunAsync(cancellationToken);
    public Task WaitForShutdownAsync(CancellationToken cancellationToken = default) => _application.WaitForShutdownAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken = default) => _application.StopAsync(cancellationToken);
    public async ValueTask DisposeAsync() => await _application.DisposeAsync();

    private static void ConfigurePipeline(WebApplication app, IAppPaths paths, Exception? startupFailure)
    {
        if (startupFailure is not null)
        {
            app.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync($$"""
                    <!doctype html><html lang="de"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width">
                    <title>Habit Tracker konnte nicht starten</title></head><body><main><h1>Habit Tracker konnte nicht starten</h1>
                    <p>Die lokale Datenbank konnte nicht vorbereitet werden. Deine Daten wurden nicht geloescht.</p>
                    <p>Pruefe <code>{{Path.Combine(paths.LogsPath, "startup.log")}}</code> und Sicherungen unter <code>{{paths.BackupsPath}}</code>.</p>
                    </main></body></html>
                    """);
            });
            return;
        }

        if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
        app.Use(async (context, next) =>
        {
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; connect-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; font-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
    }
}

public static class LoopbackEndpoint
{
    public static string SelectAvailableUrl()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}";
    }
}

public static class BrowserLauncher
{
    public static bool Open(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttp
            || !IPAddress.TryParse(uri.Host, out var address)
            || !IPAddress.IsLoopback(address))
        {
            return false;
        }

        Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
        return true;
    }
}
