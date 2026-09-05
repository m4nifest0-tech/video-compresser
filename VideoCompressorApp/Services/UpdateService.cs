using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace VideoCompressor.Services;

public record UpdateInfo(string Version, string DownloadUrl, long AssetSizeBytes, string ReleaseUrl, string ReleaseNotes);

/// <summary>
/// Controlla e applica gli aggiornamenti leggendo l'ultima release pubblica da GitHub (nessuna
/// autenticazione richiesta, il repo e' pubblico). Poiche' l'app e' un singolo exe portable senza
/// installer, l'aggiornamento non puo' sovrascrivere il file mentre e' in esecuzione: si scarica il
/// nuovo exe altrove, si genera un piccolo script che attende la chiusura del processo corrente,
/// sostituisce il file e riavvia l'app, poi l'app corrente si chiude per liberare il lock sul file.
/// </summary>
public static class UpdateService
{
    private const string RepoOwner = "m4nifest0-tech";
    private const string RepoName = "video-compresser";
    private const string LatestReleaseApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
    private const string AssetName = "VideoCompressor.exe";

    public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public static string CurrentVersionText =>
        $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VideoCompressorApp", CurrentVersionText));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return http;
    }

    /// <summary>Restituisce le informazioni sulla release piu' recente se piu' nuova di quella installata, altrimenti null.</summary>
    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        using var http = CreateClient();
        using var response = await http.GetAsync(LatestReleaseApiUrl, ct);
        if (!response.IsSuccessStatusCode) return null;

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(tagName)) return null;

        var versionText = tagName.TrimStart('v', 'V');
        if (!Version.TryParse(versionText, out var latestVersion)) return null;
        if (latestVersion.CompareTo(CurrentVersion) <= 0) return null;

        string? downloadUrl = null;
        long assetSize = 0;
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.GetProperty("name").GetString() != AssetName) continue;
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                assetSize = asset.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0;
                break;
            }
        }
        if (string.IsNullOrEmpty(downloadUrl)) return null;

        var releaseUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() ?? "" : "";
        var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

        return new UpdateInfo(versionText, downloadUrl, assetSize, releaseUrl, body);
    }

    /// <summary>
    /// Scarica il nuovo exe, prepara lo script di sostituzione e chiude l'app corrente per lasciargli
    /// il posto. Il chiamante deve considerare l'app come "in chiusura" non appena questo metodo ritorna.
    /// </summary>
    public static async Task DownloadAndApplyAsync(UpdateInfo info, IProgress<double>? progress, CancellationToken ct = default)
    {
        var currentExePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExePath))
            throw new InvalidOperationException("Impossibile determinare il percorso dell'eseguibile in esecuzione.");

        var updateDir = Path.Combine(Path.GetTempPath(), "VideoCompressorUpdate");
        Directory.CreateDirectory(updateDir);
        var newExePath = Path.Combine(updateDir, "VideoCompressor.new.exe");

        long downloadedSize;
        using (var http = CreateClient())
        using (var response = await http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? info.AssetSizeBytes;

            await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(newExePath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 1024 * 1024, useAsync: true);

            var buffer = new byte[1024 * 1024];
            long readTotal = 0;
            int read;
            while ((read = await httpStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                readTotal += read;
                if (total > 0) progress?.Report(100.0 * readTotal / total);
            }
            downloadedSize = readTotal;
        }

        var pid = Environment.ProcessId;
        var scriptPath = Path.Combine(updateDir, "apply_update.bat");
        var logPath = Path.Combine(updateDir, "update_log.txt");

        // La copy puo' fallire silenziosamente (antivirus che scansiona il file appena scaricato,
        // rilascio ritardato del lock sull'exe appena chiuso, un'istanza duplicata rimasta aperta...):
        // senza verifica e ritentativi lo script rilanciava semplicemente il vecchio exe senza che
        // nessuno se ne accorgesse, dando l'impressione che l'aggiornamento non avesse effetto. Ora
        // si ritenta piu' a lungo e si verifica la dimensione del file copiato prima di considerarlo
        // riuscito. Il mutex di singola istanza (App.xaml.cs) evita che un'istanza duplicata tenga
        // bloccato il file all'infinito; questi margini piu' ampi coprono i casi restanti (antivirus).
        var scriptContent = $"""
            @echo off
            echo [%date% %time%] Avvio aggiornamento, PID atteso {pid} > "{logPath}"

            set attempts=0
            :wait
            tasklist /FI "PID eq {pid}" /FO CSV /NH 2>nul | findstr /I "{AssetName}" >nul
            if not errorlevel 1 (
              set /a attempts+=1
              if %attempts% GEQ 60 goto waitgaveup
              timeout /t 1 /nobreak >nul
              goto wait
            )
            echo [%date% %time%] Processo terminato dopo %attempts% secondi, attendo rilascio file >> "{logPath}"
            goto proceed

            :waitgaveup
            echo [%date% %time%] Attesa chiusura processo scaduta dopo 60s, procedo comunque >> "{logPath}"

            :proceed
            timeout /t 2 /nobreak >nul

            set copyattempts=0
            :copyloop
            set /a copyattempts+=1
            copy /y "{newExePath}" "{currentExePath}" >nul 2>>"{logPath}"
            for %%A in ("{currentExePath}") do set destsize=%%~zA
            if "%destsize%"=="{downloadedSize}" goto copyok
            echo [%date% %time%] Tentativo di copia %copyattempts% non riuscito (dimensione %destsize%, attesa {downloadedSize}) >> "{logPath}"
            if %copyattempts% GEQ 20 goto copyfailed
            timeout /t 3 /nobreak >nul
            goto copyloop

            :copyok
            echo [%date% %time%] Copia riuscita al tentativo %copyattempts% >> "{logPath}"
            start "" "{currentExePath}"
            del "%~f0"
            exit /b

            :copyfailed
            echo [%date% %time%] Copia fallita dopo %copyattempts% tentativi: aggiornamento annullato >> "{logPath}"
            start "" "{currentExePath}"
            """;
        await File.WriteAllTextAsync(scriptPath, scriptContent, ct);

        Process.Start(new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        });
    }
}
