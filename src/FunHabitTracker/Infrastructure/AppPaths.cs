namespace FunHabitTracker.Infrastructure;

public interface IAppPaths
{
    string Root { get; }
    string DatabasePath { get; }
    string BackupsPath { get; }
    string ImportsPath { get; }
    string LogsPath { get; }
    void EnsureCreated();
}

public sealed class AppPaths : IAppPaths
{
    public AppPaths(IConfiguration configuration)
    {
        var configuredRoot = configuration["FunHabitTracker:DataDirectory"]
            ?? Environment.GetEnvironmentVariable("FUNHABITTRACKER_DATA_DIR");
        Root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FunHabitTracker")
            : Path.GetFullPath(configuredRoot);
    }

    public string Root { get; }
    public string DatabasePath => Path.Combine(Root, "habits.db");
    public string BackupsPath => Path.Combine(Root, "Backups");
    public string ImportsPath => Path.Combine(Root, "Imports");
    public string LogsPath => Path.Combine(Root, "Logs");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(BackupsPath);
        Directory.CreateDirectory(ImportsPath);
        Directory.CreateDirectory(LogsPath);
    }
}
