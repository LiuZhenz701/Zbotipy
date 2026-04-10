using System.IO;
using System.Text.Json;

namespace LocalMusicPlayer.Services;

public sealed class UserPreferences
{
    public bool MinimizeToTray { get; set; }

    private static string DirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zbotipy");

    private static string FilePath => Path.Combine(DirectoryPath, "preferences.json");

    public static UserPreferences Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var p = JsonSerializer.Deserialize<UserPreferences>(json);
                if (p is not null)
                    return p;
            }
        }
        catch
        {
            // ignore corrupt or missing file
        }

        return new UserPreferences();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch
        {
            // ignore IO errors
        }
    }
}
