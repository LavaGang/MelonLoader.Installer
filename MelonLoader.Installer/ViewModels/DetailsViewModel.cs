namespace MelonLoader.Installer.ViewModels;

public class DetailsViewModel(GameModel game) : ViewModelBase
{
    private bool _installing;
    private bool _confirmation;
    private bool _offline;

    public GameModel Game => game;

    public bool Installing
    {
        get => _installing;
        set
        {
            _installing = value;
            OnPropertyChanged(nameof(Installing));
            OnPropertyChanged(nameof(CanInstall));
            OnPropertyChanged(nameof(EnableSettings));
        }
    }

    public bool Confirmation
    {
        get => _confirmation;
        set
        {
            _confirmation = value;
            OnPropertyChanged(nameof(Confirmation));
            OnPropertyChanged(nameof(CanInstall));
        }
    }

    public bool Offline
    {
        get => _offline;
        set
        {
            _offline = value;
            OnPropertyChanged(nameof(Confirmation));
            OnPropertyChanged(nameof(EnableSettings));
        }
    }

    public bool CanInstall => !Installing && !Confirmation;
    public bool EnableSettings => !Offline && !Installing;
}
