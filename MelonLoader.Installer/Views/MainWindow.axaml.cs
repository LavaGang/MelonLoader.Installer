using Avalonia.Controls;
using MelonLoader.Installer.ViewModels;

namespace MelonLoader.Installer.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

        ShowMainView();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (Content is DetailsView view && view.Model != null && view.Model.Installing)
            e.Cancel = true;

        base.OnClosing(e);
    }

    public void ShowMainView()
    {
        Content = new MainView();
    }

    public void ShowDetailsView(GameModel game)
    {
        Content = new DetailsView()
        {
            DataContext = game
        };
    }
}
