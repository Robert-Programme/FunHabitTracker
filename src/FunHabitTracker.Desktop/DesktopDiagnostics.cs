namespace FunHabitTracker.Desktop;

public static class DesktopDiagnostics
{
    private static string _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FunHabitTracker");
    public static string Root => _root;
    public static string LogPath => Path.Combine(Root, "Logs", "desktop.log");

    public static void ConfigureRoot(string root) => _root = Path.GetFullPath(root);

    public static void Write(Exception exception)
    {
        TryAppend($"[{DateTime.UtcNow:O}] {exception}\n\n");
    }

    public static void WriteMessage(string message)
    {
        TryAppend($"[{DateTime.UtcNow:O}] {message}\n");
    }

    private static void TryAppend(string value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, value);
        }
        catch (IOException)
        {
            // Diagnostics must never interrupt activation or shutdown.
        }
        catch (UnauthorizedAccessException)
        {
            // The application remains functional when its optional log is unavailable.
        }
    }
}
