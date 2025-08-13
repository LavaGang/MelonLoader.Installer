using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MelonLoader.Installer.ViewModels;

namespace MelonLoader.Installer.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;
    private DispatcherTimer? _hideTimer;

    public MainWindow()
    {
        Instance = this;

        // Initialize timer for delayed hiding
        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _hideTimer.Tick += OnHideTimerTick;

        InitializeComponent();

        // Subscribe to drag and drop events
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnWindowDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnWindowDragLeave);
        AddHandler(DragDrop.DropEvent, OnWindowDrop);

        ShowMainView();
    }

    private void OnWindowDragOver(object? sender, DragEventArgs e)
    {
        // Check if the dragged data contains files
        if (Viewport.Child is MainView mainView)
        {
            bool isExecutable = InstallerUtils.CheckDragEventForGameExecutable(e);
            if (isExecutable)
            {
                e.DragEffects = DragDropEffects.Copy;
                WindowDragDropOverlaySuccess.IsVisible = true;
                WindowDragDropOverlayError.IsVisible = false;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
                WindowDragDropOverlaySuccess.IsVisible = false;
                WindowDragDropOverlayError.IsVisible = true;
            }

            _hideTimer?.Stop();
            return;
        }

        e.DragEffects = DragDropEffects.None;
        _hideTimer?.Start();
    }

    private void OnWindowDragLeave(object? sender, DragEventArgs e)
    {
        // Reset state
        _hideTimer?.Start();
    }

    private async void OnWindowDrop(object? sender, DragEventArgs e)
    {
        // Reset state
        CloseDragDropOverlay();

        // Delegate to MainView if it's currently active
        if (Viewport.Child is MainView mainView)
            await mainView.HandleDropAsync(e); // Await the async HandleDrop method
    }

    private void OnHideTimerTick(object? sender, EventArgs e)
        => CloseDragDropOverlay();

    private void CloseDragDropOverlay()
    {
        _hideTimer?.Stop();
        WindowDragDropOverlaySuccess.IsVisible = false;
        WindowDragDropOverlayError.IsVisible = false;
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

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
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
        CloseDragDropOverlay();
        var view = new DetailsView()
        {
            DataContext = new DetailsViewModel(game)
        };
        view.UpdateVersionInfo();
        SetViewport(view);
    }
}
