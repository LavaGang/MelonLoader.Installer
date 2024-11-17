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

        Activated += OnActivation;

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

    private static void OnActivation(object? sender, EventArgs e)
    {
        if (sender is not Window window)
            return;

        window.Topmost = true;
        window.Topmost = false;
#if WINDOWS
        Program.GrabAttention();
#endif
        window.Focus();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (Updater.CurrentState == Updater.State.Updating || Content is DetailsView { Model.Installing: true })
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
            DataContext = new DetailsViewModel(game)
        };
    }
}
