using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using VideoCompressor.Models;
using VideoCompressor.Services;

namespace VideoCompressor;

public partial class MainWindow : Window
{
    private static readonly string[] VideoExtensions =
        { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".3gp" };

    private const string DefaultDestDir = @"C:\VideoCompressor\Output";

    public record CodecOption(string Label, string Value);
    public record LevelOption(string Label, int Cq);
    public record ThemeOption(string Label, string Value);

    public sealed record ItemDto(string Id, string FileName, long OriginalSize, string OriginalSizeText,
        string Status, double ProgressPercent, string ProgressText, string EstimatedSizeText, string ResultSizeText,
        bool HasResult, bool HasErrorLog, string? ErrorDetail);

    public sealed record StateDto(bool Busy, bool Optimizing, string StatusText, string EtaText, string EstimateSummaryText,
        double OverallProgressValue, double OverallProgressMax, string DestDir, string CodecValue, int LevelCq,
        bool PreserveStructure, bool SkipExisting, bool DeleteSource,
        IReadOnlyList<CodecOption> Codecs, IReadOnlyList<LevelOption> Levels, IReadOnlyList<ItemDto> Items);

    public sealed record SettingsUpdateDto(string? DestDir, string? CodecValue, int? LevelCq,
        bool? PreserveStructure, bool? SkipExisting, bool? DeleteSource);

    public static readonly CodecOption[] Codecs =
    {
        new("H.264 (massima compatibilita)", "h264_nvenc"),
        new("H.265 / HEVC (file piu piccoli)", "hevc_nvenc"),
        new("AV1 (compressione migliore, richiede RTX serie 40+)", "av1_nvenc"),
    };

    public static readonly LevelOption[] Levels =
    {
        new("Qualita massima (file piu grande)", 18),
        new("Alta qualita", 23),
        new("Bilanciato (consigliato)", 28),
        new("Compressione alta", 32),
        new("Compressione massima (file piu piccolo)", 36),
    };

    private static readonly ThemeOption[] ThemeModes =
    {
        new("Chiaro", "Light"),
        new("Scuro", "Dark"),
    };

    private static readonly ThemeOption[] AccentColors =
    {
        new("Blu", "Blue"),
        new("Verde", "Green"),
        new("Viola", "Purple"),
        new("Arancione", "Orange"),
    };

    private readonly ObservableCollection<VideoItem> _items = new();
    private readonly FfmpegService _ffmpeg = new();
    private CancellationTokenSource? _cts;
    private Stopwatch? _currentItemStopwatch;
    private readonly List<double> _completedItemSeconds = new();
    private bool _themeUiReady;
    private WebUiServer? _webUiServer;
    private readonly CancellationTokenSource _gpuPollCts = new();

    public static MainWindow? Current { get; private set; }

    public bool IsBusy => _cts != null;
    public bool IsOptimizing { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        Current = this;
        FilesGrid.ItemsSource = _items;

        CodecCombo.ItemsSource = Codecs;
        CodecCombo.SelectedIndex = 0;
        LevelCombo.ItemsSource = Levels;
        LevelCombo.SelectedIndex = 2;

        ThemeModeCombo.ItemsSource = ThemeModes;
        AccentColorCombo.ItemsSource = AccentColors;

        var settings = App.Settings;
        DestTextBox.Text = string.IsNullOrWhiteSpace(settings.DestDir) ? DefaultDestDir : settings.DestDir;
        ThemeModeCombo.SelectedItem = ThemeModes.FirstOrDefault(t => t.Value == settings.ThemeMode) ?? ThemeModes[0];
        AccentColorCombo.SelectedItem = AccentColors.FirstOrDefault(a => a.Value == settings.AccentColor) ?? AccentColors[0];
        _themeUiReady = true;

        WebUiEnabledCheck.IsChecked = settings.WebUiEnabled;
        WebUiPortTextBox.Text = settings.WebUiPort.ToString();
        WebUiUsernameTextBox.Text = settings.WebUiUsername;
        UpdateWebUiStatusLabel();

        Title = $"Compressore Video (GPU NVENC) - v{UpdateService.CurrentVersionText}";
        VersionLabel.Text = $"Versione installata: {UpdateService.CurrentVersionText}";
        CheckUpdatesOnStartupCheck.IsChecked = settings.CheckUpdatesOnStartup;

        Loaded += async (_, _) =>
        {
            if (settings.WebUiEnabled) await StartWebUiServerAsync();
            if (settings.CheckUpdatesOnStartup) _ = CheckForUpdatesAsync(silent: true);
        };
        Closed += async (_, _) =>
        {
            _gpuPollCts.Cancel();
            if (_webUiServer != null) await _webUiServer.StopAsync();
        };

        _ = PollGpuLoopAsync(_gpuPollCts.Token);
    }

    /// <summary>
    /// nvidia-smi impiega qualche decina/centinaio di ms per rispondere: viene interrogato su un
    /// thread separato (Task.Run) cosi' da non bloccare mai la UI, e il risultato marshalled sul
    /// thread UI solo per l'aggiornamento del testo.
    /// </summary>
    private async Task PollGpuLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            List<GpuStat> stats;
            try { stats = await Task.Run(() => GpuInfoService.GetStats(), ct); }
            catch (OperationCanceledException) { break; }
            catch { stats = new List<GpuStat>(); }

            if (ct.IsCancellationRequested) break;
            GpuStatusLabel.Text = FormatGpuStatus(stats);

            try { await Task.Delay(2000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private static string FormatGpuStatus(List<GpuStat> stats)
    {
        if (stats.Count == 0) return "GPU: non rilevata (nvidia-smi non disponibile)";
        return string.Join("     ", stats.Select(FormatGpuStat));
    }

    private static string FormatGpuStat(GpuStat g)
    {
        var parts = new List<string> { g.Name };
        if (g.TemperatureC is { } t) parts.Add($"{t:0} °C");
        if (g.UtilizationGpuPercent is { } u) parts.Add($"GPU {u:0}%");
        if (g.EncoderUtilizationPercent is { } eu) parts.Add($"Encoder {eu:0}%");
        if (g.EncoderSessionCount is > 0) parts.Add($"{g.EncoderAvgFps ?? 0:0} fps enc");
        if (g.DecoderUtilizationPercent is { } du) parts.Add($"Decoder {du:0}%");
        if (g.MemoryUsedMb is { } mu && g.MemoryTotalMb is { } mt) parts.Add($"Mem {mu:0}/{mt:0} MB");
        if (g.PowerDrawW is { } pw) parts.Add(g.PowerLimitW is { } pl ? $"{pw:0}/{pl:0} W" : $"{pw:0} W");
        else if (g.PowerLimitW is { } plOnly) parts.Add($"-/{plOnly:0} W");
        if (g.FanSpeedPercent is { } f) parts.Add($"Ventola {f:0}%");
        return string.Join("  ·  ", parts);
    }

    private bool _updateCheckInProgress;

    private async Task CheckForUpdatesAsync(bool silent)
    {
        if (_updateCheckInProgress) return;
        _updateCheckInProgress = true;
        CheckUpdatesButton.IsEnabled = false;
        if (!silent) UpdateStatusLabel.Text = "Controllo aggiornamenti in corso...";

        try
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update == null)
            {
                UpdateStatusLabel.Text = silent ? "" : $"Sei gia' aggiornato alla versione piu' recente (v{UpdateService.CurrentVersionText}).";
                return;
            }

            if (IsBusy)
            {
                UpdateStatusLabel.Text = $"E' disponibile la versione v{update.Version}: attendi il termine dell'elaborazione in corso per installarla.";
                return;
            }

            UpdateStatusLabel.Text = $"E' disponibile la versione v{update.Version}.";
            var result = MessageBox.Show(
                $"E' disponibile la versione v{update.Version} (installata: v{UpdateService.CurrentVersionText}).\n\nVuoi scaricarla e installarla ora? L'app si riavviera' automaticamente.",
                "Aggiornamento disponibile", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                await ApplyUpdateAsync(update);
        }
        catch (Exception ex)
        {
            UpdateStatusLabel.Text = silent ? "" : $"Impossibile controllare gli aggiornamenti: {ex.Message}";
        }
        finally
        {
            _updateCheckInProgress = false;
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private async Task ApplyUpdateAsync(UpdateInfo update)
    {
        UpdateStatusLabel.Text = "Download aggiornamento: 0%";
        var progress = new Progress<double>(pct => UpdateStatusLabel.Text = $"Download aggiornamento: {pct:0}%");

        try
        {
            await UpdateService.DownloadAndApplyAsync(update, progress);
        }
        catch (Exception ex)
        {
            UpdateStatusLabel.Text = $"Aggiornamento fallito: {ex.Message}";
            MessageBox.Show($"Impossibile completare l'aggiornamento:\n\n{ex.Message}", "Errore aggiornamento",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Application.Current.Shutdown();
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e) => await CheckForUpdatesAsync(silent: false);

    private void CheckUpdatesOnStartupCheck_Changed(object sender, RoutedEventArgs e)
    {
        App.Settings.CheckUpdatesOnStartup = CheckUpdatesOnStartupCheck.IsChecked == true;
        App.Settings.Save();
    }

    private void UpdateWebUiStatusLabel()
    {
        if (_webUiServer == null)
        {
            WebUiStatusLabel.Text = "Interfaccia web non attiva.";
            return;
        }

        var addresses = string.Join(", ", NetworkInfo.GetLocalIPv4Addresses()
            .Select(ip => $"http://{ip}:{App.Settings.WebUiPort}/"));
        WebUiStatusLabel.Text = string.IsNullOrEmpty(addresses)
            ? $"Interfaccia web attiva sulla porta {App.Settings.WebUiPort}."
            : $"Interfaccia web attiva: {addresses}";
    }

    private async Task StartWebUiServerAsync()
    {
        if (_webUiServer != null) await _webUiServer.StopAsync();
        _webUiServer = null;
        try
        {
            _webUiServer = new WebUiServer(App.Settings.WebUiPort);
            _webUiServer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossibile avviare l'interfaccia web: {ex.Message}", "Errore interfaccia web",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        UpdateWebUiStatusLabel();
    }

    private async Task StopWebUiServerAsync()
    {
        if (_webUiServer != null) await _webUiServer.StopAsync();
        _webUiServer = null;
        UpdateWebUiStatusLabel();
    }

    public void NotifyWebUiServerCrashed(Exception ex)
    {
        _webUiServer = null;
        UpdateWebUiStatusLabel();
        MessageBox.Show($"L'interfaccia web si e' interrotta per un errore imprevisto:\n\n{ex.GetBaseException().Message}",
            "Interfaccia web interrotta", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async void WebUiApply_Click(object sender, RoutedEventArgs e)
    {
        bool enabled = WebUiEnabledCheck.IsChecked == true;
        string username = WebUiUsernameTextBox.Text.Trim();
        string password = WebUiPasswordBox.Password;

        if (!int.TryParse(WebUiPortTextBox.Text.Trim(), out int port) || port is <= 0 or > 65535)
        {
            MessageBox.Show("Porta non valida. Usa un numero tra 1 e 65535.", "Porta non valida",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (enabled && string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Imposta un nome utente per proteggere l'interfaccia web.", "Utente mancante",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (enabled && string.IsNullOrEmpty(App.Settings.WebUiPasswordHash) && string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Imposta una password per proteggere l'interfaccia web.", "Password mancante",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        App.Settings.WebUiEnabled = enabled;
        App.Settings.WebUiPort = port;
        App.Settings.WebUiUsername = username;
        if (!string.IsNullOrEmpty(password))
            App.Settings.WebUiPasswordHash = PasswordHasher.Hash(password);
        App.Settings.Save();
        WebUiPasswordBox.Clear();

        if (enabled) await StartWebUiServerAsync();
        else await StopWebUiServerAsync();
    }

    private void CodecOrLevel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        foreach (var item in _items) item.EstimatedSize = null;
        if (EstimateSummaryLabel != null) EstimateSummaryLabel.Text = "";
    }

    private void ThemeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_themeUiReady) return;
        if (ThemeModeCombo.SelectedItem is not ThemeOption theme) return;
        if (AccentColorCombo.SelectedItem is not ThemeOption accent) return;

        ThemeManager.Apply(theme.Value, accent.Value);
        App.Settings.ThemeMode = theme.Value;
        App.Settings.AccentColor = accent.Value;
        App.Settings.Save();
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m rimanenti";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s rimanenti";
        return $"{Math.Max(0, ts.Seconds)}s rimanenti";
    }

    /// <summary>
    /// Stima il tempo rimanente usando il tempo reale gia' impiegato sul file corrente
    /// (proiettato sulla sua percentuale) piu' la durata media dei file gia' completati
    /// per quelli ancora da iniziare. Piu' precisa della semplice media sull'intero batch,
    /// che viene distorta dall'overhead iniziale di ffprobe/ffmpeg per ogni file.
    /// </summary>
    private void UpdateEta(double currentItemPercent, int itemsRemainingAfterCurrent)
    {
        double avgPerItem = _completedItemSeconds.Count > 0 ? _completedItemSeconds.Average() : 0;

        double? remainingCurrentSeconds = null;
        if (_currentItemStopwatch != null)
        {
            double currentElapsed = _currentItemStopwatch.Elapsed.TotalSeconds;
            if (currentItemPercent >= 1)
                remainingCurrentSeconds = Math.Max(0, currentElapsed / (currentItemPercent / 100.0) - currentElapsed);
            else if (avgPerItem > 0)
                remainingCurrentSeconds = avgPerItem;
        }

        if (remainingCurrentSeconds == null && avgPerItem <= 0)
        {
            EtaLabel.Text = "Stima tempo rimanente: calcolo...";
            return;
        }

        double totalRemaining = (remainingCurrentSeconds ?? 0) + avgPerItem * itemsRemainingAfterCurrent;
        EtaLabel.Text = FormatTimeSpan(TimeSpan.FromSeconds(totalRemaining));
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Multiselect = true, Title = "Seleziona video" };
        if (dlg.ShowDialog() == true)
        {
            foreach (var path in dlg.FileNames)
                _items.Add(new VideoItem(path, null));
        }
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Seleziona cartella" };
        if (dlg.ShowDialog() == true)
        {
            var folder = dlg.FolderName;
            var found = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                .Where(f => VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .ToList();

            if (found.Count == 0)
            {
                MessageBox.Show("Nessun file video trovato in questa cartella.", "Nessun video trovato",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var path in found)
                _items.Add(new VideoItem(path, folder));
        }
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        bool canDrop = _cts == null && e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
        DropOverlay.Visibility = canDrop ? Visibility.Visible : Visibility.Collapsed;
        e.Handled = true;
    }

    private void Window_DragLeave(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
        if (_cts != null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        AddDroppedPaths(paths);
    }

    private void AddDroppedPaths(IEnumerable<string> paths)
    {
        int added = 0;
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var found = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(f => VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => f);
                foreach (var f in found)
                {
                    _items.Add(new VideoItem(f, path));
                    added++;
                }
            }
            else if (File.Exists(path) && VideoExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            {
                _items.Add(new VideoItem(path, null));
                added++;
            }
        }

        if (added == 0)
        {
            MessageBox.Show("Nessun file video trovato tra gli elementi trascinati.", "Nessun video trovato",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static void OpenWithDefaultApp(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossibile aprire il file: {ex.Message}", "Errore apertura file",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is VideoItem item)
            OpenWithDefaultApp(item.SourcePath);
    }

    private void OpenResult_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is VideoItem { DestPath: not null } item)
            OpenWithDefaultApp(item.DestPath);
    }

    private void ShowLog_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not VideoItem item) return;

        var box = new TextBox
        {
            Text = item.ErrorDetail,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(10),
        };

        new Window
        {
            Title = $"Log - {item.FileName}",
            Width = 640,
            Height = 420,
            Owner = this,
            Content = box,
        }.ShowDialog();
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in FilesGrid.SelectedItems.Cast<VideoItem>().ToList())
            _items.Remove(item);
    }

    private void ClearList_Click(object sender, RoutedEventArgs e) => _items.Clear();

    private void BrowseDest_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Seleziona cartella di destinazione" };
        if (dlg.ShowDialog() == true)
            SetDestDir(dlg.FolderName);
    }

    private void SetDestDir(string dir)
    {
        DestTextBox.Text = dir;
        App.Settings.DestDir = dir;
        App.Settings.Save();
    }

    private static string UniqueDestPath(string destPath)
    {
        if (!File.Exists(destPath)) return destPath;
        var dir = Path.GetDirectoryName(destPath)!;
        var name = Path.GetFileNameWithoutExtension(destPath);
        var ext = Path.GetExtension(destPath);
        int i = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            i++;
        } while (File.Exists(candidate));
        return candidate;
    }

    private static string ComputeDest(VideoItem item, string destDir, bool preserveStructure)
    {
        if (preserveStructure && item.BaseRoot != null)
        {
            var rel = Path.GetRelativePath(item.BaseRoot, item.SourcePath);
            var relNoExt = Path.ChangeExtension(rel, ".mp4");
            return Path.Combine(destDir, relNoExt);
        }
        var name = Path.GetFileNameWithoutExtension(item.SourcePath) + ".mp4";
        return Path.Combine(destDir, name);
    }

    public string? ValidateForEstimate()
    {
        if (_items.Count == 0) return "Aggiungi almeno un file o una cartella.";
        if (!FfmpegService.IsFfmpegAvailable()) return "ffmpeg non e stato trovato nel PATH. Installalo e riprova.";
        return null;
    }

    public string? ValidateForOptimize()
    {
        if (_items.Count == 0) return "Aggiungi almeno un file o una cartella.";
        if (!FfmpegService.IsFfmpegAvailable()) return "ffmpeg non e stato trovato nel PATH. Installalo e riprova.";
        return null;
    }

    private async void Optimize_Click(object sender, RoutedEventArgs e)
    {
        var error = ValidateForOptimize();
        if (error != null)
        {
            MessageBox.Show(error, "Impossibile calcolare i valori ottimali", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RunOptimizeAsync();
    }

    public async Task RunOptimizeAsync()
    {
        IsOptimizing = true;
        OptimizeButton.IsEnabled = false;
        StatusLabel.Text = "Analisi dei file in corso...";

        int maxWidth = 0, maxHeight = 0;
        double maxFps = 0, totalDurationSeconds = 0;
        int probed = 0;

        try
        {
            foreach (var item in _items.ToList())
            {
                var probe = await _ffmpeg.ProbeAsync(item.SourcePath, CancellationToken.None);
                if (probe.Width is { } w && probe.Height is { } h)
                {
                    if ((long)w * h > (long)maxWidth * maxHeight) { maxWidth = w; maxHeight = h; }
                    probed++;
                }
                if (probe.Fps is { } f && f > maxFps) maxFps = f;
                if (probe.DurationSeconds is { } d) totalDurationSeconds += d;
            }
        }
        finally
        {
            IsOptimizing = false;
            OptimizeButton.IsEnabled = true;
        }

        if (probed == 0)
        {
            StatusLabel.Text = "Impossibile analizzare i file selezionati (formato non riconosciuto da ffprobe?).";
            return;
        }

        var (codec, level, reason) = ComputeOptimalSettings(maxWidth, maxHeight, maxFps, totalDurationSeconds);
        CodecCombo.SelectedItem = codec;
        LevelCombo.SelectedItem = level;
        StatusLabel.Text = $"Impostazioni ottimali applicate: {codec.Label} · {level.Label}. {reason}";
    }

    /// <summary>
    /// Regola empirica (non una formula scientifica: qualita' percepita vs dimensione e' in parte
    /// soggettiva) orientata al RISPARMIO DI SPAZIO, l'obiettivo dichiarato di questa app - non alla
    /// massima qualita' possibile:
    /// - Codec: HEVC comprime meglio a parita' di qualita' ed e' quasi sempre la scelta giusta oltre
    ///   il 720p; AV1 comprimerebbe ancora meglio ma non viene proposto automaticamente (il supporto
    ///   hardware varia troppo tra schede, meglio un'attivazione manuale consapevole) - lo si segnala
    ///   pero' nel messaggio quando la risoluzione lo renderebbe utile.
    /// - Livello: al contrario di uno schema "massima qualita'", qui la compressione aumenta con la
    ///   risoluzione anziche' diminuire, perche' e' li' che si concentra il risparmio assoluto in byte
    ///   e perche' gli artefatti da compressione sono meno percepibili all'alta densita' di pixel del
    ///   4K. La durata totale del lotto spinge verso ancora piu' compressione se molto lunga (contenere
    ///   la dimensione complessiva); niente sconti di qualita' per fps alti o clip brevi, che
    ///   andrebbero contro l'obiettivo di risparmiare spazio.
    /// Il risultato viene arrotondato al livello disponibile piu' vicino tra i 5 in elenco.
    /// </summary>
    public static (CodecOption Codec, LevelOption Level, string Reason) ComputeOptimalSettings(
        int maxWidth, int maxHeight, double maxFps, double totalDurationSeconds)
    {
        int maxDim = Math.Max(maxWidth, maxHeight);
        var codec = maxDim > 1280 ? Codecs[1] : Codecs[0]; // HEVC oltre il 720p, altrimenti H.264

        double cq = maxDim switch
        {
            <= 1920 => 32, // fino al Full HD: "Compressione alta" e' gia' un buon compromesso
            <= 2560 => 35, // 1440p/2K: piu' byte in gioco, l'artefatto si nota meno alla stessa densita' di pixel
            _ => 36,       // 4K e oltre: compressione massima, e' dove il risparmio assoluto e' piu' grande
        };
        if (totalDurationSeconds >= 3600) cq += 2; // batch molto lunghi: ancora piu' compressione

        var level = Levels.OrderBy(l => Math.Abs(l.Cq - cq)).First();

        var reason = $"Basato su risoluzione max {maxWidth}x{maxHeight}, {maxFps:0} fps, durata totale {FormatDurationPlain(totalDurationSeconds)}, orientato al risparmio di spazio.";
        if (maxDim >= 2560)
            reason += " Per risparmiare ulteriore spazio valuta AV1, se la tua GPU lo supporta (RTX serie 40+).";

        return (codec, level, reason);
    }

    private static string FormatDurationPlain(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{Math.Max(0, ts.Seconds)}s";
    }

    private async void Estimate_Click(object sender, RoutedEventArgs e)
    {
        var error = ValidateForEstimate();
        if (error != null)
        {
            MessageBox.Show(error, "Impossibile avviare la stima", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RunEstimateAsync();
    }

    public async Task RunEstimateAsync()
    {
        var codec = (CodecOption)CodecCombo.SelectedItem;
        var level = (LevelOption)LevelCombo.SelectedItem;
        var itemsSnapshot = _items.ToList();

        _cts = new CancellationTokenSource();
        SetBusy(true);
        EstimateSummaryLabel.Text = "";
        OverallProgress.Minimum = 0;
        OverallProgress.Maximum = itemsSnapshot.Count;
        OverallProgress.Value = 0;

        long totalOriginal = 0;
        long totalEstimated = 0;
        int estimatedCount = 0;
        int done = 0;

        foreach (var item in itemsSnapshot)
        {
            if (_cts.Token.IsCancellationRequested) break;

            StatusLabel.Text = $"Stima [{done + 1}/{itemsSnapshot.Count}] {item.FileName}";
            var durationResult = await _ffmpeg.GetDurationSecondsAsync(item.SourcePath, _cts.Token);
            var estimateResult = durationResult.Seconds.HasValue
                ? await _ffmpeg.EstimateOutputSizeAsync(item.SourcePath, codec.Value, level.Cq, durationResult.Seconds.Value, _cts.Token)
                : new EstimateResult(null, durationResult.ErrorDetail);

            item.EstimatedSize = estimateResult.Bytes;
            item.ErrorDetail = estimateResult.Bytes.HasValue ? null : estimateResult.ErrorDetail;
            if (estimateResult.Bytes.HasValue && item.OriginalSize > 0)
            {
                totalOriginal += item.OriginalSize;
                totalEstimated += estimateResult.Bytes.Value;
                estimatedCount++;
            }

            done++;
            OverallProgress.Value = done;
        }

        if (_cts.Token.IsCancellationRequested)
        {
            StatusLabel.Text = "Stima annullata.";
        }
        else if (estimatedCount > 0)
        {
            double change = 100.0 * (1 - (double)totalEstimated / totalOriginal);
            string sign = change >= 0 ? "-" : "+";
            EstimateSummaryLabel.Text =
                $"Stima totale: {VideoItem.FormatSize(totalEstimated)} ({sign}{Math.Abs(change):0}% rispetto a {VideoItem.FormatSize(totalOriginal)}) su {estimatedCount}/{itemsSnapshot.Count} file";
            StatusLabel.Text = "Stima completata.";
        }
        else
        {
            StatusLabel.Text = "Impossibile stimare: verifica che il codec scelto sia supportato dalla GPU.";
        }

        OverallProgress.Value = 0;
        SetBusy(false);
        _cts = null;
    }

    public string? ValidateForStart()
    {
        if (_items.Count == 0) return "Aggiungi almeno un file o una cartella.";
        if (string.IsNullOrEmpty(DestTextBox.Text.Trim())) return "Scegli una cartella di destinazione.";
        if (!FfmpegService.IsFfmpegAvailable()) return "ffmpeg non e stato trovato nel PATH. Installalo e riprova.";
        return null;
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        var error = ValidateForStart();
        if (error != null)
        {
            MessageBox.Show(error, "Impossibile avviare la compressione", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RunCompressionAsync();
    }

    public async Task RunCompressionAsync()
    {
        var destDir = DestTextBox.Text.Trim();
        Directory.CreateDirectory(destDir);

        var codec = (CodecOption)CodecCombo.SelectedItem;
        var level = (LevelOption)LevelCombo.SelectedItem;
        bool preserveStructure = PreserveStructureCheck.IsChecked == true;
        bool skipExisting = SkipExistingCheck.IsChecked == true;
        bool deleteSource = DeleteSourceCheck.IsChecked == true;
        var itemsSnapshot = _items.ToList();

        _cts = new CancellationTokenSource();
        SetBusy(true);
        OverallProgress.Minimum = 0;
        OverallProgress.Maximum = itemsSnapshot.Count;
        OverallProgress.Value = 0;
        _completedItemSeconds.Clear();
        EtaLabel.Text = "Stima tempo rimanente: calcolo...";

        int done = 0;
        foreach (var item in itemsSnapshot)
        {
            if (_cts.Token.IsCancellationRequested)
            {
                item.Status = "Annullato";
                continue;
            }

            item.ErrorDetail = null;
            var dest = ComputeDest(item, destDir, preserveStructure);

            if (File.Exists(dest))
            {
                if (skipExisting)
                {
                    item.Status = "Saltato";
                    item.ProgressPercent = 100;
                    item.DestPath = dest;
                    item.ResultSize = new FileInfo(dest).Length;
                    done++;
                    OverallProgress.Value = done;
                    UpdateEta(0, itemsSnapshot.Count - done);
                    continue;
                }
                dest = UniqueDestPath(dest);
            }

            item.DestPath = dest;
            item.Status = "In corso";
            item.ProgressPercent = 0;
            StatusLabel.Text = $"[{done + 1}/{itemsSnapshot.Count}] {item.FileName}";
            _currentItemStopwatch = Stopwatch.StartNew();
            int itemsAfterThis = itemsSnapshot.Count - done - 1;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                var durationResult = await _ffmpeg.GetDurationSecondsAsync(item.SourcePath, _cts.Token);

                CompressResult result;
                try
                {
                    result = await _ffmpeg.CompressAsync(item.SourcePath, dest, codec.Value, level.Cq, durationResult.Seconds,
                        pct => Dispatcher.Invoke(() =>
                        {
                            item.ProgressPercent = pct;
                            UpdateEta(pct, itemsAfterThis);
                        }),
                        _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    result = new CompressResult(-1, "");
                }

                if (_cts.Token.IsCancellationRequested)
                {
                    item.Status = "Annullato";
                    try { if (File.Exists(dest)) File.Delete(dest); } catch { /* file forse in uso */ }
                }
                else if (result.ExitCode != 0)
                {
                    item.Status = "Errore";
                    item.ErrorDetail = string.IsNullOrWhiteSpace(result.StdErr) ? "ffmpeg ha restituito un errore senza dettagli." : result.StdErr;
                }
                else
                {
                    item.Status = "Completato";
                    item.ProgressPercent = 100;
                    item.ResultSize = File.Exists(dest) ? new FileInfo(dest).Length : null;

                    if (deleteSource)
                    {
                        try { File.Delete(item.SourcePath); }
                        catch (IOException ex) { StatusLabel.Text = $"Impossibile eliminare originale: {ex.Message}"; }
                    }
                }
            }
            catch (Exception ex)
            {
                // Un singolo file guasto (permessi, percorso non valido, disco pieno...) non deve
                // interrompere l'intero batch: lo si segna come errore e si prosegue con il successivo.
                item.Status = "Errore";
                item.ErrorDetail = ex.Message;
            }

            _completedItemSeconds.Add(_currentItemStopwatch.Elapsed.TotalSeconds);
            _currentItemStopwatch = null;

            done++;
            OverallProgress.Value = done;
            UpdateEta(0, itemsSnapshot.Count - done);
        }

        StatusLabel.Text = _cts.Token.IsCancellationRequested ? "Annullato." : "Completato.";
        EtaLabel.Text = "";
        _currentItemStopwatch = null;
        SetBusy(false);
        _cts = null;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Cancel();

    public void Cancel()
    {
        _cts?.Cancel();
        StatusLabel.Text = "Annullamento in corso...";
    }

    /// <summary>
    /// Aggiunge un file caricato dall'interfaccia web alla coda, cosi' da essere trattato
    /// esattamente come un file aggiunto dal desktop (stessa lista, stesse impostazioni).
    /// </summary>
    public void AddUploadedFile(string path) => _items.Add(new VideoItem(path, null));

    public VideoItem? FindItem(Guid id) => _items.FirstOrDefault(i => i.Id == id);

    public bool RemoveItemById(Guid id)
    {
        var item = FindItem(id);
        if (item == null) return false;
        _items.Remove(item);
        return true;
    }

    public string? ApplySettings(SettingsUpdateDto dto)
    {
        if (IsBusy) return "Impossibile modificare le impostazioni mentre una compressione e' in corso.";

        if (dto.CodecValue != null)
        {
            var codec = Codecs.FirstOrDefault(c => c.Value == dto.CodecValue);
            if (codec == null) return $"Codec sconosciuto: {dto.CodecValue}";
            CodecCombo.SelectedItem = codec;
        }

        if (dto.LevelCq.HasValue)
        {
            var level = Levels.FirstOrDefault(l => l.Cq == dto.LevelCq.Value);
            if (level == null) return $"Livello di compressione sconosciuto: {dto.LevelCq.Value}";
            LevelCombo.SelectedItem = level;
        }

        if (dto.DestDir != null) SetDestDir(dto.DestDir);
        if (dto.PreserveStructure.HasValue) PreserveStructureCheck.IsChecked = dto.PreserveStructure.Value;
        if (dto.SkipExisting.HasValue) SkipExistingCheck.IsChecked = dto.SkipExisting.Value;
        if (dto.DeleteSource.HasValue) DeleteSourceCheck.IsChecked = dto.DeleteSource.Value;

        return null;
    }

    public StateDto GetState()
    {
        var codec = (CodecOption)CodecCombo.SelectedItem;
        var level = (LevelOption)LevelCombo.SelectedItem;

        var items = _items.Select(i => new ItemDto(i.Id.ToString(), i.FileName, i.OriginalSize, i.OriginalSizeText,
            i.Status, i.ProgressPercent, i.ProgressText, i.EstimatedSizeText, i.ResultSizeText, i.HasResult,
            i.HasErrorLog, i.ErrorDetail)).ToList();

        return new StateDto(IsBusy, IsOptimizing, StatusLabel.Text, EtaLabel.Text, EstimateSummaryLabel.Text,
            OverallProgress.Value, OverallProgress.Maximum, DestTextBox.Text, codec.Value, level.Cq,
            PreserveStructureCheck.IsChecked == true, SkipExistingCheck.IsChecked == true,
            DeleteSourceCheck.IsChecked == true, Codecs, Levels, items);
    }

    /// <summary>
    /// Blocca i controlli che modificherebbero la lista file (o le impostazioni) mentre
    /// Estimate_Click/Start_Click la stanno enumerando: senza questo, rimuovere o aggiungere
    /// un elemento a meta' elaborazione lancia un'eccezione non gestita e chiude l'app.
    /// </summary>
    private void SetBusy(bool busy)
    {
        AddFilesButton.IsEnabled = !busy;
        AddFolderButton.IsEnabled = !busy;
        RemoveSelectedButton.IsEnabled = !busy;
        ClearListButton.IsEnabled = !busy;
        FilesGrid.IsEnabled = !busy;
        DestTextBox.IsEnabled = !busy;
        BrowseDestButton.IsEnabled = !busy;
        CodecCombo.IsEnabled = !busy;
        LevelCombo.IsEnabled = !busy;
        PreserveStructureCheck.IsEnabled = !busy;
        SkipExistingCheck.IsEnabled = !busy;
        DeleteSourceCheck.IsEnabled = !busy;
        EstimateButton.IsEnabled = !busy;
        StartButton.IsEnabled = !busy;
        OptimizeButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
    }
}
