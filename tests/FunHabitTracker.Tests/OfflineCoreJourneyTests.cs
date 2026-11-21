using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed partial class OfflineCoreJourneyTests
{
    private string _root = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-journey-{Guid.NewGuid():N}");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("FunHabitTracker:DataDirectory", _root));
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client.Dispose(); _factory.Dispose(); SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [TestMethod]
    public async Task CoreJourney_WorksThroughServerRenderedForms()
    {
        var createHtml = await GetHtmlAsync("/Habits/Create");
        var create = await PostFormAsync("/Habits/Create", createHtml, new Dictionary<string, string>
        {
            ["Name"] = "A very long habit name that remains readable on narrow screens without changing the layout width",
            ["Description"] = "Offline journey",
            ["StartDate"] = DateTime.Today.ToString("yyyy-MM-dd"),
            ["RecurrenceType"] = "Daily"
        });
        Assert.AreEqual(HttpStatusCode.Redirect, create.StatusCode);
        var historyPath = create.Headers.Location?.ToString();
        Assert.IsNotNull(historyPath);

        var historyHtml = await GetHtmlAsync(historyPath);
        AssertCurrentNavigation(historyHtml, "Habits");
        StringAssert.Contains(historyHtml, "Offline journey");
        StringAssert.Contains(historyHtml, "Zurück zu Heute");
        StringAssert.Contains(historyHtml, "href=\"/Home/Today\"");
        var editPath = HistoryIdRegex().Match(historyPath).Groups[1].Value;
        var editHtml = await GetHtmlAsync($"/Habits/Edit/{editPath}");
        AssertCurrentNavigation(editHtml, "Habits");
        var edit = await PostFormAsync("/Habits/Edit", editHtml, new Dictionary<string, string>
        {
            ["Id"] = editPath,
            ["Name"] = "Offline habit edited",
            ["Description"] = "Still local",
            ["StartDate"] = DateTime.Today.ToString("yyyy-MM-dd"),
            ["RecurrenceType"] = "Daily"
        });
        Assert.AreEqual(HttpStatusCode.Redirect, edit.StatusCode);

        var todayHtml = await GetHtmlAsync("/Home/Today");
        AssertCurrentNavigation(todayHtml, "Heute");
        StringAssert.Contains(todayHtml, "Offline habit edited erledigen");
        var done = await PostFormAsync("/Home/SetOutcome", todayHtml, new Dictionary<string, string> { ["habitId"] = editPath, ["outcome"] = "Done" });
        Assert.AreEqual(HttpStatusCode.Redirect, done.StatusCode);
        StringAssert.Contains(await GetHtmlAsync("/Home/Today"), "1 von 1 erledigt");

        todayHtml = await GetHtmlAsync("/Home/Today");
        StringAssert.Contains(todayHtml, "Offline habit edited wieder als offen markieren");
        var pending = await PostFormAsync("/Home/SetOutcome", todayHtml, new Dictionary<string, string> { ["habitId"] = editPath, ["outcome"] = "Done" });
        Assert.AreEqual(HttpStatusCode.Redirect, pending.StatusCode);
        StringAssert.Contains(await GetHtmlAsync("/Home/Today"), "0 von 1 erledigt");

        var backupHtml = await GetHtmlAsync("/Backup");
        AssertCurrentNavigation(backupHtml, "Daten");
        var export = await PostFormAsync("/Backup/ExportJson", backupHtml, new Dictionary<string, string>());
        Assert.AreEqual(HttpStatusCode.OK, export.StatusCode);
        var json = await export.Content.ReadAsByteArrayAsync();
        Assert.IsGreaterThan(50, json.Length);

        backupHtml = await GetHtmlAsync("/Backup");
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(GetToken(backupHtml)), "__RequestVerificationToken");
        var file = new ByteArrayContent(json); file.Headers.ContentType = new MediaTypeHeaderValue("application/json"); multipart.Add(file, "file", "backup.json");
        var preview = await _client.PostAsync("/Backup/PreviewJson", multipart);
        var previewHtml = await preview.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, preview.StatusCode);
        StringAssert.Contains(previewHtml, "Import bestaetigen");
        var importToken = ImportTokenRegex().Match(previewHtml).Groups[1].Value;
        var confirm = await PostFormAsync("/Backup/ConfirmJson", previewHtml, new Dictionary<string, string> { ["token"] = importToken });
        Assert.AreEqual(HttpStatusCode.Redirect, confirm.StatusCode);

        var habitsHtml = await GetHtmlAsync("/Habits");
        AssertCurrentNavigation(habitsHtml, "Habits");
        var archive = await PostFormAsync("/Habits/Archive", habitsHtml, new Dictionary<string, string> { ["id"] = editPath });
        Assert.AreEqual(HttpStatusCode.Redirect, archive.StatusCode);
        StringAssert.Contains(await GetHtmlAsync("/Habits"), "Archiviert");
    }

    [TestMethod]
    public async Task CorePages_ReferenceOnlyLocalRuntimeAssetsAndExposeAccessibleShell()
    {
        foreach (var path in new[] { "/Home/Today", "/Habits", "/Habits/Create", "/Backup" })
        {
            var html = await GetHtmlAsync(path);
            Assert.IsFalse(ExternalAssetRegex().IsMatch(html), $"External runtime asset found on {path}");
            var response = await _client.GetAsync(path);
            StringAssert.Contains(response.Headers.GetValues("Content-Security-Policy").Single(), "connect-src 'self'");
            StringAssert.Contains(html, "<html lang=\"de\">");
            StringAssert.Contains(html, "name=\"viewport\"");
            StringAssert.Contains(html, "aria-label=\"Hauptnavigation\"");
        }
    }

    private async Task<string> GetHtmlAsync(string path)
    {
        var response = await _client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, html);
        return html;
    }

    private Task<HttpResponseMessage> PostFormAsync(string path, string html, IReadOnlyDictionary<string, string> values)
    {
        var fields = values.Concat(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", GetToken(html)) });
        return _client.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    private static string GetToken(string html)
    {
        var match = AntiForgeryRegex().Match(html);
        Assert.IsTrue(match.Success, "Anti-forgery token was not rendered");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static void AssertCurrentNavigation(string html, string expectedSection)
    {
        var currentLinks = CurrentNavigationRegex().Matches(html);
        Assert.HasCount(1, currentLinks);
        Assert.AreEqual(expectedSection, currentLinks[0].Groups[1].Value);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiForgeryRegex();
    [GeneratedRegex("/Habits/History/(\\d+)")]
    private static partial Regex HistoryIdRegex();
    [GeneratedRegex("name=\"token\" value=\"([a-f0-9]{32})\"")]
    private static partial Regex ImportTokenRegex();
    [GeneratedRegex("(?:src|href)=\"https?://", RegexOptions.IgnoreCase)]
    private static partial Regex ExternalAssetRegex();
    [GeneratedRegex("<a[^>]*aria-current=\"page\"[^>]*>([^<]+)</a>")]
    private static partial Regex CurrentNavigationRegex();
}
