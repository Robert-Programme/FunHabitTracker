using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed class ApplicationSmokeTests
{
    private string _root = null!;
    private WebApplicationFactory<Program> _factory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-web-{Guid.NewGuid():N}");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("FunHabitTracker:DataDirectory", _root));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _factory.Dispose(); SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [TestMethod]
    [DataRow("/Home/Today")]
    [DataRow("/Home/Compact")]
    [DataRow("/Habits")]
    [DataRow("/Habits/Create")]
    [DataRow("/Backup")]
    public async Task NavigationTarget_RendersSuccessfully(string path)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, html);
        StringAssert.Contains(html, "Fun Habit Tracker");
    }

    [TestMethod]
    [DataRow("/Home/Today", "Heute")]
    [DataRow("/Habits", "Habits")]
    [DataRow("/Habits/Create", "Habits")]
    [DataRow("/Backup", "Daten")]
    public async Task FullNavigation_MarksExactlyOneCurrentSection(string path, string expectedSection)
    {
        using var client = _factory.CreateClient();
        var html = await client.GetStringAsync(path);
        var currentLinks = Regex.Matches(html, "<a[^>]*aria-current=\"page\"[^>]*>([^<]+)</a>");

        Assert.HasCount(1, currentLinks);
        Assert.AreEqual(expectedSection, currentLinks[0].Groups[1].Value);
    }
}
