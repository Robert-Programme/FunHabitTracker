using FunHabitTracker.Infrastructure;
using FunHabitTracker.Models;
using FunHabitTracker.Services;
using FunHabitTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FunHabitTracker.Controllers;

public sealed class HabitsController : Controller
{
    private readonly HabitService _habits;
    private readonly DailyTrackingService _tracking;
    private readonly ILocalDateProvider _localDate;

    public HabitsController(HabitService habits, DailyTrackingService tracking, ILocalDateProvider localDate)
    {
        _habits = habits;
        _tracking = tracking;
        _localDate = localDate;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await _habits.ListAsync(_localDate.Today, cancellationToken));

    public IActionResult Create() => View(new HabitInputModel { StartDate = _localDate.Today });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HabitInputModel model, int[] weekdays, CancellationToken cancellationToken)
    {
        RevalidateSchedule(model, weekdays);
        if (!ModelState.IsValid) return View(model);
        var id = await _habits.CreateAsync(model, cancellationToken);
        TempData["Success"] = "Habit wurde erstellt.";
        return RedirectToAction(nameof(History), new { id });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _habits.GetInputAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HabitInputModel model, int[] weekdays, CancellationToken cancellationToken)
    {
        RevalidateSchedule(model, weekdays);
        if (!ModelState.IsValid) return View(model);
        if (!await _habits.UpdateAsync(model, cancellationToken)) return NotFound();
        TempData["Success"] = "Aenderungen wurden gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        if (!await _habits.ArchiveAsync(id, cancellationToken)) return NotFound();
        TempData["Success"] = "Habit wurde archiviert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        if (!await _habits.RestoreAsync(id, cancellationToken)) return NotFound();
        TempData["Success"] = "Habit ist wieder aktiv.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> History(int id, CancellationToken cancellationToken)
    {
        var model = await _tracking.GetHistoryAsync(id, _localDate.Today, cancellationToken: cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    private void RevalidateSchedule(HabitInputModel model, int[] weekdays)
    {
        model.ScheduledDays = model.RecurrenceType == RecurrenceType.Daily
            ? ScheduledDays.EveryDay
            : weekdays.Aggregate(ScheduledDays.None, (current, value) => current | (ScheduledDays)value);
        ModelState.Clear();
        TryValidateModel(model);
    }
}
