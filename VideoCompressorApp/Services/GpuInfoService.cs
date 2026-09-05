using System.Diagnostics;
using System.Globalization;

namespace VideoCompressor.Services;

public record GpuStat(string Name, double? TemperatureC, double? UtilizationGpuPercent, double? UtilizationMemPercent,
    double? MemoryUsedMb, double? MemoryTotalMb, double? PowerDrawW, double? PowerLimitW);

/// <summary>
/// Legge le metriche della GPU NVIDIA tramite nvidia-smi (incluso con i driver NVIDIA, nessuna
/// dipendenza aggiuntiva). Il risultato viene tenuto in cache per un secondo perche' avviare
/// nvidia-smi ha un costo non trascurabile e l'interfaccia web lo interroga di continuo.
/// </summary>
public static class GpuInfoService
{
    private static readonly object CacheLock = new();
    private static List<GpuStat>? _cache;
    private static DateTime _cacheTimeUtc;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(1);

    public static List<GpuStat> GetStats()
    {
        lock (CacheLock)
        {
            if (_cache != null && DateTime.UtcNow - _cacheTimeUtc < CacheDuration)
                return _cache;
        }

        var result = QueryNvidiaSmi();

        lock (CacheLock)
        {
            _cache = result;
            _cacheTimeUtc = DateTime.UtcNow;
        }

        return result;
    }

    private static List<GpuStat> QueryNvidiaSmi()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add(
                "--query-gpu=name,temperature.gpu,utilization.gpu,utilization.memory,memory.used,memory.total,power.draw,power.limit");
            psi.ArgumentList.Add("--format=csv,noheader,nounits");

            using var proc = Process.Start(psi);
            if (proc == null) return new List<GpuStat>();

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);
            if (!proc.HasExited)
            {
                try { proc.Kill(true); } catch { /* ignora */ }
                return new List<GpuStat>();
            }

            var stats = new List<GpuStat>();
            foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = rawLine.Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length < 8) continue;

                stats.Add(new GpuStat(
                    parts[0],
                    ParseDouble(parts[1]),
                    ParseDouble(parts[2]),
                    ParseDouble(parts[3]),
                    ParseDouble(parts[4]),
                    ParseDouble(parts[5]),
                    ParseDouble(parts[6]),
                    ParseDouble(parts[7])));
            }
            return stats;
        }
        catch
        {
            // nvidia-smi assente dal PATH o GPU non NVIDIA: si torna una lista vuota,
            // l'interfaccia web mostrera' semplicemente "GPU non disponibile".
            return new List<GpuStat>();
        }
    }

    private static double? ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
}
