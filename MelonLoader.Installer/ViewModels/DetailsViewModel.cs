namespace MelonLoader.Installer.ViewModels;

public class DetailsViewModel(GameModel game) : ViewModelBase
{
    private bool _installing;
    private bool _offline;
    private bool _linuxInstructions;

    public GameModel Game => game;

    public bool Installing
    {
        get => _installing;
        set
        {
            _installing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EnableSettings));
        }
    }

    public bool Offline
    {
        get => _offline;
        set
        {
            _offline = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EnableSettings));
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

    public bool EnableSettings => !Offline && !Installing;
}
