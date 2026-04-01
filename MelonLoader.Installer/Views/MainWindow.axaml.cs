using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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
        
        //Opened += OnWindowOpened;

        ShowMainView();
    }

    /*
    private void AFJ()
    {
        var now = DateTime.Now;
        if ((now.Month != 4) || (now.Day > 2))
            return;

        if (Config.Instance.AFJY != now.Year)
        {
            Config.Instance.AFJY = now.Year;
            Config.Instance.AFJS = 2;
            Config.Save();
        }
        if (Config.Instance.AFJS < 0)
            Config.Instance.AFJS = 0;
        if (Config.Instance.AFJS > 3)
            Config.Instance.AFJS = 3;
        
        Background = Brushes.Transparent;

        var btn = new Button()
        {
            Content = "🔄",
            FontSize = 18,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
            Background = Brushes.Transparent,
            Margin = new(5),
            RenderTransform = new ScaleTransform(-1, 1)
        };

        btn.Click += (s, e) =>
        {
            Config.Instance.AFJS++;
            if (Config.Instance.AFJS > 3)
                Config.Instance.AFJS = 0;
            Config.Save();
            AFJR();
        };

        ((Grid)LogicalChildren[0]).Children.Add(btn);
        AFJR();
    }

    private void AFJR()
        => ApplyRotationAndFit(90 * Config.Instance.AFJS);

    private void OnWindowOpened(object? sender, EventArgs e)
        => AFJ();
    */

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
    
    private void ApplyRotationAndFit(double angleDegrees)
    {
        var view = (Grid)LogicalChildren[0];
        var bounds = view.Bounds;
        var w = bounds.Width;
        var h = bounds.Height;

        view.RenderTransform = new RotateTransform(angleDegrees);

        var radians = angleDegrees * Math.PI / 180.0;
        var cos = Math.Abs(Math.Cos(radians));
        var sin = Math.Abs(Math.Sin(radians));

        var newWidth = w * cos + h * sin;
        var newHeight = w * sin + h * cos;

        ClientSize = new Size(Math.Ceiling(newWidth), Math.Ceiling(newHeight));
    }
}
