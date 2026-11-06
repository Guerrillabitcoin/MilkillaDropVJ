using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using MilkillaDropVJ.Services;
using MilkillaDropVJ.Services.Spout;

namespace MilkillaDropVJ;

/// <summary>
/// Hito 1: lanzar BeatDrop como proceso externo y ver su salida Spout dentro de la aplicación.
/// Código de ventana a propósito sencillo; la arquitectura MVVM llega después de este hito.
/// </summary>
public partial class MainWindow : Window
{
    private const int MaxLogLines = 300;

    private readonly AppSettings _settings = AppSettings.Load();
    private readonly BeatDropService _beatDrop = new();
    private readonly SpoutReceiver _receiver = new();
    private readonly DispatcherTimer _frameTimer;
    private readonly DispatcherTimer _statusTimer;
    private readonly List<string> _logLines = new();

    private bool _loading = true;
    private bool _updatingSenders;
    private bool _wasRunning;
    private string? _currentSender;
    private SpoutSenderInfo? _lastInfo;
    private string? _lastError;
    private DateTime _retryAfter = DateTime.MinValue;
    private int _framesThisSecond;

    public MainWindow()
    {
        InitializeComponent();

        _frameTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _frameTimer.Tick += FrameTimer_Tick;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _statusTimer.Tick += StatusTimer_Tick;

        _beatDrop.StateChanged += (_, _) => Dispatcher.BeginInvoke(new Action(UpdateBeatDropUi));
    }

    // ---------- Arranque y cierre ----------

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_settings.BeatDropPath))
        {
            var guess = AppSettings.GuessBeatDropPath();
            if (guess != null)
            {
                _settings.BeatDropPath = guess;
                Log($"BeatDrop encontrado en {guess}");
            }
        }

        PathBox.Text = _settings.BeatDropPath;
        CloseOnExitBox.IsChecked = _settings.CloseBeatDropOnExit;

        if (_beatDrop.TryAttachExisting(_settings.BeatDropPath))
        {
            Log("BeatDrop ya estaba abierto; lo vigilo desde aquí.");
        }

        UpdateBeatDropUi();
        RefreshSenders();
        _loading = false;

        _frameTimer.Start();
        _statusTimer.Start();
        Log("Listo. Lanza BeatDrop y pon música para ver la imagen por Spout.");
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _frameTimer.Stop();
        _statusTimer.Stop();

        _settings.BeatDropPath = PathBox.Text.Trim();
        TrySaveSettings();

        if (_settings.CloseBeatDropOnExit)
        {
            _beatDrop.StopBlocking();
        }

        _receiver.Dispose();
        _beatDrop.Dispose();
    }

    // ---------- BeatDrop ----------

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Elige el ejecutable de BeatDrop",
            Filter = "BeatDrop|BeatDrop*.exe|Ejecutables (*.exe)|*.exe",
        };
        if (File.Exists(PathBox.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(PathBox.Text);
        }
        if (dialog.ShowDialog(this) == true)
        {
            PathBox.Text = dialog.FileName;
            _settings.BeatDropPath = dialog.FileName;
            TrySaveSettings();
        }
    }

    private void PathBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _settings.BeatDropPath = PathBox.Text.Trim();
        TrySaveSettings();
    }

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text.Trim();
        _settings.BeatDropPath = path;
        TrySaveSettings();

        try
        {
            _beatDrop.Start(path);
            Log($"BeatDrop lanzado: {path}");
        }
        catch (Exception ex)
        {
            Log("No se pudo lanzar BeatDrop: " + ex.Message);
            MessageBox.Show(this, "No se pudo lanzar BeatDrop.\n\n" + ex.Message,
                "Milkilla Drop VJ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        try
        {
            await _beatDrop.StopAsync();
        }
        catch (Exception ex)
        {
            Log("No se pudo cerrar BeatDrop: " + ex.Message);
        }
        UpdateBeatDropUi();
    }

    private void ToggleSpout_Click(object sender, RoutedEventArgs e)
    {
        if (_beatDrop.SendToggleSpout())
        {
            Log("Enviado Ctrl+Z a BeatDrop (alternar Spout).");
        }
        else
        {
            Log("BeatDrop no está abierto o aún no tiene ventana.");
        }
    }

    private void CloseOnExit_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }
        _settings.CloseBeatDropOnExit = CloseOnExitBox.IsChecked == true;
        TrySaveSettings();
    }

    private void UpdateBeatDropUi()
    {
        bool running = _beatDrop.IsRunning;
        BeatDropStatus.Text = running ? $"En marcha (PID {_beatDrop.ProcessId})" : "Parado";
        LaunchButton.IsEnabled = !running;
        StopButton.IsEnabled = running;
        ToggleSpoutButton.IsEnabled = running;

        if (_wasRunning && !running)
        {
            Log("BeatDrop se ha cerrado.");
        }
        _wasRunning = running;
    }

    // ---------- Spout ----------

    private void SenderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingSenders || SenderBox.SelectedItem is not string chosen)
        {
            return;
        }
        _settings.SpoutSender = chosen;
        TrySaveSettings();
        SelectSender(chosen);
        Log($"Emisor elegido a mano: {chosen}");
    }

    private void AutoSender_Click(object sender, RoutedEventArgs e)
    {
        _settings.SpoutSender = "";
        TrySaveSettings();
        Log("Elección de emisor en automático.");
        RefreshSenders();
    }

    private void RefreshSenders()
    {
        var live = SpoutDirectory.GetLiveSenderNames();

        string? target = null;
        if (!string.IsNullOrEmpty(_settings.SpoutSender) && live.Contains(_settings.SpoutSender))
        {
            target = _settings.SpoutSender;
        }
        else
        {
            var active = SpoutDirectory.GetActiveSenderName();
            target = live.FirstOrDefault(n => n.Contains("BeatDrop", StringComparison.OrdinalIgnoreCase))
                     ?? (active != null && live.Contains(active) ? active : null)
                     ?? live.FirstOrDefault();
        }

        _updatingSenders = true;
        try
        {
            var current = SenderBox.Items.Cast<string>().ToList();
            if (!current.SequenceEqual(live))
            {
                SenderBox.Items.Clear();
                foreach (var name in live)
                {
                    SenderBox.Items.Add(name);
                }
            }
            SenderBox.SelectedItem = target;
        }
        finally
        {
            _updatingSenders = false;
        }

        SelectSender(target);
    }

    private void SelectSender(string? name)
    {
        if (name == _currentSender)
        {
            return;
        }

        if (name == null)
        {
            Log($"El emisor Spout «{_currentSender}» ha desaparecido.");
        }
        else
        {
            Log($"Recibiendo Spout de «{name}».");
        }

        _currentSender = name;
        _lastInfo = null;
        _lastError = null;
        _retryAfter = DateTime.MinValue;
        _receiver.ReleaseTextures();
        if (name == null)
        {
            ShowNoSignal("Sin señal Spout");
        }
    }

    private void FrameTimer_Tick(object? sender, EventArgs e)
    {
        if (_currentSender == null)
        {
            return;
        }
        if (DateTime.Now < _retryAfter)
        {
            return;
        }

        var info = SpoutDirectory.GetSenderInfo(_currentSender);
        if (info == null)
        {
            ShowNoSignal("Sin señal Spout");
            return;
        }

        try
        {
            if (_receiver.Receive(info))
            {
                _framesThisSecond++;
                if (!ReferenceEquals(PreviewImage.Source, _receiver.Bitmap))
                {
                    PreviewImage.Source = _receiver.Bitmap;
                }
                NoSignalText.Visibility = Visibility.Collapsed;
            }
            _lastInfo = info;
            _lastError = null;
        }
        catch (Exception ex)
        {
            if (ex.Message != _lastError)
            {
                Log($"Error leyendo Spout de «{info.Name}»: {ex.Message}");
                _lastError = ex.Message;
            }
            _receiver.ReleaseTextures();
            _retryAfter = DateTime.Now.AddSeconds(1);
            ShowNoSignal("Error al leer Spout (mira el registro)");
        }
    }

    private void StatusTimer_Tick(object? sender, EventArgs e)
    {
        RefreshSenders();
        UpdateBeatDropUi();

        if (_currentSender == null)
        {
            SpoutStatus.Text = _beatDrop.IsRunning
                ? "No hay emisores Spout. Si BeatDrop no envía, pulsa «Alternar Spout»."
                : "No hay emisores Spout.";
        }
        else if (_lastInfo != null)
        {
            SpoutStatus.Text =
                $"{_lastInfo.Width}×{_lastInfo.Height} · {_receiver.FormatName} · {_framesThisSecond} fps en la vista previa";
            SenderBox.ToolTip = string.IsNullOrEmpty(_lastInfo.Description) ? null : _lastInfo.Description;
        }
        else
        {
            SpoutStatus.Text = _lastError ?? "Conectando…";
        }
        _framesThisSecond = 0;
    }

    private void ShowNoSignal(string text)
    {
        NoSignalText.Text = text;
        NoSignalText.Visibility = Visibility.Visible;
        PreviewImage.Source = null;
    }

    // ---------- Utilidades ----------

    private void TrySaveSettings()
    {
        try
        {
            _settings.Save();
        }
        catch (Exception ex)
        {
            Log("No se pudieron guardar los ajustes: " + ex.Message);
        }
    }

    private void Log(string message)
    {
        _logLines.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        if (_logLines.Count > MaxLogLines)
        {
            _logLines.RemoveRange(0, _logLines.Count - MaxLogLines);
        }
        LogBox.Text = string.Join(Environment.NewLine, _logLines);
        LogBox.ScrollToEnd();
    }
}
