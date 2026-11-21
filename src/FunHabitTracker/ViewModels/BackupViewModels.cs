namespace FunHabitTracker.ViewModels;

public sealed record ImportPreviewModel(string Token, int HabitCount, int OutcomeCount, DateTime ExportedAtUtc);

public sealed class BackupPageModel
{
    public ImportPreviewModel? Preview { get; init; }
    public required string DataDirectory { get; init; }
}
