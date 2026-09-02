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

    public record CodecOption(string Label, string Value);
    public record LevelOption(string Label, int Cq);
    public record ThemeOption(string Label, string Value);

    private static readonly CodecOption[] Codecs =
    {
        new("H.264 (massima compatibilita)", "h264_nvenc"),
        new("H.265 / HEVC (file piu piccoli)", "hevc_nvenc"),
        new("AV1 (compressione migliore, richiede RTX serie 40+)", "av1_nvenc"),
    };

    private static readonly LevelOption[] Levels =
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

    public MainWindow()
    {
        InitializeComponent();
        FilesGrid.ItemsSource = _items;

        CodecCombo.ItemsSource = Codecs;
        CodecCombo.SelectedIndex = 0;
        LevelCombo.ItemsSource = Levels;
        LevelCombo.SelectedIndex = 2;

        ThemeModeCombo.ItemsSource = ThemeModes;
        AccentColorCombo.ItemsSource = AccentColors;

        var settings = App.Settings;
        ThemeModeCombo.SelectedItem = ThemeModes.FirstOrDefault(t => t.Value == settings.ThemeMode) ?? ThemeModes[0];
        AccentColorCombo.SelectedItem = AccentColors.FirstOrDefault(a => a.Value == settings.AccentColor) ?? AccentColors[0];
        _themeUiReady = true;
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
        e.Effects = _cts == null && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
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
            DestTextBox.Text = dlg.FolderName;
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

    private async void Estimate_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("Aggiungi almeno un file o una cartella.", "Lista vuota",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!FfmpegService.IsFfmpegAvailable())
        {
            MessageBox.Show("ffmpeg non e stato trovato nel PATH. Installalo e riprova.", "ffmpeg non trovato",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

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
            var duration = await _ffmpeg.GetDurationSecondsAsync(item.SourcePath, _cts.Token);
            var estimate = duration.HasValue
                ? await _ffmpeg.EstimateOutputSizeAsync(item.SourcePath, codec.Value, level.Cq, duration.Value, _cts.Token)
                : null;

            item.EstimatedSize = estimate;
            if (estimate.HasValue && item.OriginalSize > 0)
            {
                totalOriginal += item.OriginalSize;
                totalEstimated += estimate.Value;
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

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("Aggiungi almeno un file o una cartella.", "Lista vuota",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var destDir = DestTextBox.Text.Trim();
        if (string.IsNullOrEmpty(destDir))
        {
            MessageBox.Show("Scegli una cartella di destinazione.", "Destinazione mancante",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!FfmpegService.IsFfmpegAvailable())
        {
            MessageBox.Show("ffmpeg non e stato trovato nel PATH. Installalo e riprova.", "ffmpeg non trovato",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

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
                var duration = await _ffmpeg.GetDurationSecondsAsync(item.SourcePath, _cts.Token);

                CompressResult result;
                try
                {
                    result = await _ffmpeg.CompressAsync(item.SourcePath, dest, codec.Value, level.Cq, duration,
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

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        StatusLabel.Text = "Annullamento in corso...";
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
        CancelButton.IsEnabled = busy;
    }
}
