using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace VideoCompressor.Services;

/// <summary>
/// Piccolo server web (Kestrel), eseguito nello stesso processo della finestra WPF, che espone
/// coda, impostazioni, upload/download e metriche GPU in modo che un altro PC sulla rete locale
/// possa pilotare la stessa compressione mostrata a schermo. Le mutazioni della coda condivisa
/// (_items) devono avvenire sul thread UI: ogni handler che le tocca passa quindi da
/// Dispatch/Application.Current.Dispatcher.
///
/// L'accesso e' protetto da una pagina di login (invece della finestra Basic Auth del browser)
/// con una sessione tenuta in memoria (cookie opaco -> scadenza), persa ai riavvii del server web.
/// </summary>
public class WebUiServer
{
    private const string SessionCookieName = "vc_session";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private const int MaxLoginAttempts = 5;
    private static readonly TimeSpan LoginAttemptWindow = TimeSpan.FromMinutes(5);

    private readonly int _port;
    private WebApplication? _app;
    private readonly ConcurrentDictionary<string, DateTime> _sessions = new();
    private readonly ConcurrentDictionary<string, (int Count, DateTime WindowStartUtc)> _loginAttempts = new();

    public WebUiServer(int port)
    {
        _port = port;
    }

    public void Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o =>
        {
            o.ListenAnyIP(_port);
            o.Limits.MaxRequestBodySize = null; // i video possono superare di gran lunga il limite di default (30 MB)
        });

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (path == "/login" || path == "/logout")
            {
                await next();
                return;
            }

            if (IsSessionValid(context.Request))
            {
                await next();
                return;
            }

            if (path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Sessione scaduta, effettua di nuovo l'accesso." });
                return;
            }

            context.Response.Redirect("/login");
        });

        app.MapGet("/login", () => Results.Content(LoginHtml.Content, "text/html; charset=utf-8"));

        app.MapPost("/login", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (IsRateLimited(ip)) return Results.Redirect("/login?error=locked");

            var settings = App.Settings;
            bool ok = !string.IsNullOrEmpty(settings.WebUiUsername) && !string.IsNullOrEmpty(settings.WebUiPasswordHash)
                && username == settings.WebUiUsername && PasswordHasher.Verify(password, settings.WebUiPasswordHash);

            if (!ok)
            {
                RegisterFailedLogin(ip);
                return Results.Redirect("/login?error=1");
            }

            _loginAttempts.TryRemove(ip, out _);
            SetSessionCookie(context);
            return Results.Redirect("/");
        });

        app.MapPost("/logout", (HttpContext context) =>
        {
            if (context.Request.Cookies.TryGetValue(SessionCookieName, out var token))
                _sessions.TryRemove(token, out _);
            context.Response.Cookies.Delete(SessionCookieName);
            return Results.Ok();
        });

        app.MapGet("/", () => Results.Content(IndexHtml.Content, "text/html; charset=utf-8"));

        app.MapGet("/api/state", () => Dispatch(() => Results.Json(MainWindow.Current!.GetState())));

        app.MapGet("/api/gpu", () => Results.Json(GpuInfoService.GetStats()));

        // Endpoint diagnostico: legge e scarta il corpo della richiesta senza mai scrivere su disco,
        // cosi' da misurare la velocita' di rete pura (Client -> Kestrel) isolata dalla velocita' del
        // disco di destinazione. Utile per capire se un upload lento e' un problema di rete o di I/O.
        // Uso: curl -X POST --data-binary @video.mp4 http://host:porta/api/diag/throughput -w "%{speed_upload} B/s"
        app.MapPost("/api/diag/throughput", async (HttpRequest request) =>
        {
            var sw = Stopwatch.StartNew();
            var buffer = new byte[1024 * 1024];
            long total = 0;
            int read;
            while ((read = await request.Body.ReadAsync(buffer)) > 0) total += read;
            sw.Stop();

            double seconds = sw.Elapsed.TotalSeconds;
            double mbPerSec = seconds > 0 ? total / (1024.0 * 1024.0) / seconds : 0;
            return Results.Json(new { bytes = total, seconds, mbPerSec });
        });

        app.MapPost("/api/estimate", () => Dispatch(() =>
        {
            var mw = MainWindow.Current!;
            var error = mw.ValidateForEstimate();
            if (error != null) return Results.BadRequest(new { error });
            if (mw.IsBusy) return Results.Conflict(new { error = "Operazione gia' in corso." });
            mw.Dispatcher.BeginInvoke(new Action(async () => await mw.RunEstimateAsync()));
            return Results.Ok(new { started = true });
        }));

        app.MapPost("/api/optimize", () => Dispatch(() =>
        {
            var mw = MainWindow.Current!;
            var error = mw.ValidateForOptimize();
            if (error != null) return Results.BadRequest(new { error });
            if (mw.IsBusy) return Results.Conflict(new { error = "Operazione gia' in corso." });
            mw.Dispatcher.BeginInvoke(new Action(async () => await mw.RunOptimizeAsync()));
            return Results.Ok(new { started = true });
        }));

        app.MapPost("/api/start", () => Dispatch(() =>
        {
            var mw = MainWindow.Current!;
            var error = mw.ValidateForStart();
            if (error != null) return Results.BadRequest(new { error });
            if (mw.IsBusy) return Results.Conflict(new { error = "Operazione gia' in corso." });
            mw.Dispatcher.BeginInvoke(new Action(async () => await mw.RunCompressionAsync()));
            return Results.Ok(new { started = true });
        }));

        app.MapPost("/api/cancel", () => Dispatch(() =>
        {
            MainWindow.Current!.Cancel();
            return Results.Ok();
        }));

        app.MapPost("/api/settings", async (HttpRequest request) =>
        {
            var dto = await request.ReadFromJsonAsync<MainWindow.SettingsUpdateDto>();
            if (dto == null) return Results.BadRequest();
            return Dispatch(() =>
            {
                var error = MainWindow.Current!.ApplySettings(dto);
                return error != null ? Results.BadRequest(new { error }) : Results.Ok();
            });
        });

        app.MapDelete("/api/items/{id:guid}", (Guid id) => Dispatch(() =>
        {
            var mw = MainWindow.Current!;
            if (mw.IsBusy) return Results.Conflict(new { error = "Impossibile modificare la lista durante l'elaborazione." });
            return mw.RemoveItemById(id) ? Results.Ok() : Results.NotFound();
        }));

        app.MapGet("/api/download/{id:guid}", (Guid id) => Dispatch(() =>
        {
            var item = MainWindow.Current!.FindItem(id);
            if (item?.DestPath == null || !File.Exists(item.DestPath)) return Results.NotFound();
            var stream = File.OpenRead(item.DestPath);
            return Results.File(stream, "video/mp4", Path.GetFileName(item.DestPath), enableRangeProcessing: true);
        }));

        app.MapPost("/api/upload", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest(new { error = "Richiesta non valida." });

            var boundary = GetMultipartBoundary(request.ContentType);
            if (boundary == null) return Results.BadRequest(new { error = "Richiesta non valida." });

            var uploadDir = Path.Combine(Path.GetTempPath(), "VideoCompressorUploads");
            Directory.CreateDirectory(uploadDir);

            // Si legge il corpo multipart a mano invece di usare Request.ReadFormAsync(): quest'ultimo
            // ha un limite di default di 128 MB e comunque bufferizza ogni file su un file temporaneo
            // prima di restituirlo, raddoppiando le scritture su disco. Con MultipartReader ogni file
            // viene invece copiato in streaming direttamente nella destinazione finale, indispensabile
            // per i video da centinaia di MB o alcuni GB che questa app gestisce normalmente.
            var reader = new MultipartReader(boundary, request.Body, bufferSize: 1024 * 1024) { BodyLengthLimit = null };
            var savedPaths = new List<string>();

            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)) continue;
                if (!disposition.IsFileDisposition()) continue;

                var safeName = Path.GetFileName(disposition.FileName.Value ?? "");
                if (string.IsNullOrWhiteSpace(safeName)) continue;

                var dest = Path.Combine(uploadDir, $"{Guid.NewGuid():N}_{safeName}");

                // File.Create() apre lo stream con un buffer di 4 KB e I/O sincrono: per un video
                // da qualche GB significa centinaia di migliaia di scritture minuscole. Un buffer
                // da 1 MB in modalita' asincrona/sequenziale riduce drasticamente le chiamate al
                // filesystem e velocizza sensibilmente l'upload di file di grandi dimensioni.
                const int copyBufferSize = 1024 * 1024;
                await using (var stream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: copyBufferSize, options: FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await section.Body.CopyToAsync(stream, copyBufferSize);
                }

                if (new FileInfo(dest).Length <= 0) { File.Delete(dest); continue; }
                savedPaths.Add(dest);
            }

            if (savedPaths.Count == 0) return Results.BadRequest(new { error = "Nessun file valido ricevuto." });

            return Dispatch(() =>
            {
                foreach (var path in savedPaths) MainWindow.Current!.AddUploadedFile(path);
                return Results.Ok(new { added = savedPaths.Count });
            });
        });

        _app = app;
        app.RunAsync().ContinueWith(t =>
        {
            // Se Kestrel si ferma per un'eccezione non gestita, l'app WPF continuerebbe a girare
            // senza che nessuno se ne accorga: un avviso e' meglio di un'interfaccia web silenziosamente morta.
            if (t.Exception == null) return;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() => MainWindow.Current?.NotifyWebUiServerCrashed(t.Exception)));
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Deve essere await-ata dal thread UI, mai bloccata con GetAwaiter().GetResult(): lo shutdown
    /// di Kestrel attende che le richieste in corso finiscano, e queste (tramite Dispatch) hanno
    /// bisogno proprio del thread UI per completare. Bloccarlo qui li farebbe attendere a vicenda
    /// all'infinito (l'app si "impianta" cliccando Applica con l'interfaccia web aperta altrove).
    /// </summary>
    public async Task StopAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            if (_app != null) await _app.StopAsync(cts.Token);
        }
        catch { /* server gia' in chiusura o timeout di spegnimento */ }
        _app = null;
        _sessions.Clear();
        _loginAttempts.Clear();
    }

    /// <summary>
    /// Limita l'attesa sul thread UI: se e' occupato piu' di 5s (es. proprio durante uno stop/riavvio
    /// del server) la richiesta fallisce con 503 invece di restare appesa indefinitamente.
    /// </summary>
    private static IResult Dispatch(Func<IResult> func)
    {
        var result = (IResult?)Application.Current!.Dispatcher.Invoke((Delegate)func, TimeSpan.FromSeconds(5));
        return result ?? Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    private static string? GetMultipartBoundary(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType) || !MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            return null;
        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        return string.IsNullOrWhiteSpace(boundary) ? null : boundary;
    }

    private void SetSessionCookie(HttpContext context)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = DateTime.UtcNow + SessionLifetime;
        context.Response.Cookies.Append(SessionCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            Secure = false, // interfaccia pensata per rete locale in HTTP semplice
            Expires = DateTimeOffset.UtcNow + SessionLifetime,
            Path = "/",
        });
    }

    private bool IsSessionValid(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(SessionCookieName, out var token) || string.IsNullOrEmpty(token))
            return false;
        if (!_sessions.TryGetValue(token, out var expiry)) return false;
        if (expiry < DateTime.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Blocco basilare anti brute-force: dopo troppi tentativi falliti dallo stesso IP entro
    /// la finestra temporale, i login vengono rifiutati finche' la finestra non si azzera.
    /// </summary>
    private bool IsRateLimited(string ip)
    {
        if (!_loginAttempts.TryGetValue(ip, out var attempt)) return false;
        if (DateTime.UtcNow - attempt.WindowStartUtc > LoginAttemptWindow)
        {
            _loginAttempts.TryRemove(ip, out _);
            return false;
        }
        return attempt.Count >= MaxLoginAttempts;
    }

    private void RegisterFailedLogin(string ip)
    {
        _loginAttempts.AddOrUpdate(ip,
            _ => (1, DateTime.UtcNow),
            (_, current) => DateTime.UtcNow - current.WindowStartUtc > LoginAttemptWindow
                ? (1, DateTime.UtcNow)
                : (current.Count + 1, current.WindowStartUtc));
    }
}
