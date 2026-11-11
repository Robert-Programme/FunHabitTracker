using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace FunHabitTracker.Desktop;

public sealed class DesktopBrowserWindow : Window, IDesktopSurfaceWindow
{
    private readonly Uri _origin;
    private readonly Uri _initialUri;
    private readonly bool _compact;
    private readonly Action _openFull;
    private readonly Action _openCompact;
    private readonly WebView2 _webView = new();
    private bool _allowClose;

    public DesktopBrowserWindow(Uri origin, string path, bool compact, Action openFull, Action openCompact)
    {
        var profile = DesktopWindowProfile.For(compact);
        _origin = origin;
        _initialUri = new Uri(origin, path);
        _compact = compact;
        _openFull = openFull;
        _openCompact = openCompact;
        Title = profile.Title;
        Width = compact ? 420 : 1100;
        Height = compact ? 620 : 780;
        MinWidth = compact ? 320 : 720;
        MinHeight = compact ? 400 : 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = profile.ShowInTaskbar;
        ResizeMode = ResizeMode.CanResize;
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app-icon.ico");
        if (File.Exists(iconPath)) Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        Content = BuildContent();
        SourceInitialized += (_, _) => NativeWindowPolicy.SetCanMaximize(new WindowInteropHelper(this).Handle, profile.CanMaximize);
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public event EventHandler? Hiding;

    public void CloseForExit()
    {
        _allowClose = true;
        Close();
    }

    public void BringForward()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    public void Navigate(string path)
    {
        if (_webView.CoreWebView2 is not null) _webView.CoreWebView2.Navigate(new Uri(_origin, path).ToString());
    }

    private UIElement BuildContent()
    {
        var grid = new Grid();
        grid.Children.Add(_webView);
        return grid;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var userData = Path.Combine(DesktopDiagnostics.Root, "WebView2");
            Directory.CreateDirectory(userData);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
            await _webView.EnsureCoreWebView2Async(environment);
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
            _webView.CoreWebView2.NewWindowRequested += (_, request) => request.Handled = true;
            _webView.CoreWebView2.Navigate(_initialUri.ToString());
        }
        catch (Exception exception)
        {
            DesktopDiagnostics.Write(exception);
            var result = System.Windows.MessageBox.Show(
                "Das eingebettete Fenster konnte nicht gestartet werden. Soll die lokale App im Standardbrowser geöffnet werden?",
                "Fenster nicht verfügbar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes) Process.Start(new ProcessStartInfo(_origin.ToString()) { UseShellExecute = true });
            Hide();
        }
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!NavigationPolicy.IsAllowed(_origin, e.Uri))
        {
            e.Cancel = true;
            return;
        }

        var requestedSurface = SurfaceNavigationPolicy.Resolve(_origin, _compact, e.Uri);
        if (requestedSurface is not null)
        {
            e.Cancel = true;
            if (requestedSurface == DesktopSurface.Full) _openFull();
            else _openCompact();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        Hiding?.Invoke(this, EventArgs.Empty);
        Hide();
    }
}

public enum DesktopSurface
{
    Compact,
    Full
}

public static class SurfaceNavigationPolicy
{
    public static DesktopSurface? Resolve(Uri origin, bool sourceIsCompact, string candidate)
    {
        if (!NavigationPolicy.IsAllowed(origin, candidate)
            || !Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || uri.Query.Length > 0
            || uri.Fragment.Length > 0)
        {
            return null;
        }

        if (sourceIsCompact && string.Equals(uri.AbsolutePath, "/Home/Today", StringComparison.OrdinalIgnoreCase))
        {
            return DesktopSurface.Full;
        }

        if (!sourceIsCompact && string.Equals(uri.AbsolutePath, "/Home/Compact", StringComparison.OrdinalIgnoreCase))
        {
            return DesktopSurface.Compact;
        }

        return null;
    }
}

public interface IDesktopSurfaceWindow
{
    bool IsVisible { get; }
    void Hide();
    void Navigate(string path);
    void BringForward();
}

public sealed class DesktopSurfaceSwitcher
{
    public void Activate(IDesktopSurfaceWindow requested, IDesktopSurfaceWindow? previous, string path)
    {
        if (previous is not null && !ReferenceEquals(previous, requested)) previous.Hide();
        requested.Navigate(path);
        requested.BringForward();
    }
}

public sealed record DesktopWindowProfile(string Title, bool ShowInTaskbar, bool CanMaximize)
{
    public static DesktopWindowProfile For(bool compact) => compact
        ? new("Heute | Fun Habit Tracker", ShowInTaskbar: true, CanMaximize: false)
        : new("Fun Habit Tracker", ShowInTaskbar: true, CanMaximize: true);
}

public static class NativeWindowPolicy
{
    public const long MaximizeBox = 0x00010000L;
    private const int StyleIndex = -16;
    private const uint NoMove = 0x0002;
    private const uint NoSize = 0x0001;
    private const uint NoZOrder = 0x0004;
    private const uint FrameChanged = 0x0020;

    public static nint CalculateStyle(nint style, bool canMaximize)
    {
        var value = style.ToInt64();
        return new nint(canMaximize ? value | MaximizeBox : value & ~MaximizeBox);
    }

    public static void SetCanMaximize(nint handle, bool canMaximize)
    {
        var style = GetWindowLongPtr(handle, StyleIndex);
        SetWindowLongPtr(handle, StyleIndex, CalculateStyle(style, canMaximize));
        SetWindowPos(handle, nint.Zero, 0, 0, 0, 0, NoMove | NoSize | NoZOrder | FrameChanged);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint window, int index, nint value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
}

public static class NavigationPolicy
{
    public static bool IsAllowed(Uri origin, string candidate)
    {
        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttp
            && uri.Host.Equals(origin.Host, StringComparison.OrdinalIgnoreCase)
            && uri.Port == origin.Port;
    }
}

public static class WindowPlacement
{
    public static bool IsVisible(double left, double top, double width, double height)
    {
        var rectangle = new System.Drawing.Rectangle(
            (int)Math.Round(left),
            (int)Math.Round(top),
            Math.Max(1, (int)Math.Round(width)),
            Math.Max(1, (int)Math.Round(height)));
        return System.Windows.Forms.Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(rectangle));
    }
}
