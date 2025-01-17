using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MelonLoader.Installer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? search;

    [ObservableProperty]
    private ObservableCollection<GameModel> games = GameManager.Games;

    partial void OnSearchChanged(string? value)
    {
        Games = new(GameManager.Games.Where(x => string.IsNullOrEmpty(value) || x.Name.Contains(value, StringComparison.CurrentCultureIgnoreCase)));
    }
}
