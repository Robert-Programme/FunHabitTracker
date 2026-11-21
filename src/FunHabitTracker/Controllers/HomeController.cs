using System.Diagnostics;
using FunHabitTracker.Infrastructure;
using FunHabitTracker.Models;
using FunHabitTracker.Services;
using FunHabitTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FunHabitTracker.Controllers;

public sealed class HomeController : Controller
{
    private readonly DailyTrackingService _tracking;
    private readonly ILocalDateProvider _localDate;

    public HomeController(DailyTrackingService tracking, ILocalDateProvider localDate)
    {
        _tracking = tracking;
        _localDate = localDate;
    }

    public IActionResult Index() => RedirectToAction(nameof(Today));

    public async Task<IActionResult> Today(CancellationToken cancellationToken)
    {
        return View(await _tracking.GetTodayAsync(_localDate.Today, cancellationToken));
    }

    public async Task<IActionResult> Compact(CancellationToken cancellationToken)
    {
        return View(await _tracking.GetTodayAsync(_localDate.Today, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetOutcome(int habitId, HabitOutcome outcome, string? returnTo, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(outcome) || !await _tracking.ToggleOutcomeAsync(habitId, _localDate.Today, outcome, cancellationToken))
        {
            TempData["Error"] = "Das Habit ist fuer heute nicht verfuegbar.";
        }
        return RedirectAfterOutcome(returnTo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearOutcome(int habitId, string? returnTo, CancellationToken cancellationToken)
    {
        if (!await _tracking.ClearOutcomeAsync(habitId, _localDate.Today, cancellationToken))
        {
            TempData["Error"] = "Das Habit wurde nicht gefunden.";
        }
        return RedirectAfterOutcome(returnTo);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private IActionResult RedirectAfterOutcome(string? returnTo) =>
        string.Equals(returnTo, nameof(Compact), StringComparison.Ordinal)
            ? RedirectToAction(nameof(Compact))
            : RedirectToAction(nameof(Today));
}
