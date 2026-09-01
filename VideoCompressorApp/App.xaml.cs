using System.Windows;
using VideoCompressor.Services;

namespace VideoCompressor;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Settings = AppSettings.Load();
        ThemeManager.Apply(Settings.ThemeMode, Settings.AccentColor);

        new MainWindow().Show();
    }
}
