using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Text;

namespace MelonLoader.Installer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool ready;

    [ObservableProperty]
    private string? search;

    [ObservableProperty]
    private ObservableCollection<GameModel> games = GameManager.Games;

    [ObservableProperty]
    private string? supportedExtensions;

    public MainViewModel() : base()
    {
        StringBuilder builder = new();
        int count = InstallerUtils._validExtensions.Count;
        for (int i = 0; i < count; i++)
        {
            if (i >= (count - 1))
                builder.Append("and ");

            var type = InstallerUtils._validExtensions.ElementAt(i);
            builder.Append(type);

            if (i < (count - 1))
                builder.Append(", ");
        }
        supportedExtensions = builder.ToString();
    }

    partial void OnSearchChanged(string? value)
    {
        Games = new(GameManager.Games.Where(x => string.IsNullOrEmpty(value) || x.Name.Contains(value, StringComparison.CurrentCultureIgnoreCase)));
    }
}
