using System.Text.Json;

namespace MilkillaDropVJ.Services;

/// <summary>Ajustes del usuario, guardados en %AppData%\MilkillaDropVJ\settings.json.</summary>
public sealed class AppSettings
{
    public string BeatDropPath { get; set; } = "";

    /// <summary>Emisor Spout elegido a mano; vacío = elegir solo.</summary>
    public string SpoutSender { get; set; } = "";

    public bool CloseBeatDropOnExit { get; set; } = true;

    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MilkillaDropVJ");

    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // Ajustes dañados: se empieza de cero.
        }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>Rutas habituales donde puede estar BeatDrop, por si el usuario aún no lo ha elegido.</summary>
    public static string? GuessBeatDropPath()
    {
        string[] roots =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
        };

        foreach (var root in roots.Where(r => !string.IsNullOrEmpty(r)))
        {
            var candidate = Path.Combine(root, "BeatDrop", "BeatDrop.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }
}
