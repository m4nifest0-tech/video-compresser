using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using VideoCompressor.Services;

namespace VideoCompressor;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    // Tenuto vivo per tutta la durata del processo: se venisse raccolto dal GC il mutex si
    // rilascerebbe prematuramente, permettendo a una seconda istanza di avviarsi.
    private static Mutex? _singleInstanceMutex;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    protected override void OnStartup(StartupEventArgs e)
    {
        // Un'istanza duplicata (avviata per errore, o perche' l'utente ha ri-lanciato l'exe
        // pensando che l'aggiornamento non fosse partito) altrimenti resterebbe in memoria e
        // terrebbe bloccato il file .exe, facendo fallire ogni successivo aggiornamento.
        bool isNewInstance;
        try
        {
            _singleInstanceMutex = new Mutex(true, "Global\\VideoCompressorApp_SingleInstance", out isNewInstance);
        }
        catch (UnauthorizedAccessException)
        {
            _singleInstanceMutex = new Mutex(true, "VideoCompressorApp_SingleInstance", out isNewInstance);
        }

        if (!isNewInstance)
        {
            BringExistingInstanceToFront();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        Settings = AppSettings.Load();
        ThemeManager.Apply(Settings.ThemeMode, Settings.AccentColor);

        new MainWindow().Show();
    }

    private static void BringExistingInstanceToFront()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            var existing = Process.GetProcessesByName(current.ProcessName).FirstOrDefault(p => p.Id != current.Id);
            if (existing != null && existing.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(existing.MainWindowHandle, 9); // SW_RESTORE
                SetForegroundWindow(existing.MainWindowHandle);
            }
        }
        catch
        {
            // se non riusciamo a portarla in primo piano l'utente la trovera' comunque aperta
        }
    }

    // Cattura le eccezioni non gestite sul thread della UI (inclusi gli async void degli
    // event handler dei pulsanti) cosi' l'app mostra un messaggio invece di sparire senza avviso.
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Si e' verificato un errore imprevisto:\n\n{e.Exception.Message}\n\nL'applicazione restera' aperta, ma l'operazione in corso potrebbe non essere stata completata.",
            "Errore imprevisto", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    // Le eccezioni su thread diversi dalla UI non possono essere "gestite" (il processo termina
    // comunque), ma mostrare il messaggio prima della chiusura aiuta a capire cosa e' successo.
    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show($"Errore critico, l'applicazione verra' chiusa:\n\n{ex.Message}",
                "Errore critico", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
