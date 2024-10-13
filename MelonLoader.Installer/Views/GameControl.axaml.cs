using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MelonLoader.Installer.ViewModels;

namespace MelonLoader.Installer.Views;

public partial class GameControl : UserControl
{
    public GameModel? Model => (GameModel?)DataContext;

    public GameControl()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (Model == null)
        {
            IconsPanel.IsVisible = false;
            return;
        }

        var mlInstalled = Model.MLVersion != null;

        IconsPanel.IsVisible = mlInstalled || Model.Launcher != null;
        MLIcon.IsVisible = mlInstalled;
        if (Model.Launcher != null)
        {
            LauncherIcon.Source = new Bitmap(AssetLoader.Open(new("avares://" + typeof(GameControl).Assembly.GetName().Name + Model.Launcher.IconPath)));
            LauncherIcon.IsVisible = true;
        }
    }

    public void ClickHandler(object sender, RoutedEventArgs args)
    {
        if (Model == null)
            return;

        // For some reason DataContext becomes null if it updates, so we have to keep our own ref
        var model = Model;

        if (!model.ValidateGame())
            return;

        MainWindow.Instance.ShowDetailsView(model);
    }
}