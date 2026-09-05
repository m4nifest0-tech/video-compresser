using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace VideoCompressor.Services;

public record GpuStat(string Name, double? TemperatureC, double? UtilizationGpuPercent, double? UtilizationMemPercent,
    double? MemoryUsedMb, double? MemoryTotalMb, double? PowerDrawW, double? PowerLimitW, double? FanSpeedPercent);

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
        FillMissingPowerDraw(result);

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
                "--query-gpu=name,temperature.gpu,utilization.gpu,utilization.memory,memory.used,memory.total,power.draw,power.limit,fan.speed");
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
                if (parts.Length < 9) continue;

                stats.Add(new GpuStat(
                    parts[0],
                    ParseDouble(parts[1]),
                    ParseDouble(parts[2]),
                    ParseDouble(parts[3]),
                    ParseDouble(parts[4]),
                    ParseDouble(parts[5]),
                    ParseDouble(parts[6]),
                    ParseDouble(parts[7]),
                    ParseDouble(parts[8])));
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

    /// <summary>
    /// Alcune GPU (anche professionali, non solo laptop) non riportano il consumo istantaneo tramite
    /// il campo "power.draw" della query CSV pur avendo comunque un limite di potenza configurato.
    /// Il consumo reale resta pero' disponibile in "nvidia-smi -q -d POWER", sotto "Power Samples",
    /// come media delle ultime letture campionate dal driver: la si usa come ripiego solo quando serve,
    /// per evitare di avviare un secondo processo nvidia-smi ad ogni interrogazione.
    /// </summary>
    private static void FillMissingPowerDraw(List<GpuStat> stats)
    {
        if (stats.Count == 0 || !stats.Any(s => s.PowerDrawW == null)) return;

        var samples = QueryPowerSamplesAvg();
        for (int i = 0; i < stats.Count && i < samples.Count; i++)
        {
            if (stats[i].PowerDrawW == null && samples[i] is { } avg)
                stats[i] = stats[i] with { PowerDrawW = avg };
        }
    }

    private static List<double?> QueryPowerSamplesAvg()
    {
        var result = new List<double?>();
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
            psi.ArgumentList.Add("-q");
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add("POWER");

            using var proc = Process.Start(psi);
            if (proc == null) return result;

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);
            if (!proc.HasExited)
            {
                try { proc.Kill(true); } catch { /* ignora */ }
                return result;
            }

            // Ogni GPU e' delimitata da una riga "GPU <indirizzo PCI>", nello stesso ordine
            // restituito dalla query CSV usata altrove in questa classe.
            var blocks = Regex.Split(output, @"(?m)^GPU\s+\S+");
            foreach (var block in blocks.Skip(1))
            {
                var direct = Regex.Match(block, @"Instantaneous Power Draw\s*:\s*([\d.]+)\s*W");
                if (!direct.Success) direct = Regex.Match(block, @"Average Power Draw\s*:\s*([\d.]+)\s*W");
                if (direct.Success)
                {
                    result.Add(ParseDouble(direct.Groups[1].Value));
                    continue;
                }

                var sampled = Regex.Match(block, @"Power Samples[\s\S]*?Avg\s*:\s*([\d.]+)\s*W");
                result.Add(sampled.Success ? ParseDouble(sampled.Groups[1].Value) : null);
            }
        }
        catch
        {
            // fallback silenzioso: si continua a mostrare solo il limite gia' noto dalla query CSV
        }
        return result;
    }

    private static double? ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
}
