using FunHabitTracker.Infrastructure;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
    .AddCommandLine(args)
    .AddEnvironmentVariables()
    .Build();

var desktopMode = configuration.GetValue<bool>("FunHabitTracker:DesktopMode");
var openDesktopBrowser = configuration.GetValue("FunHabitTracker:OpenBrowser", true);
Mutex? desktopMutex = null;
string? desktopUrl = null;

if (desktopMode)
{
    desktopMutex = new Mutex(initiallyOwned: true, "Local\\FunHabitTracker.Desktop", out var ownsMutex);
    var paths = new AppPaths(configuration);
    paths.EnsureCreated();
    var endpointFile = Path.Combine(paths.Root, "desktop-endpoint.txt");
    if (!ownsMutex)
    {
        if (openDesktopBrowser && File.Exists(endpointFile))
        {
            BrowserLauncher.Open(await File.ReadAllTextAsync(endpointFile));
        }
        desktopMutex.Dispose();
        return;
    }
    desktopUrl = LoopbackEndpoint.SelectAvailableUrl();
}

await using var runtime = await HabitTrackerWebApplication.CreateAsync(args, desktopUrl);

if (desktopMode && desktopUrl is not null)
{
    var endpointFile = Path.Combine(runtime.Paths.Root, "desktop-endpoint.txt");
    try
    {
        await runtime.StartAsync();
        await File.WriteAllTextAsync(endpointFile, desktopUrl);
        if (openDesktopBrowser) BrowserLauncher.Open(desktopUrl);
        await runtime.WaitForShutdownAsync();
    }
    finally
    {
        if (File.Exists(endpointFile)) File.Delete(endpointFile);
        desktopMutex?.ReleaseMutex();
        desktopMutex?.Dispose();
    }
}
else
{
    await runtime.RunAsync();
}

public partial class Program;
