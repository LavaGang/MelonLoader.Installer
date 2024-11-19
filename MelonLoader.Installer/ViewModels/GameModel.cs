using Avalonia.Media.Imaging;
using MelonLoader.Installer.GameLaunchers;
using Semver;

namespace MelonLoader.Installer.ViewModels;

public class GameModel(string path, string name, bool is32Bit, bool isLinux, GameLauncher? launcher, Bitmap? icon, SemVersion? mlVersion, bool isProtected) : ViewModelBase
{
    public string Path => path;
    public string Name => name;
    public bool Is32Bit => is32Bit;
    public bool IsLinux => isLinux;
    public GameLauncher? Launcher => launcher;
    public Bitmap? Icon => icon;
    public string? MLVersionText => mlVersion != null ? 'v' + mlVersion.ToString() : null;
    public string MLStatusText => mlVersion == null ? "Not Installed" : "Installed " + MLVersionText;
    public bool MLInstalled => mlVersion != null;
    public bool IsProtected => isProtected;
    public string Dir { get; } = System.IO.Path.GetDirectoryName(path)!;

    public SemVersion? MLVersion
    {
        get => mlVersion;
        set
        {
            mlVersion = value;
            OnPropertyChanged();
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
        var exeExtIdx = path.LastIndexOf('.');
        
        var pathNoExt = exeExtIdx != -1 ? path[..exeExtIdx] : path;
        
        if (!File.Exists(path) || !Directory.Exists(pathNoExt + "_Data"))
        {
            GameManager.RemoveGame(this);
            return false;
        }

        var newMlVersion = Installer.MLVersion.GetMelonLoaderVersion(Dir, out var ml86, out var mlLinux);
        if (newMlVersion != null && (ml86 != Is32Bit || mlLinux != IsLinux))
            newMlVersion = null;
        
        if (newMlVersion == MLVersion) 
            return true;
        
        MLVersion = newMlVersion;
        GameManager.ResortGame(this);

        return true;
    }
}
