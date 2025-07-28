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

        var showWine =
#if LINUX || OSX
            Model.IsWindows;
#else
            false;
#endif

        IconsPanel.IsVisible = mlInstalled || Model.Launcher != null || showWine;
        WineIcon.IsVisible = showWine;

        MLIcon.IsVisible = mlInstalled;
        if (Model.Launcher != null)
        {
            LauncherIcon.Source = new Bitmap(AssetLoader.Open(new("avares://" + typeof(GameControl).Assembly.GetName().Name + Model.Launcher.IconPath)));
            LauncherIcon.IsVisible = true;
        }

        if (Model.IsProtected)
        {
            ToolTip.SetTip(this, "This game contains anti-modding measures.");
        }
    }

    public void ClickHandler(object sender, RoutedEventArgs args)
    {
        if (Model == null)
            return;

        // For some reason DataContext becomes null if it updates, so we have to keep our own ref
        var model = Model;

        if (!model.Validate(out _))
            return;

        MainWindow.Instance.ShowDetailsView(model);
    }
}