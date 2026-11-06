using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MilkillaDropVJ.Services;

/// <summary>
/// Lanza, vigila y cierra BeatDrop como proceso externo.
/// El evento <see cref="StateChanged"/> puede llegar desde otro hilo.
/// </summary>
public sealed class BeatDropService : IDisposable
{
    private Process? _process;

    public event EventHandler? StateChanged;

    public bool IsRunning
    {
        get
        {
            try { return _process is { HasExited: false }; }
            catch (InvalidOperationException) { return false; }
        }
    }

    public int? ProcessId => IsRunning ? _process!.Id : null;

    /// <summary>Lanza BeatDrop con su propia carpeta como directorio de trabajo (presets, beatdrop.ini).</summary>
    public void Start(string exePath)
    {
        if (IsRunning)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            throw new FileNotFoundException("No encuentro BeatDrop en esa ruta.", exePath);
        }

        var startInfo = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? "",
            UseShellExecute = true,
        };

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows no ha devuelto el proceso de BeatDrop.");
        Attach(process);
    }

    /// <summary>Si BeatDrop ya estaba abierto (lanzado a mano), lo adopta.</summary>
    public bool TryAttachExisting(string exePath)
    {
        if (IsRunning)
        {
            return true;
        }
        var name = string.IsNullOrWhiteSpace(exePath) ? "BeatDrop" : Path.GetFileNameWithoutExtension(exePath);
        var existing = Process.GetProcessesByName(name).FirstOrDefault();
        if (existing == null)
        {
            return false;
        }
        Attach(existing);
        return true;
    }

    private void Attach(Process process)
    {
        _process?.Dispose();
        _process = process;
        try
        {
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) => StateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // Sin permiso para vigilarlo: seguimos sin evento de salida.
        }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Pide a BeatDrop que se cierre; si no responde en 4 s, lo mata.</summary>
    public async Task StopAsync()
    {
        var process = _process;
        if (process == null || !IsRunning)
        {
            return;
        }
        try
        {
            process.CloseMainWindow();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill();
        }
        catch (InvalidOperationException)
        {
            // Ya había salido.
        }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Versión bloqueante para el cierre de la aplicación.</summary>
    public void StopBlocking()
    {
        var process = _process;
        if (process == null || !IsRunning)
        {
            return;
        }
        try
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(3000))
            {
                process.Kill();
            }
        }
        catch (InvalidOperationException)
        {
            // Ya había salido.
        }
    }

    /// <summary>
    /// Pone BeatDrop delante y le manda Ctrl+Z, el atajo con el que BeatDrop activa o desactiva su salida Spout.
    /// </summary>
    public bool SendToggleSpout()
    {
        var process = _process;
        if (process == null || !IsRunning)
        {
            return false;
        }
        process.Refresh();
        var hwnd = process.MainWindowHandle;
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        SetForegroundWindow(hwnd);
        Thread.Sleep(150);
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_Z, 0, 0, UIntPtr.Zero);
        keybd_event(VK_Z, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        return true;
    }

    public void Dispose()
    {
        _process?.Dispose();
        _process = null;
    }

    private const byte VK_CONTROL = 0x11;
    private const byte VK_Z = 0x5A;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
