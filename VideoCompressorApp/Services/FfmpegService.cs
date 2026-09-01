using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace VideoCompressor.Services;

public class FfmpegService
{
    private static readonly Regex TimeRegex = new(@"out_time=(\d+):(\d+):(\d+)\.(\d+)", RegexOptions.Compiled);

    public static bool IsFfmpegAvailable() => FindOnPath("ffmpeg.exe") != null || FindOnPath("ffmpeg") != null;

    private static string? FindOnPath(string exe)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            try
            {
                var full = Path.Combine(dir, exe);
                if (File.Exists(full)) return full;
            }
            catch
            {
                // ignora voci di PATH non valide
            }
        }
        return null;
    }

    public async Task<double?> GetDurationSecondsAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-v");
            psi.ArgumentList.Add("error");
            psi.ArgumentList.Add("-show_entries");
            psi.ArgumentList.Add("format=duration");
            psi.ArgumentList.Add("-of");
            psi.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
            psi.ArgumentList.Add(filePath);

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            string output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            if (double.TryParse(output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
                return seconds;
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<int> CompressAsync(string sourcePath, string destPath, string codec, int cq,
        double? durationSeconds, Action<double> onProgress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        string[] args =
        {
            "-y", "-hide_banner", "-loglevel", "error",
            "-i", sourcePath,
            "-c:v", codec,
            "-preset", "p7",
            "-rc", "vbr",
            "-cq", cq.ToString(CultureInfo.InvariantCulture),
            "-b:v", "0",
            "-spatial-aq", "1",
            "-temporal-aq", "1",
            "-aq-strength", "8",
            "-rc-lookahead", "20",
            "-c:a", "aac",
            "-b:a", "160k",
            "-movflags", "+faststart",
            "-progress", "pipe:1",
            "-nostats",
            destPath,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.Start();

        // Drena stderr in background: se nessuno lo legge, il buffer del pipe puo
        // riempirsi e bloccare ffmpeg a meta compressione (deadlock).
        var stderrDrainTask = proc.StandardError.ReadToEndAsync();

        using var registration = ct.Register(() =>
        {
            try
            {
                if (!proc.HasExited) proc.Kill(true);
            }
            catch
            {
                // il processo potrebbe essere gia terminato
            }
        });

        string? line;
        while ((line = await proc.StandardOutput.ReadLineAsync()) != null)
        {
            if (ct.IsCancellationRequested) break;
            var match = TimeRegex.Match(line);
            if (match.Success && durationSeconds is > 0)
            {
                int h = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                int m = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                int s = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                double frac = double.Parse("0." + match.Groups[4].Value, CultureInfo.InvariantCulture);
                double elapsed = h * 3600 + m * 60 + s + frac;
                double pct = Math.Clamp(elapsed / durationSeconds.Value * 100.0, 0, 100);
                onProgress(pct);
            }
        }

        await proc.WaitForExitAsync(CancellationToken.None);
        try { await stderrDrainTask; } catch { /* ignora */ }
        return proc.ExitCode;
    }
}
