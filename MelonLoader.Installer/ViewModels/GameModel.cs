using Avalonia.Media.Imaging;
using MelonLoader.Installer.GameLaunchers;
using MelonLoader.Installer.Views;
using Semver;

namespace MelonLoader.Installer.ViewModels;

public class GameModel(string path, string name, Architecture architecture, GameLauncher? launcher, Bitmap? icon, SemVersion? mlVersion, string? proxyName, bool isProtected) : ViewModelBase
{
    public string Path => path;
    public string Name => name;
    public Architecture Arch => architecture;
    
    public bool IsWindows => ((architecture == Architecture.WindowsX86)
                              || (architecture == Architecture.WindowsX64) 
                              || (architecture == Architecture.WindowsArm64));
    
    public bool IsLinux => ((architecture == Architecture.LinuxX86)
                            || (architecture == Architecture.LinuxX64)
                            || (architecture == Architecture.LinuxArm64));
    
    public bool IsMacOS => ((architecture == Architecture.MacOSX64) 
                            || (architecture == Architecture.MacOSArm64));
    
    public GameLauncher? Launcher => launcher;
    public Bitmap? Icon => icon;
    public string? MLVersionText => mlVersion != null ? 'v' + mlVersion.ToString() : null;
    public string MLStatusText => mlVersion == null ? "Not Installed" : "Installed " + MLVersionText;
    public bool MLInstalled => mlVersion != null;
    public bool IsProtected => isProtected;
    public string Dir { get; } = System.IO.Path.GetDirectoryName(path)!;
    public string? ProxyName = proxyName;

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
        if ((newMlVersion == null)
            || (arch != Arch))
        {
            MLVersion = null;
            ProxyName = null;
        }
        else
        {
            MLVersion = newMlVersion.Value.Item1;
            ProxyName = newMlVersion.Value.Item2;
        }
        
        GameManager.ResortGame(this);
        return true;
    }
}
