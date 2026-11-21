using System.Diagnostics;
using System.Drawing;
using System.Windows;
using FunHabitTracker.Infrastructure;
using Forms = System.Windows.Forms;

namespace FunHabitTracker.Desktop;

public sealed class DesktopCoordinator : IAsyncDisposable
{
    private readonly string[] _args;
    private readonly CancellationTokenSource _stop = new();
    private readonly DesktopSurfaceSwitcher _surfaceSwitcher = new();
    private SingleInstanceCoordinator? _instance;
    private HabitTrackerWebApplication? _runtime;
    private Forms.NotifyIcon? _notifyIcon;
    private Icon? _trayIcon;
    private DesktopBrowserWindow? _compactWindow;
    private DesktopBrowserWindow? _fullWindow;
    private DesktopSettingsStore? _settingsStore;
    private StartAtSignInService? _startAtSignIn;
    private Uri? _origin;
    private string? _endpointFile;
    private bool _quitting;
    private bool _disposed;

    public DesktopCoordinator(string[] args) => _args = args;

    public async Task<bool> StartAsync()
    {
        _instance = new SingleInstanceCoordinator();
        var background = _args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        var requestedCommand = _args.Contains("--quit", StringComparer.OrdinalIgnoreCase)
            ? ActivationCommand.Quit
            : _args.Contains("--full", StringComparer.OrdinalIgnoreCase)
                ? ActivationCommand.ShowFull
                : ActivationCommand.ShowCompact;
        if (!_instance.IsOwner)
        {
            if (!background || requestedCommand == ActivationCommand.Quit)
            {
                await _instance.SendAsync(requestedCommand, _stop.Token);
            }
            return false;
        }

        if (requestedCommand == ActivationCommand.Quit) return false;

        var stopwatch = Stopwatch.StartNew();
        var url = LoopbackEndpoint.SelectAvailableUrl();
        _origin = new Uri(url);
        var webArguments = _args.Where(argument =>
            !string.Equals(argument, "--background", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(argument, "--full", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(argument, "--quit", StringComparison.OrdinalIgnoreCase)).ToArray();
        _runtime = await HabitTrackerWebApplication.CreateAsync(webArguments, url, AppContext.BaseDirectory, _stop.Token);
        if (_runtime.StartupFailure is not null)
        {
            throw new InvalidOperationException(
                $"Die lokale Datenbank konnte nicht vorbereitet werden. Protokoll: {Path.Combine(_runtime.Paths.LogsPath, "startup.log")}",
                _runtime.StartupFailure);
        }
        DesktopDiagnostics.ConfigureRoot(_runtime.Paths.Root);
        await _runtime.StartAsync(_stop.Token);
        _endpointFile = Path.Combine(_runtime.Paths.Root, "desktop-endpoint.txt");
        await File.WriteAllTextAsync(_endpointFile, url, _stop.Token);
        _settingsStore = new DesktopSettingsStore(_runtime.Paths.Root);
        _startAtSignIn = new StartAtSignInService(new WindowsStartupRegistry(), Environment.ProcessPath!);
        CreateTrayIcon();
        _instance.Listen(HandleActivationAsync);
        if (!background)
        {
            if (requestedCommand == ActivationCommand.ShowFull) ShowFull();
            else ShowCompact();
        }
        stopwatch.Stop();
        DesktopPerformance.WriteColdReady(stopwatch.Elapsed.TotalMilliseconds, background);
        return true;
    }

    private Task HandleActivationAsync(ActivationCommand command)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            switch (command)
            {
                case ActivationCommand.ShowCompact: ShowCompact(); break;
                case ActivationCommand.ShowFull: ShowFull(); break;
                case ActivationCommand.Quit: Quit(); break;
            }
        }).Task;
    }

    private void CreateTrayIcon()
    {
        var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "app-icon.png");
        using var bitmap = new Bitmap(imagePath);
        _trayIcon = Icon.FromHandle(bitmap.GetHicon());
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Heute öffnen", null, (_, _) => ShowCompact());
        menu.Items.Add("Ganze App öffnen", null, (_, _) => ShowFull());
        menu.Items.Add(new Forms.ToolStripSeparator());
        var startup = new Forms.ToolStripMenuItem("Mit Windows starten") { Checked = _startAtSignIn!.IsEnabled, CheckOnClick = false };
        startup.Click += (_, _) => ToggleStartAtSignIn(startup);
        menu.Items.Add(startup);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => Quit());
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "Fun Habit Tracker",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowCompact();
    }

    private void ToggleStartAtSignIn(Forms.ToolStripMenuItem item)
    {
        try
        {
            _startAtSignIn!.SetEnabled(!_startAtSignIn.IsEnabled);
            item.Checked = _startAtSignIn.IsEnabled;
        }
        catch (Exception exception)
        {
            DesktopDiagnostics.Write(exception);
            System.Windows.MessageBox.Show("Die Autostart-Einstellung wurde nicht geändert.", "Autostart", MessageBoxButton.OK, MessageBoxImage.Warning);
            item.Checked = _startAtSignIn!.IsEnabled;
        }
    }

    private void ShowCompact()
    {
        if (_origin is null || _settingsStore is null) return;
        var timer = Stopwatch.StartNew();
        if (_compactWindow is null)
        {
            _compactWindow = new DesktopBrowserWindow(_origin, "/Home/Compact", compact: true, ShowFull, ShowCompact);
            var settings = _settingsStore.Load();
            _compactWindow.Width = settings.Width;
            _compactWindow.Height = settings.Height;
            if (settings.Left is double left && settings.Top is double top && WindowPlacement.IsVisible(left, top, settings.Width, settings.Height))
            {
                _compactWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _compactWindow.Left = left;
                _compactWindow.Top = top;
            }
            _compactWindow.Hiding += async (_, _) => await SaveWindowSettingsAsync();
        }
        _surfaceSwitcher.Activate(_compactWindow, _fullWindow, "/Home/Compact");
        timer.Stop();
        DesktopPerformance.WriteWarmActivation(timer.Elapsed.TotalMilliseconds);
    }

    private void ShowFull()
    {
        if (_origin is null) return;
        _fullWindow ??= new DesktopBrowserWindow(_origin, "/Home/Today", compact: false, ShowFull, ShowCompact);
        _surfaceSwitcher.Activate(_fullWindow, _compactWindow, "/Home/Today");
    }

    private async Task SaveWindowSettingsAsync()
    {
        if (_compactWindow is null || _settingsStore is null) return;
        await _settingsStore.SaveAsync(new DesktopSettings(
            Left: _compactWindow.Left,
            Top: _compactWindow.Top,
            Width: _compactWindow.Width,
            Height: _compactWindow.Height));
    }

    private async void Quit()
    {
        if (_quitting) return;
        _quitting = true;
        DesktopDiagnostics.WriteMessage("Explicit Quit requested.");
        await DisposeAsync();
        System.Windows.Application.Current.Shutdown();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _quitting = true;
        _stop.Cancel();
        if (_compactWindow is not null)
        {
            await SaveWindowSettingsAsync();
            _compactWindow.CloseForExit();
        }
        _fullWindow?.CloseForExit();
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        _trayIcon?.Dispose();
        if (_endpointFile is not null && File.Exists(_endpointFile)) File.Delete(_endpointFile);
        if (_runtime is not null)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { await _runtime.StopAsync(timeout.Token); } catch (OperationCanceledException) { }
            await _runtime.DisposeAsync();
        }
        _instance?.Dispose();
        _stop.Dispose();
    }
}

public static class DesktopPerformance
{
    private static readonly object Sync = new();
    private static string PathName => Path.Combine(DesktopDiagnostics.Root, "Logs", "desktop-performance.jsonl");

    public static void WriteColdReady(double milliseconds, bool background) => Write("cold-ready", milliseconds, background);
    public static void WriteWarmActivation(double milliseconds) => Write("warm-activation", milliseconds, false);

    private static void Write(string measurement, double milliseconds, bool background)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName)!);
            var line = System.Text.Json.JsonSerializer.Serialize(new
            {
                recordedAtUtc = DateTime.UtcNow,
                measurement,
                milliseconds = Math.Round(milliseconds, 1),
                background
            });
            lock (Sync) File.AppendAllText(PathName, line + Environment.NewLine);
        }
        catch (IOException)
        {
            // Performance evidence is optional and must not block the desktop lifecycle.
        }
        catch (UnauthorizedAccessException)
        {
            // The owner remains usable when its diagnostics directory is unavailable.
        }
    }
}
