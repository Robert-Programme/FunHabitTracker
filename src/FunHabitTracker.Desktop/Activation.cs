using System.IO.Pipes;
using System.Text;

namespace FunHabitTracker.Desktop;

public enum ActivationCommand
{
    ShowCompact,
    ShowFull,
    Quit
}

public static class ActivationProtocol
{
    public const int MaxCommandLength = 32;

    public static bool TryParse(string? value, out ActivationCommand command) =>
        Enum.TryParse(value?.Trim(), ignoreCase: false, out command)
        && Enum.IsDefined(command)
        && value!.Length <= MaxCommandLength;
}

public sealed class SingleInstanceCoordinator : IDisposable
{
    private const string MutexName = "Local\\FunHabitTracker.Desktop.0.2";
    private const string PipeName = "FunHabitTracker.Desktop.Activation.0.2";
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _stop = new();
    private Task? _listener;
    private bool _ownsMutex;

    public SingleInstanceCoordinator()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out _ownsMutex);
    }

    public bool IsOwner => _ownsMutex;

    public async Task<bool> SendAsync(ActivationCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);
            await pipe.ConnectAsync(1500, cancellationToken);
            var payload = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes($"{command}\n");
            await pipe.WriteAsync(payload, cancellationToken);
            await pipe.FlushAsync(cancellationToken);
            DesktopDiagnostics.WriteMessage($"Sent activation command {command}.");
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public void Listen(Func<ActivationCommand, Task> handler)
    {
        if (!IsOwner || _listener is not null) throw new InvalidOperationException("Only the owning process can listen.");
        _listener = Task.Run(async () =>
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    await using var pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await pipe.WaitForConnectionAsync(_stop.Token);
                    using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    var value = await reader.ReadLineAsync(_stop.Token);
                    if (value?.Length <= ActivationProtocol.MaxCommandLength && ActivationProtocol.TryParse(value, out var command))
                    {
                        DesktopDiagnostics.WriteMessage($"Received activation command {command}.");
                        await handler(command);
                    }
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    DesktopDiagnostics.Write(exception);
                }
            }
        });
    }

    public void Dispose()
    {
        _stop.Cancel();
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }
        _mutex.Dispose();
        _stop.Dispose();
    }
}
