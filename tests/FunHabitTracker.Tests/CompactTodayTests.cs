using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed partial class CompactTodayTests
{
    private string _root = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-compact-{Guid.NewGuid():N}");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("FunHabitTracker:DataDirectory", _root));
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client.Dispose();
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public async Task CompactToday_RendersFreeDayWithAccessibleLocalShell()
    {
        var response = await _client.GetAsync("/Home/Compact");
        var html = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(html, "Heute ist frei");
        StringAssert.Contains(html, "aria-label=\"Heute kompakt\"");
        StringAssert.Contains(html, "Ganze App öffnen");
        Assert.IsFalse(html.Contains("aria-label=\"Hauptnavigation\"", StringComparison.Ordinal));
        Assert.IsFalse(ExternalAssetRegex().IsMatch(html));
    }

    [TestMethod]
    public async Task CompactToday_ServerFormsTransitionEveryOutcomeAndReturnToCompact()
    {
        var createHtml = await GetHtmlAsync("/Habits/Create");
        var create = await PostAsync("/Habits/Create", createHtml, new()
        {
            ["Name"] = "Compact habit",
            ["StartDate"] = DateTime.Today.ToString("yyyy-MM-dd"),
            ["RecurrenceType"] = "Daily"
        });
        var id = IdRegex().Match(create.Headers.Location?.ToString() ?? string.Empty).Groups[1].Value;
        Assert.IsFalse(string.IsNullOrWhiteSpace(id));

        var compact = await GetHtmlAsync("/Home/Compact");
        StringAssert.Contains(compact, "Compact habit");
        var done = await PostAsync("/Home/SetOutcome", compact, new() { ["habitId"] = id, ["outcome"] = "Done", ["returnTo"] = "Compact" });
        Assert.AreEqual("/Home/Compact", done.Headers.Location?.ToString());
        StringAssert.Contains(await GetHtmlAsync("/Home/Compact"), "Compact habit erledigt\" aria-pressed=\"true");

        compact = await GetHtmlAsync("/Home/Compact");
        await PostAsync("/Home/SetOutcome", compact, new() { ["habitId"] = id, ["outcome"] = "Done", ["returnTo"] = "Compact" });
        StringAssert.Contains(await GetHtmlAsync("/Home/Compact"), "Compact habit offen\" aria-pressed=\"true");

        compact = await GetHtmlAsync("/Home/Compact");
        await PostAsync("/Home/SetOutcome", compact, new() { ["habitId"] = id, ["outcome"] = "Skipped", ["returnTo"] = "Compact" });
        StringAssert.Contains(await GetHtmlAsync("/Home/Compact"), "Compact habit ueberspringen\" aria-pressed=\"true");

        compact = await GetHtmlAsync("/Home/Compact");
        await PostAsync("/Home/SetOutcome", compact, new() { ["habitId"] = id, ["outcome"] = "Skipped", ["returnTo"] = "Compact" });
        StringAssert.Contains(await GetHtmlAsync("/Home/Compact"), "Compact habit offen\" aria-pressed=\"true");

        compact = await GetHtmlAsync("/Home/Compact");
        await PostAsync("/Home/SetOutcome", compact, new() { ["habitId"] = id, ["outcome"] = "Done", ["returnTo"] = "Compact" });
        compact = await GetHtmlAsync("/Home/Compact");
        await PostAsync("/Home/SetOutcome", compact, new() { ["habitId"] = id, ["outcome"] = "Skipped", ["returnTo"] = "Compact" });
        StringAssert.Contains(await GetHtmlAsync("/Home/Compact"), "Compact habit ueberspringen\" aria-pressed=\"true");

        compact = await GetHtmlAsync("/Home/Compact");
        await PostAsync("/Home/ClearOutcome", compact, new() { ["habitId"] = id, ["returnTo"] = "Compact" });
        StringAssert.Contains(await GetHtmlAsync("/Home/Compact"), "Compact habit offen\" aria-pressed=\"true");

        compact = await GetHtmlAsync("/Home/Compact");
        await PostAsync("/Home/ClearOutcome", compact, new() { ["habitId"] = id, ["returnTo"] = "Compact" });
        StringAssert.Contains(await GetHtmlAsync("/Home/Compact"), "Compact habit offen\" aria-pressed=\"true");
    }

    [TestMethod]
    public async Task CompactToday_InvalidOutcomeDoesNotCreateState()
    {
        var tokenPage = await GetHtmlAsync("/Habits/Create");
        var response = await PostAsync("/Home/SetOutcome", tokenPage, new() { ["habitId"] = "999", ["outcome"] = "Done", ["returnTo"] = "Compact" });
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        var compact = await GetHtmlAsync("/Home/Compact");
        StringAssert.Contains(compact, "Heute ist frei");
        StringAssert.Contains(compact, "Das Habit ist fuer heute nicht verfuegbar.");
    }

    [TestMethod]
    public async Task CompactToday_OutcomePostRequiresAntiForgeryToken()
    {
        var response = await _client.PostAsync("/Home/SetOutcome", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["habitId"] = "1",
            ["outcome"] = "Done",
            ["returnTo"] = "Compact"
        }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> GetHtmlAsync(string path)
    {
        var response = await _client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, html);
        return html;
    }

    private Task<HttpResponseMessage> PostAsync(string path, string html, Dictionary<string, string> values)
    {
        values["__RequestVerificationToken"] = WebUtility.HtmlDecode(TokenRegex().Match(html).Groups[1].Value);
        return _client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();
    [GeneratedRegex("/Habits/History/(\\d+)")]
    private static partial Regex IdRegex();
    [GeneratedRegex("(?:src|href)=\"https?://", RegexOptions.IgnoreCase)]
    private static partial Regex ExternalAssetRegex();
}
