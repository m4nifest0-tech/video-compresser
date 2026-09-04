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

    public async Task<DurationResult> GetDurationSecondsAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
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
            if (proc == null) return new DurationResult(null, "Impossibile avviare ffprobe.");

            var stderrTask = proc.StandardError.ReadToEndAsync();
            string output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            string stderr = "";
            try { stderr = await stderrTask; } catch { /* ignora */ }

            if (double.TryParse(output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
                return new DurationResult(seconds, "");

            return new DurationResult(null, string.IsNullOrWhiteSpace(stderr)
                ? "ffprobe non ha restituito una durata valida (file non riconosciuto?)."
                : stderr.Trim());
        }
        catch (OperationCanceledException)
        {
            return new DurationResult(null, "");
        }
        catch (Exception ex)
        {
            return new DurationResult(null, ex.Message);
        }
    }

    /// <summary>
    /// Codifica un breve campione (attorno al 30% della durata) con le impostazioni scelte
    /// e ne misura il bitrate risultante, per stimare la dimensione finale dell'intero file
    /// senza dover attendere la compressione completa.
    /// </summary>
    public async Task<EstimateResult> EstimateOutputSizeAsync(string sourcePath, string codec, int cq,
        double durationSeconds, CancellationToken ct)
    {
        if (durationSeconds <= 0) return new EstimateResult(null, "Durata del video non valida.");

        double sampleSeconds = Math.Min(5, durationSeconds);
        double start = Math.Max(0, durationSeconds * 0.3 - sampleSeconds / 2);
        string tempFile = Path.Combine(Path.GetTempPath(), $"vc_sample_{Guid.NewGuid():N}.mp4");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            string[] args =
            {
                "-y", "-hide_banner", "-loglevel", "error",
                "-ss", start.ToString(CultureInfo.InvariantCulture),
                "-i", sourcePath,
                "-t", sampleSeconds.ToString(CultureInfo.InvariantCulture),
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
                tempFile,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var proc = new Process { StartInfo = psi };
            proc.Start();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            using var registration = ct.Register(() =>
            {
                try { if (!proc.HasExited) proc.Kill(true); }
                catch { /* processo forse gia' terminato */ }
            });

            await proc.WaitForExitAsync(CancellationToken.None);
            string stderrText = "";
            try { stderrText = await stderrTask; } catch { /* ignora */ }

            if (ct.IsCancellationRequested) return new EstimateResult(null, "");

            if (proc.ExitCode != 0)
            {
                string msg = string.IsNullOrWhiteSpace(stderrText)
                    ? $"ffmpeg ha restituito il codice di errore {proc.ExitCode} senza dettagli."
                    : stderrText.Trim();
                return new EstimateResult(null, msg);
            }

            if (!File.Exists(tempFile))
                return new EstimateResult(null, "ffmpeg ha terminato senza errori ma non ha creato il file campione.");

            long sampleBytes = new FileInfo(tempFile).Length;
            return new EstimateResult((long)(sampleBytes / sampleSeconds * durationSeconds), "");
        }
        catch (Exception ex)
        {
            return new EstimateResult(null, ex.Message);
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); }
            catch { /* file forse ancora in uso */ }
        }
    }

    public async Task<CompressResult> CompressAsync(string sourcePath, string destPath, string codec, int cq,
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
        string stderrText = "";
        try { stderrText = await stderrDrainTask; } catch { /* ignora */ }
        return new CompressResult(proc.ExitCode, stderrText.Trim());
    }
}

public record CompressResult(int ExitCode, string StdErr);
public record DurationResult(double? Seconds, string ErrorDetail);
public record EstimateResult(long? Bytes, string ErrorDetail);
