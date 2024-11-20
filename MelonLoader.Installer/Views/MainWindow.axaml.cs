using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using MelonLoader.Installer.ViewModels;

namespace MelonLoader.Installer.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    public MainWindow()
    {
        Instance = this;

        InitializeComponent();

        if (Updater.CurrentState == Updater.State.Updating)
        {
            Updater.Finished += (errorMessage) => Dispatcher.UIThread.Post(() => OnUpdateFinished(errorMessage));
            Content = new UpdaterView();
            return;
        }

        if (Updater.CurrentState == Updater.State.Finished)
        {
            OnUpdateFinished(Updater.LatestError);
            return;
        }

        ShowMainView();
    }

    private void OnUpdateFinished(string? errorMessage)
    {
        if (errorMessage != null)
        {
            DialogBox.ShowError(errorMessage);
            ShowMainView();
            return;
        }

        Close();
    }

    protected override void IsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.IsVisibleChanged(e);

        if (!IsVisible)
            return;
        
        Topmost = true;
        Topmost = false;
#if WINDOWS
        Program.GrabAttention();
#endif
        Focus();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (Updater.CurrentState == Updater.State.Updating || Content is DetailsView { Model.Installing: true })
            e.Cancel = true;

        base.OnClosing(e);
    }

    public void SetViewport(UserControl view)
    {
        Viewport.Child = view;
    }

    public void ShowMainView()
    {
        SetViewport(new MainView());
    }

    public void ShowDetailsView(GameModel game)
    {
        SetViewport(new DetailsView()
        {
            DataContext = new DetailsViewModel(game)
        });
    }
}
