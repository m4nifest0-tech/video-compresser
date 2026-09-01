using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
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

    private static readonly CodecOption[] Codecs =
    {
        new("H.264 (massima compatibilita)", "h264_nvenc"),
        new("H.265 / HEVC (file piu piccoli)", "hevc_nvenc"),
    };

    private static readonly LevelOption[] Levels =
    {
        new("Qualita massima (file piu grande)", 18),
        new("Alta qualita", 23),
        new("Bilanciato (consigliato)", 28),
        new("Compressione alta", 32),
        new("Compressione massima (file piu piccolo)", 36),
    };

    private readonly ObservableCollection<VideoItem> _items = new();
    private readonly FfmpegService _ffmpeg = new();
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        FilesGrid.ItemsSource = _items;

        CodecCombo.ItemsSource = Codecs;
        CodecCombo.SelectedIndex = 0;
        LevelCombo.ItemsSource = Levels;
        LevelCombo.SelectedIndex = 2;
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

        _cts = new CancellationTokenSource();
        StartButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        OverallProgress.Minimum = 0;
        OverallProgress.Maximum = _items.Count;
        OverallProgress.Value = 0;

        int done = 0;
        foreach (var item in _items)
        {
            if (_cts.Token.IsCancellationRequested)
            {
                item.Status = "Annullato";
                continue;
            }

            var dest = ComputeDest(item, destDir, preserveStructure);

            if (File.Exists(dest))
            {
                if (skipExisting)
                {
                    item.Status = "Saltato";
                    item.ProgressPercent = 100;
                    done++;
                    OverallProgress.Value = done;
                    continue;
                }
                dest = UniqueDestPath(dest);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            item.DestPath = dest;
            item.Status = "In corso";
            item.ProgressPercent = 0;
            StatusLabel.Text = $"[{done + 1}/{_items.Count}] {item.FileName}";

            var duration = await _ffmpeg.GetDurationSecondsAsync(item.SourcePath, _cts.Token);

            int exitCode;
            try
            {
                exitCode = await _ffmpeg.CompressAsync(item.SourcePath, dest, codec.Value, level.Cq, duration,
                    pct => Dispatcher.Invoke(() => item.ProgressPercent = pct),
                    _cts.Token);
            }
            catch (OperationCanceledException)
            {
                exitCode = -1;
            }

            if (_cts.Token.IsCancellationRequested)
            {
                item.Status = "Annullato";
                try { if (File.Exists(dest)) File.Delete(dest); } catch { /* file forse in uso */ }
            }
            else if (exitCode != 0)
            {
                item.Status = "Errore";
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

            done++;
            OverallProgress.Value = done;
        }

        StatusLabel.Text = _cts.Token.IsCancellationRequested ? "Annullato." : "Completato.";
        StartButton.IsEnabled = true;
        CancelButton.IsEnabled = false;
        _cts = null;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        StatusLabel.Text = "Annullamento in corso...";
    }
}
