using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MelonLoader.Installer.ViewModels;
using System;

namespace MelonLoader.Installer.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;
    private bool _hasValidFiles = false;
    private bool _overlayVisible = false;
    private DispatcherTimer? _hideTimer;
    private readonly HashSet<string> _validExtensions = new(StringComparer.OrdinalIgnoreCase) { ".exe", ".app", ".x86_64" };

    public MainWindow()
    {
        Instance = this;

        InitializeComponent();
        
        // Subscribe to drag and drop events
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnWindowDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnWindowDragLeave);
        AddHandler(DragDrop.DropEvent, OnWindowDrop);

        // Initialize timer for delayed hiding
        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _hideTimer.Tick += OnHideTimerTick;

        ShowMainView();
    }

    private void OnWindowDragOver(object? sender, DragEventArgs e)
    {
        // Stop any pending hide timer
        _hideTimer?.Stop();

        // Check if the dragged data contains files
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                // Check if any of the files is a Unity game executable
                var hasGameExecutable = files.Any(file => 
                {
                    var path = file.Path.LocalPath;
                    var extension = Path.GetExtension(path);
                    var fileName = Path.GetFileName(path);
                    
                    // Support Windows .exe, macOS .app bundles, and Linux .x86_64 executables
                    return _validExtensions.Contains(extension) || 
                           fileName.EndsWith(".x86_64", StringComparison.OrdinalIgnoreCase);
                });

                if (hasGameExecutable)
                {
                    e.DragEffects = DragDropEffects.Copy;
                    
                    // Only update UI if state has changed
                    if (!_hasValidFiles || !_overlayVisible)
                    {
                        _hasValidFiles = true;
                        _overlayVisible = true;
                        WindowDragDropOverlay.IsVisible = true;
                    }
                    return;
                }
            }
        }

        e.DragEffects = DragDropEffects.None;
        
        // Start timer to hide overlay
        if (_hasValidFiles || _overlayVisible)
        {
            _hideTimer?.Start();
        }
    }

    private void OnWindowDragLeave(object? sender, DragEventArgs e)
    {
        // Start timer to hide overlay after a short delay
        _hideTimer?.Start();
    }

    private void OnHideTimerTick(object? sender, EventArgs e)
    {
        // Stop the timer and hide overlay
        _hideTimer?.Stop();
        
        _hasValidFiles = false;
        _overlayVisible = false;
        WindowDragDropOverlay.IsVisible = false;
    }

    private async void OnWindowDrop(object? sender, DragEventArgs e)
    {
        // Stop timer and reset state
        _hideTimer?.Stop();
        _hasValidFiles = false;
        _overlayVisible = false;
        WindowDragDropOverlay.IsVisible = false;

        // Delegate to MainView if it's currently active
        if (Viewport.Child is MainView mainView)
        {
            // Await the async HandleDrop method
            await mainView.HandleDropAsync(e);
        }
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
        // Clean up timer
        _hideTimer?.Stop();
        _hideTimer = null;
        
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
        var view = new DetailsView()
        {
            DataContext = new DetailsViewModel(game)
        };
        view.UpdateVersionInfo();
        SetViewport(view);
    }
}
