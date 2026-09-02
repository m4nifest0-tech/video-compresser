using System.Windows;
using System.Windows.Threading;
using VideoCompressor.Services;

namespace VideoCompressor;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        Settings = AppSettings.Load();
        ThemeManager.Apply(Settings.ThemeMode, Settings.AccentColor);

        new MainWindow().Show();
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
