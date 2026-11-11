using System.Text.Json;
using FunHabitTracker.Infrastructure;
using FunHabitTracker.Services;
using FunHabitTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FunHabitTracker.Controllers;

public sealed class BackupController : Controller
{
    private readonly BackupService _backup;
    private readonly IAppPaths _paths;
    public BackupController(BackupService backup, IAppPaths paths) { _backup = backup; _paths = paths; }

    public IActionResult Index() => View(new BackupPageModel { DataDirectory = _paths.Root });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task ExportJson(CancellationToken cancellationToken)
    {
        Response.ContentType = "application/json";
        Response.Headers.ContentDisposition = $"attachment; filename=fun-habit-tracker-{DateTime.UtcNow:yyyyMMdd-HHmm}.json";
        await _backup.WriteJsonExportAsync(Response.Body, cancellationToken);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> PreviewJson(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var preview = await _backup.StageJsonImportAsync(stream, file.Length, cancellationToken);
            return View("Index", new BackupPageModel { DataDirectory = _paths.Root, Preview = preview });
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmJson(string token, CancellationToken cancellationToken)
    {
        try { await _backup.ImportJsonAsync(token, cancellationToken); TempData["Success"] = "JSON-Backup wurde wiederhergestellt."; }
        catch (Exception exception) when (exception is InvalidDataException or FileNotFoundException or DbUpdateException) { TempData["Error"] = exception.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportDatabase(CancellationToken cancellationToken)
    {
        var path = await _backup.CreateDatabaseBackupAsync("manual", cancellationToken);
        var bytes = await System.IO.File.ReadAllBytesAsync(path, cancellationToken);
        return File(bytes, "application/vnd.sqlite3", Path.GetFileName(path));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(260 * 1024 * 1024)]
    public async Task<IActionResult> RestoreDatabase(IFormFile file, CancellationToken cancellationToken)
    {
        try { await using var stream = file.OpenReadStream(); await _backup.RestoreDatabaseAsync(stream, file.Length, cancellationToken); TempData["Success"] = "SQLite-Backup wurde wiederhergestellt."; }
        catch (Exception exception) when (exception is InvalidDataException or SqliteException or IOException) { TempData["Error"] = exception.Message; }
        return RedirectToAction(nameof(Index));
    }
}
