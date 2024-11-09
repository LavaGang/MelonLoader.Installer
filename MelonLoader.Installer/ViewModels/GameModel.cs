using Avalonia.Media.Imaging;
using MelonLoader.Installer.GameLaunchers;
using Semver;

namespace MelonLoader.Installer.ViewModels;

public class GameModel(string path, string name, bool is32Bit, GameLauncher? launcher, Bitmap? icon, SemVersion? mlVersion) : ViewModelBase
{
    public string Path => path;
    public string Name => name;
    public bool Is32Bit => is32Bit;
    public GameLauncher? Launcher => launcher;
    public Bitmap? Icon => icon;
    public string? MLVersionText => mlVersion != null ? 'v' + mlVersion.ToString() : null;
    public string MLStatusText => mlVersion == null ? "Not Installed" : "Installed " + MLVersionText;
    public bool MLInstalled => mlVersion != null;

    public SemVersion? MLVersion
    {
        get => mlVersion;
        set
        {
            mlVersion = value;
            OnPropertyChanged(nameof(MLVersion));
            OnPropertyChanged(nameof(MLVersionText));
            OnPropertyChanged(nameof(MLStatusText));
            OnPropertyChanged(nameof(MLInstalled));
        }
    }

    /// <summary>
    /// Checks if the game is still valid. Otherwise, automatically removes it from the Games list.
    /// </summary>
    /// <returns>True if the game still exists, otherwise false.</returns>
    public bool ValidateGame()
    {
        if (!File.Exists(path) || !Directory.Exists(path[..^4] + "_Data"))
        {
            GameManager.RemoveGame(this);
            return false;
        }

        var newMlVersion = Installer.MLVersion.GetMelonLoaderVersion(System.IO.Path.GetDirectoryName(path)!);
        if (newMlVersion != MLVersion)
        {
            MLVersion = newMlVersion;
            GameManager.ResortGame(this);
        }

        return true;
    }
}
