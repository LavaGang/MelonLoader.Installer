using Avalonia.Media.Imaging;
using MelonLoader.Installer.GameLaunchers;
using MelonLoader.Installer.Views;
using Semver;

namespace MelonLoader.Installer.ViewModels;

public class GameModel(string path, string name, Architecture architecture, GameLauncher? launcher, Bitmap? icon, SemVersion? mlVersion, bool isProtected) : ViewModelBase
{
    public string Path => path;
    public string Name => name;
    public Architecture Arch => architecture;
    public bool IsWindows => ((architecture == Architecture.WindowsX64) || (architecture == Architecture.WindowsX86));
    public bool IsLinux => ((architecture == Architecture.LinuxX64) || (architecture == Architecture.LinuxX86));
    public bool IsMacOS => ((architecture == Architecture.MacOSX64) || (architecture == Architecture.MacOSArm64));
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
    public bool Validate(out string? errorMessage)
    {
        errorMessage = null;

        string gameDir = path;
        if (!GameManager.ValidateGame(ref gameDir, out _, out _, out errorMessage)
            || (errorMessage != null))
        {
            GameManager.RemoveGame(this);
            return false;
        }

        var newMlVersion = Installer.MLVersion.GetMelonLoaderVersion(gameDir, out var arch, out errorMessage);
        if (newMlVersion != null && arch != Arch)
            newMlVersion = null;

        if (newMlVersion == MLVersion)
            return true;

        MLVersion = newMlVersion;
        GameManager.ResortGame(this);

        return true;
    }
}
