using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Desktop.Tests;

[TestClass]
public sealed class DesktopSettingsTests
{
    [TestMethod]
    public async Task Settings_DefaultAndAtomicRoundTripRemainValid()
    {
        var root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-desktop-settings-{Guid.NewGuid():N}");
        try
        {
            var store = new DesktopSettingsStore(root);
            Assert.AreEqual(new DesktopSettings(), store.Load());
            var expected = new DesktopSettings(Left: 100, Top: 120, Width: 500, Height: 700);
            await store.SaveAsync(expected);
            Assert.AreEqual(expected, store.Load());
            Assert.AreEqual(0, Directory.GetFiles(root, "*.tmp").Length);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void Settings_InvalidDocumentFallsBackWithoutDeletingIt()
    {
        var root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-desktop-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "desktop-settings.json");
            File.WriteAllText(path, "{broken");
            var store = new DesktopSettingsStore(root);
            Assert.AreEqual(new DesktopSettings(), store.Load());
            Assert.IsTrue(File.Exists(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void StartAtSignIn_EnableDisableAndFailureRollbackUseActualState()
    {
        var registry = new FakeStartupRegistry();
        var service = new StartAtSignInService(registry, @"C:\Apps\FunHabitTracker.Desktop.exe");
        Assert.IsFalse(service.IsEnabled);
        service.SetEnabled(true);
        Assert.IsTrue(service.IsEnabled);
        StringAssert.EndsWith(registry.Value!, "--background");
        service.SetEnabled(false);
        Assert.IsFalse(service.IsEnabled);

        registry.Value = "previous";
        Assert.IsFalse(service.IsEnabled, "A stale command must not be reported as enabled.");
        registry.FailWrites = true;
        Assert.ThrowsExactly<UnauthorizedAccessException>(() => service.SetEnabled(true));
        Assert.AreEqual("previous", registry.Value);
    }

    private sealed class FakeStartupRegistry : IStartupRegistry
    {
        public string? Value { get; set; }
        public bool FailWrites { get; set; }
        public string? Read(string name) => Value;
        public void Write(string name, string command)
        {
            if (FailWrites) throw new UnauthorizedAccessException();
            Value = command;
        }
        public void Delete(string name) => Value = null;
    }
}
