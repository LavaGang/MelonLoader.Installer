using System.Collections.ObjectModel;

namespace MelonLoader.Installer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<GameModel> Games => GameManager.Games;

    public string Version => 'v' + typeof(MainViewModel).Assembly.GetName().Version?.ToString(3);
}
