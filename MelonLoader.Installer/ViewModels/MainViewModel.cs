using System.Collections.ObjectModel;

namespace MelonLoader.Installer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private static bool _ready;

    public bool Ready
    {
        get => _ready;
        set
        {
            _ready = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<GameModel> Games => GameManager.Games;

    public string Version => Program.VersionName;
}
