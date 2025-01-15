using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using MelonLoader.Installer.ViewModels;

namespace MelonLoader.Installer.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    private static ViewState? _lastViewState;

    public static bool InLoadView => _currentLoadView != null;

    private static LoadView? _currentLoadView;

    private static readonly List<CloseLock> _closeLocks = [];

    public MainWindow()
    {
        Instance = this;

        InitializeComponent();

        InitAsync();
    }

    public async void InitAsync()
    {
        using (RequestCloseLock())
        {
            if (await Updater.UpdateIfPossibleAsync(SetLoadStatus))
            {
                Close();
                return;
            }
        }

        ShowMainView();
        FinishLoad();
    }

    public static CloseLock RequestCloseLock()
    {
        var loc = new CloseLock();
        _closeLocks.Add(loc);
        return loc;
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
        if (_closeLocks.Count != 0)
            return;

        base.OnClosing(e);
    }

    public void SetViewport<T>(ViewModelBase? model = null) where T : UserControl, new()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => SetViewport<T>(model));
            return;
        }

        _lastViewState = new ViewState<T>(model);

        if (!InLoadView)
        {
            Viewport.Child = _lastViewState.Get();
        }
    }

    public void ShowMainView()
    {
        SetViewport<MainView>();
    }

    public void SetLoadStatus(double progress, string? newStatus)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => SetLoadStatus(progress, newStatus));
            return;
        }

        if (_currentLoadView == null)
        {
            if (_lastViewState != null)
            {
                _lastViewState.Model = Viewport.Child?.DataContext as ViewModelBase;
            }

            _currentLoadView = new LoadView();
            Viewport.Child = _currentLoadView;
        }

        _currentLoadView.SetProgress(progress, newStatus);
    }

    public void FinishLoad()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(FinishLoad);
            return;
        }

        if (!InLoadView)
            return;

        _currentLoadView = null;

        if (_lastViewState == null)
        {
            Viewport.Child = null;
            return;
        }

        Viewport.Child = _lastViewState.Get();
    }

    public void ShowDetailsView(GameModel game)
    {
        SetViewport<DetailsView>(new DetailsViewModel(game));
    }

    public class CloseLock : IDisposable
    {
        public void Dispose()
        {
            _closeLocks.Remove(this);
        }
    }

    private abstract class ViewState(ViewModelBase? model)
    {
        public ViewModelBase? Model { get; set; } = model;

        public abstract UserControl Get();
    }

    private class ViewState<T>(ViewModelBase? model) : ViewState(model) where T : UserControl, new()
    {
        public override UserControl Get()
        {
            var obj = new T();
            if (Model != null)
                obj.DataContext = Model;

            return obj;
        }
    }
}
