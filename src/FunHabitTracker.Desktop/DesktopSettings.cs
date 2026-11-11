using System.Text.Json;
using Microsoft.Win32;

namespace FunHabitTracker.Desktop;

public sealed record DesktopSettings(int Version = 1, double? Left = null, double? Top = null, double Width = 420, double Height = 620);

public sealed class DesktopSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public DesktopSettingsStore(string root) => _path = Path.Combine(root, "desktop-settings.json");

    public DesktopSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new DesktopSettings();
            var settings = JsonSerializer.Deserialize<DesktopSettings>(File.ReadAllText(_path), JsonOptions);
            return settings is { Version: 1, Width: >= 320 and <= 1200, Height: >= 400 and <= 1200 }
                ? settings
                : new DesktopSettings();
        }
        catch (JsonException)
        {
            return new DesktopSettings();
        }
    }

    public async Task SaveAsync(DesktopSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(settings, JsonOptions), cancellationToken);
            File.Move(temporary, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

public interface IStartupRegistry
{
    string? Read(string name);
    void Write(string name, string command);
    void Delete(string name);
}

public sealed class WindowsStartupRegistry : IStartupRegistry
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public string? Read(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(name) as string;
    }

    public void Write(string name, string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        key.SetValue(name, command, RegistryValueKind.String);
    }

    public void Delete(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}

public sealed class StartAtSignInService
{
    public const string RegistrationName = "FunHabitTracker";
    private readonly IStartupRegistry _registry;
    private readonly string _executablePath;

    public StartAtSignInService(IStartupRegistry registry, string executablePath)
    {
        _registry = registry;
        _executablePath = Path.GetFullPath(executablePath);
    }

    public bool IsEnabled => string.Equals(_registry.Read(RegistrationName), ExpectedCommand, StringComparison.OrdinalIgnoreCase);
    public string ExpectedCommand => $"\"{_executablePath}\" --background";

    public void SetEnabled(bool enabled)
    {
        var previous = _registry.Read(RegistrationName);
        try
        {
            if (enabled) _registry.Write(RegistrationName, ExpectedCommand);
            else _registry.Delete(RegistrationName);
            if (IsEnabled != enabled) throw new InvalidOperationException("Windows did not retain the requested start-at-sign-in state.");
        }
        catch
        {
            if (previous is null) _registry.Delete(RegistrationName);
            else _registry.Write(RegistrationName, previous);
            throw;
        }
    }
}
