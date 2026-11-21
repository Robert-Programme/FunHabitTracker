using FunHabitTracker.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Desktop.Tests;

[TestClass]
public sealed class DesktopFailureTests
{
    [TestMethod]
    public async Task InvalidDataRoot_FailsWithoutDeletingTheUserSelectedPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-invalid-root-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(path, "preserve me");
        try
        {
            await Assert.ThrowsExactlyAsync<IOException>(() => HabitTrackerWebApplication.CreateAsync(
                new[] { $"--FunHabitTracker:DataDirectory={path}" },
                LoopbackEndpoint.SelectAvailableUrl(),
                AppContext.BaseDirectory));
            Assert.AreEqual("preserve me", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
