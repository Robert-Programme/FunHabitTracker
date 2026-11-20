using System;
using System.IO;
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
public class StartupTests
{
    [TestMethod]
    public void UnusableSqlitePath_PreventsStartup()
    {
        var directory = Path.Combine(Path.GetTempPath(), "habit-startup-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            using var application = new BrokenDatabaseApplication(directory);
            Assert.ThrowsException<SqliteException>(() =>
            {
                using var client = application.CreateClient();
            });
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private sealed class BrokenDatabaseApplication : WebApplicationFactory<HomeController>
    {
        private readonly string directory;
        public BrokenDatabaseApplication(string directory) => this.directory = directory;
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = directory }.ToString()));
            });
        }
    }
}
