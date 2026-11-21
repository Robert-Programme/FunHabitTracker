using Microsoft.VisualStudio.TestTools.UnitTesting;
using FunHabitTracker.Infrastructure;

namespace FunHabitTracker.Desktop.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DesktopLifecycleTests
{
    [TestMethod]
    public void SecondInstance_SendsValidatedCommandToOwner()
    {
        using var owner = new SingleInstanceCoordinator();
        Assert.IsTrue(owner.IsOwner);
        var received = new TaskCompletionSource<ActivationCommand>(TaskCreationOptions.RunContinuationsAsynchronously);
        owner.Listen(command => { received.TrySetResult(command); return Task.CompletedTask; });
        Task.Run(() =>
        {
            using var second = new SingleInstanceCoordinator();
            Assert.IsFalse(second.IsOwner);
            Assert.IsTrue(second.SendAsync(ActivationCommand.ShowFull).GetAwaiter().GetResult());
        }).GetAwaiter().GetResult();
        Assert.AreEqual(
            ActivationCommand.ShowFull,
            received.Task.WaitAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult());
    }

    [TestMethod]
    public void ActivationProtocol_RejectsUnknownAndOversizedCommands()
    {
        Assert.IsTrue(ActivationProtocol.TryParse("ShowCompact", out var command));
        Assert.AreEqual(ActivationCommand.ShowCompact, command);
        Assert.IsFalse(ActivationProtocol.TryParse("showcompact", out _));
        Assert.IsFalse(ActivationProtocol.TryParse(new string('x', ActivationProtocol.MaxCommandLength + 1), out _));
    }

    [TestMethod]
    public void NavigationPolicy_AllowsOnlyExactLoopbackOrigin()
    {
        var origin = new Uri("http://127.0.0.1:5016");
        Assert.IsTrue(NavigationPolicy.IsAllowed(origin, "http://127.0.0.1:5016/Home/Compact"));
        Assert.IsFalse(NavigationPolicy.IsAllowed(origin, "https://127.0.0.1:5016/Home/Compact"));
        Assert.IsFalse(NavigationPolicy.IsAllowed(origin, "http://127.0.0.1:5017/Home/Compact"));
        Assert.IsFalse(NavigationPolicy.IsAllowed(origin, "https://example.com"));
        Assert.IsFalse(BrowserLauncher.Open("https://example.com"));
    }

    [TestMethod]
    public void SurfaceNavigationPolicy_RecognizesOnlyExactOppositeSurfaceRoute()
    {
        var origin = new Uri("http://127.0.0.1:5016");

        Assert.AreEqual(DesktopSurface.Full, SurfaceNavigationPolicy.Resolve(origin, sourceIsCompact: true, "http://127.0.0.1:5016/Home/Today"));
        Assert.AreEqual(DesktopSurface.Compact, SurfaceNavigationPolicy.Resolve(origin, sourceIsCompact: false, "http://127.0.0.1:5016/Home/Compact"));
        Assert.IsNull(SurfaceNavigationPolicy.Resolve(origin, sourceIsCompact: true, "http://127.0.0.1:5016/Home/Compact"));
        Assert.IsNull(SurfaceNavigationPolicy.Resolve(origin, sourceIsCompact: false, "http://127.0.0.1:5016/Home/Today"));
        Assert.IsNull(SurfaceNavigationPolicy.Resolve(origin, sourceIsCompact: false, "http://127.0.0.1:5016/Home/Compact?source=test"));
        Assert.IsNull(SurfaceNavigationPolicy.Resolve(origin, sourceIsCompact: false, "http://127.0.0.1:5017/Home/Compact"));
    }

    [TestMethod]
    public void SurfaceSwitcher_HidesPreviousBeforeRefreshingAndShowingRequestedWindow()
    {
        var events = new List<string>();
        var compact = new FakeSurfaceWindow("compact", events, isVisible: true);
        var full = new FakeSurfaceWindow("full", events, isVisible: false);
        var switcher = new DesktopSurfaceSwitcher();

        switcher.Activate(full, compact, "/Home/Today");
        CollectionAssert.AreEqual(new[] { "compact:hide", "full:navigate:/Home/Today", "full:show" }, events);
        Assert.IsFalse(compact.IsVisible);
        Assert.IsTrue(full.IsVisible);

        events.Clear();
        switcher.Activate(compact, full, "/Home/Compact");
        CollectionAssert.AreEqual(new[] { "full:hide", "compact:navigate:/Home/Compact", "compact:show" }, events);
        Assert.IsTrue(compact.IsVisible);
        Assert.IsFalse(full.IsVisible);

        events.Clear();
        switcher.Activate(compact, full, "/Home/Compact");
        Assert.IsTrue(compact.IsVisible);
        Assert.IsFalse(full.IsVisible);
    }

    [TestMethod]
    public void LoopbackSelection_ReturnsBindableIpv4Endpoint()
    {
        var uri = new Uri(LoopbackEndpoint.SelectAvailableUrl());
        Assert.AreEqual("127.0.0.1", uri.Host);
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, uri.Port);
        listener.Start();
        listener.Stop();
    }

    [TestMethod]
    public void WindowPlacement_RejectsPositionOutsideEveryDisplay()
    {
        Assert.IsFalse(WindowPlacement.IsVisible(-100000, -100000, 420, 620));
    }

    [TestMethod]
    public void WindowProfiles_KeepCompactDiscoverableWithoutRemovingFullMaximize()
    {
        var compact = DesktopWindowProfile.For(compact: true);
        Assert.IsTrue(compact.ShowInTaskbar);
        Assert.IsFalse(compact.CanMaximize);
        Assert.AreEqual("Heute | Fun Habit Tracker", compact.Title);

        var full = DesktopWindowProfile.For(compact: false);
        Assert.IsTrue(full.ShowInTaskbar);
        Assert.IsTrue(full.CanMaximize);
        Assert.AreEqual("Fun Habit Tracker", full.Title);
    }

    [TestMethod]
    public void NativeWindowPolicy_RemovesOnlyTheMaximizeCapability()
    {
        const long minimizeBox = 0x00020000L;
        const long thickFrame = 0x00040000L;
        var original = new nint(NativeWindowPolicy.MaximizeBox | minimizeBox | thickFrame);
        var compact = NativeWindowPolicy.CalculateStyle(original, canMaximize: false).ToInt64();
        Assert.AreEqual(0, compact & NativeWindowPolicy.MaximizeBox);
        Assert.AreNotEqual(0, compact & minimizeBox);
        Assert.AreNotEqual(0, compact & thickFrame);
        Assert.AreNotEqual(0, NativeWindowPolicy.CalculateStyle(new nint(compact), canMaximize: true).ToInt64() & NativeWindowPolicy.MaximizeBox);
    }

    [TestMethod]
    public void DesktopIcon_IsMultiSizeAndCopiedToOutput()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app-icon.ico");
        Assert.IsTrue(File.Exists(path), path);
        var bytes = File.ReadAllBytes(path);
        Assert.IsGreaterThanOrEqualTo(6, BitConverter.ToUInt16(bytes, 4));
    }

    private sealed class FakeSurfaceWindow(string name, List<string> events, bool isVisible) : IDesktopSurfaceWindow
    {
        public bool IsVisible { get; private set; } = isVisible;

        public void Hide()
        {
            events.Add($"{name}:hide");
            IsVisible = false;
        }

        public void Navigate(string path) => events.Add($"{name}:navigate:{path}");

        public void BringForward()
        {
            events.Add($"{name}:show");
            IsVisible = true;
        }
    }
}
