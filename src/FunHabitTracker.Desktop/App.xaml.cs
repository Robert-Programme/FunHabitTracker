using System.Windows;

namespace FunHabitTracker.Desktop;

public partial class App : System.Windows.Application
{
    private DesktopCoordinator? _coordinator;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            _coordinator = new DesktopCoordinator(e.Args);
            if (!await _coordinator.StartAsync())
            {
                Shutdown();
            }
        }
        catch (Exception exception)
        {
            DesktopDiagnostics.Write(exception);
            System.Windows.MessageBox.Show(
                $"Fun Habit Tracker konnte nicht gestartet werden.\n\n{exception.Message}\n\nProtokoll: {DesktopDiagnostics.LogPath}",
                "Startfehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e) => base.OnExit(e);
}
