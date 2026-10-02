namespace MelonLoader.Installer.ViewModels;

public class DetailsViewModel(GameModel game) : ViewModelBase
{
    private bool _installing;
    private bool _offline;
    private bool _linuxInstructions;
    private bool _macOSInstructions;

    private MLVersion? _selectedVersion;

    public GameModel Game => game;

    public MLVersion? SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            _selectedVersion = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCompatibleVersion));
            OnPropertyChanged(nameof(CanInstall));
        }
    }

    public bool HasCompatibleVersion => SelectedVersion?.GetDownload(Game.Arch) != null;
    public bool CanInstall => EnableSettings && HasCompatibleVersion;
    public string NoCompatibleVersionMessage =>
        Offline
            ? "Versions could not be loaded. Check your connection and reopen this game to retry."
            : $"No compatible version is available for {Game.Arch}. You can enable Nightly builds or select a compatible local ZIP.";

    public bool Installing
    {
        get => _installing;
        set
        {
            _installing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EnableSettings));
            OnPropertyChanged(nameof(CanInstall));
        }
    }

    public bool Offline
    {
        get => _offline;
        set
        {
            _offline = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NoCompatibleVersionMessage));
            OnPropertyChanged(nameof(EnableSettings));
            OnPropertyChanged(nameof(CanInstall));
        }
    }

    public bool LinuxInstructions
    {
        get => _linuxInstructions;
        set
        {
            _linuxInstructions = value;
            OnPropertyChanged();
        }
    }

    public bool MacOSInstructions
    {
        get => _macOSInstructions;
        set
        {
            _macOSInstructions = value;
            OnPropertyChanged();
        }
    }

    public bool EnableSettings => !Offline && !Installing;
}
