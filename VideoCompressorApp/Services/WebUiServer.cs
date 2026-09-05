using System.IO;
using System.Text;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VideoCompressor.Services;

/// <summary>
/// Piccolo server web (Kestrel), eseguito nello stesso processo della finestra WPF, che espone
/// coda, impostazioni e upload/download in modo che un altro PC sulla rete locale possa pilotare
/// la stessa compressione mostrata a schermo. Le mutazioni della coda condivisa (_items) devono
/// avvenire sul thread UI: ogni handler passa quindi da Dispatch/Application.Current.Dispatcher.
/// </summary>
public class WebUiServer
{
    private readonly int _port;
    private WebApplication? _app;

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
            if (!IsAuthorized(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Basic realm=\"Compressore Video\"";
                return;
            }
            await next();
        });

        app.MapGet("/", () => Results.Content(IndexHtml.Content, "text/html; charset=utf-8"));

        app.MapGet("/api/state", () => Dispatch(() => Results.Json(MainWindow.Current!.GetState())));

        app.MapPost("/api/estimate", () => Dispatch(() =>
        {
            var mw = MainWindow.Current!;
            var error = mw.ValidateForEstimate();
            if (error != null) return Results.BadRequest(new { error });
            if (mw.IsBusy) return Results.Conflict(new { error = "Operazione gia' in corso." });
            mw.Dispatcher.BeginInvoke(new Action(async () => await mw.RunEstimateAsync()));
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

            var form = await request.ReadFormAsync();
            var uploadDir = Path.Combine(Path.GetTempPath(), "VideoCompressorUploads");
            Directory.CreateDirectory(uploadDir);

            var savedPaths = new List<string>();
            foreach (var file in form.Files)
            {
                if (file.Length <= 0) continue;
                var safeName = Path.GetFileName(file.FileName);
                if (string.IsNullOrWhiteSpace(safeName)) continue;
                var dest = Path.Combine(uploadDir, $"{Guid.NewGuid():N}_{safeName}");
                await using var stream = File.Create(dest);
                await file.CopyToAsync(stream);
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
        app.RunAsync();
    }

    public void Stop()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            _app?.StopAsync(cts.Token).GetAwaiter().GetResult();
        }
        catch { /* server gia' in chiusura */ }
        _app = null;
    }

    private static IResult Dispatch(Func<IResult> func) => Application.Current!.Dispatcher.Invoke(func);

    private static bool IsAuthorized(HttpRequest request)
    {
        var settings = App.Settings;
        if (string.IsNullOrEmpty(settings.WebUiUsername) || string.IsNullOrEmpty(settings.WebUiPasswordHash))
            return false;

        var header = request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex < 0) return false;
            var user = decoded[..separatorIndex];
            var pass = decoded[(separatorIndex + 1)..];
            return user == settings.WebUiUsername && PasswordHasher.Verify(pass, settings.WebUiPasswordHash);
        }
        catch
        {
            return false;
        }
    }
}
