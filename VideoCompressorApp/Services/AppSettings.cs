using System.IO;
using System.Text.Json;

namespace VideoCompressor.Services;

public class AppSettings
{
    public string ThemeMode { get; set; } = "Light";
    public string AccentColor { get; set; } = "Blue";
    public string DestDir { get; set; } = "";
    public bool CheckUpdatesOnStartup { get; set; } = true;

    public bool WebUiEnabled { get; set; }
    public int WebUiPort { get; set; } = 5150;
    public string WebUiUsername { get; set; } = "";
    public string WebUiPasswordHash { get; set; } = "";

    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VideoCompressor", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // impostazioni corrotte o illeggibili: si torna ai valori predefiniti
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this));
        }
        catch
        {
            // se non è possibile salvare, si continua comunque con le impostazioni in memoria
        }
    }
}
