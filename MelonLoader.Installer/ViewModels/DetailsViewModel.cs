namespace MelonLoader.Installer.ViewModels;

public class DetailsViewModel(GameModel game) : ViewModelBase
{
    private bool _installing;

    public GameModel Game => game;

    public bool Installing
    {
        get => _installing;
        set
        {
            _installing = value;
            OnPropertyChanged(nameof(Installing));
        }
    }
}
