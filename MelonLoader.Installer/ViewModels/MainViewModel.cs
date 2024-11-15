using System.Collections.ObjectModel;

namespace MelonLoader.Installer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private static bool _ready;

    public bool Ready
    {
        get => _ready;
        set
        {
            _ready = value;
            OnPropertyChanged(nameof(Ready));
        }
    }

    public ObservableCollection<GameModel> Games => GameManager.Games;

    public string Version => 'v' + Program.Version.ToString(3);
}
