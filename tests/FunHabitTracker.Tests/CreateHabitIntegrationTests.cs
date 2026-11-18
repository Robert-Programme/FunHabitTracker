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
