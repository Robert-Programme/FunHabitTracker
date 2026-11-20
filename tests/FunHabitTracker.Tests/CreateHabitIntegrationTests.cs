using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using FunHabitTracker.Controllers;
using FunHabitTracker.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public class CreateHabitIntegrationTests
{
    private HabitApplication application;
    private HttpClient client;

    [TestInitialize]
    public void Setup()
    {
        application = new HabitApplication();
        client = application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new System.Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    [TestCleanup]
    public void Cleanup()
    {
        client?.Dispose();
        application?.Dispose();
    }

    [TestMethod]
    public async Task Create_Get_RendersForm()
    {
        var response = await client.GetAsync("/Habits/Create");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "name=\"Name\"");
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(201)]
    public async Task Create_InvalidPost_RendersErrorsWithoutSaving(int nameLength)
    {
        var response = await PostForm(new string('x', nameLength));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "field-validation-error");
        using var scope = application.Services.CreateScope();
        Assert.AreEqual(0, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Habits.CountAsync());
    }

    [TestMethod]
    public async Task Create_ValidPost_PersistsAndRedirectsToList()
    {
        var response = await PostForm("Read daily");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/Habits", response.Headers.Location.OriginalString);
        using var scope = application.Services.CreateScope();
        var habit = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Habits.SingleAsync();
        Assert.AreEqual("Read daily", habit.Name);
        Assert.AreEqual("Daily", habit.Frequency);
        Assert.IsTrue(habit.IsActive);
        var list = await client.GetStringAsync("/Habits");
        StringAssert.Contains(list, "Read daily");
    }

    [TestMethod]
    public async Task Create_WithoutAntiforgeryToken_RejectsPost()
    {
        var response = await client.PostAsync("/Habits/Create", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Name"] = "Read daily" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = application.Services.CreateScope();
        Assert.AreEqual(0, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Habits.CountAsync());
    }

    [TestMethod]
    public async Task Today_WithoutScriptsSection_StillRenders()
    {
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/Home/Today")).StatusCode);
    }

    [TestMethod]
    public async Task QuickCreate_OverlongName_RejectsWithoutSaving()
    {
        var response = await PostQuick(new string('x', 201), "Daily");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = application.Services.CreateScope();
        Assert.AreEqual(0, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Habits.CountAsync());
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task QuickCreate_BlankFrequency_UsesDaily(string frequency)
    {
        var response = await PostQuick("Read", frequency);
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = application.Services.CreateScope();
        Assert.AreEqual("Daily", (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Habits.SingleAsync()).Frequency);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(200)]
    public async Task QuickCreate_NameBoundary_IsValidated(int length)
    {
        var response = await PostQuick(new string('x', length), "Daily");
        Assert.AreEqual(length == 0 ? HttpStatusCode.BadRequest : HttpStatusCode.Redirect, response.StatusCode);
        using var scope = application.Services.CreateScope();
        Assert.AreEqual(length == 0 ? 0 : 1, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Habits.CountAsync());
    }

    private async Task<HttpResponseMessage> PostQuick(string name, string frequency)
    {
        var html = await client.GetStringAsync("/Home/Today");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.IsTrue(token.Success);
        var values = new Dictionary<string, string> {
            ["name"] = name,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
        };
        if (frequency != null) values["frequency"] = frequency;
        return await client.PostAsync("/Home/AddHabit", new FormUrlEncodedContent(values));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("invalid")]
    [DataRow("-1")]
    [DataRow("99999")]
    public async Task Toggle_InvalidId_ReturnsControlledResponse(string id)
    {
        var html = await client.GetStringAsync("/Home/Today");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value) };
        if (id != null) values["habitId"] = id;
        var response = await client.PostAsync("/Home/ToggleToday", new FormUrlEncodedContent(values));
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = application.Services.CreateScope();
        Assert.AreEqual(0, await scope.ServiceProvider.GetRequiredService<AppDbContext>().HabitCompletions.CountAsync());
    }

    private async Task<HttpResponseMessage> PostForm(string name)
    {
        var html = await client.GetStringAsync("/Habits/Create");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.IsTrue(token.Success, "Create form must include an antiforgery token");
        return await client.PostAsync("/Habits/Create", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["IsActive"] = "true",
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
            }));
    }

    [DataTestMethod]
    [DataRow("Development")]
    [DataRow("Production")]
    public async Task Create_ValidationScripts_AreLocalAndLoadInDependencyOrder(string environment)
    {
        using var app = new HabitApplication(environment);
        using var browser = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new System.Uri("https://localhost")
        });
        var html = await browser.GetStringAsync("/Habits/Create");
        var previous = -1;
        foreach (var path in new[] {
            "/lib/jquery/dist/jquery.min.js",
            "/lib/jquery-validation/dist/jquery.validate.min.js",
            "/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js" })
        {
            var response = await browser.GetAsync(path);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, path);
            var script = Regex.Match(html, "src=\"" + Regex.Escape(path) + @"(?:\?[^""\r\n]*)?""");
            Assert.IsTrue(script.Success && script.Index > previous, "Missing or out-of-order script: " + path + " in " + string.Join(";", System.Linq.Enumerable.Select(Regex.Matches(html, "<script[^>]+>"), m => m.Value)));
            previous = script.Index;
        }
        Assert.IsFalse(html.Contains("cdnjs.cloudflare.com"), "Form must work without CDN access");
    }

    private sealed class HabitApplication : WebApplicationFactory<HomeController>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly string environment;

        public HabitApplication(string environment = "Development") => this.environment = environment;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            builder.UseEnvironment(environment);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) connection.Dispose();
        }
    }
}
