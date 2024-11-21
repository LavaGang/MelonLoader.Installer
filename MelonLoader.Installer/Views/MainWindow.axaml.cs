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

        ShowMainView();
    }

    public async Task HandleUpdate(Task updaterTask)
    {
        try
        {
            SetViewport(new UpdaterView());
            await updaterTask;
            Close();
        }
        catch (Exception ex)
        {
            DialogBox.ShowError(ex.Message);
            ShowMainView();
        }
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
        if (Updater.State == Updater.UpdateState.Updating || Content is DetailsView { Model.Installing: true })
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
