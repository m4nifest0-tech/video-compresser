using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace VideoCompressor.Models;

public class VideoItem : INotifyPropertyChanged
{
    public string SourcePath { get; }
    public string? BaseRoot { get; }
    public long OriginalSize { get; }
    public string? DestPath { get; set; }

    public string FileName => Path.GetFileName(SourcePath);
    public string OriginalSizeText => FormatSize(OriginalSize);

    private string _status = "In attesa";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
    }

    private double _progressPercent;
    public double ProgressPercent
    {
        get => _progressPercent;
        set { _progressPercent = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
    }

    public string ProgressText => Status is "In corso" or "Completato" or "Saltato" ? $"{ProgressPercent:0}%" : "";

    private long? _resultSize;
    public long? ResultSize
    {
        get => _resultSize;
        set { _resultSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(ResultSizeText)); }
    }

    public string ResultSizeText => ResultSize.HasValue ? FormatSize(ResultSize.Value) : "-";

    public VideoItem(string sourcePath, string? baseRoot)
    {
        SourcePath = sourcePath;
        BaseRoot = baseRoot;
        OriginalSize = File.Exists(sourcePath) ? new FileInfo(sourcePath).Length : 0;
    }

    public static string FormatSize(long bytes)
    {
        double size = bytes;
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        while (size >= 1024 && i < units.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:0.0} {units[i]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
