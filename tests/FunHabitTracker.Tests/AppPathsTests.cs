using FunHabitTracker.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed class AppPathsTests
{
    [TestMethod]
    public void ConfiguredRoot_IsIndependentFromWorkingDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-path-{Guid.NewGuid():N}");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FunHabitTracker:DataDirectory"] = root }).Build();
        var original = Environment.CurrentDirectory;
        try
        {
            var first = new AppPaths(configuration).DatabasePath;
            Environment.CurrentDirectory = Path.GetTempPath();
            var second = new AppPaths(configuration).DatabasePath;
            Assert.AreEqual(first, second);
            StringAssert.StartsWith(first, Path.GetFullPath(root));
        }
        finally { Environment.CurrentDirectory = original; }
    }
}
