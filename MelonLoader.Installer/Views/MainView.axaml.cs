using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MelonLoader.Installer.ViewModels;
using System.Collections.Specialized;

namespace MelonLoader.Installer.Views;

public partial class MainView : UserControl
{
    public MainViewModel? Model => (MainViewModel?)DataContext;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (Model == null)
            return;

        Task.Run(InitServicesAsync);
    }

    private void InitServicesAsync()
    {
        MLManager.Init();
        GameManager.Init();

        Dispatcher.UIThread.Post(Init);
    }

    private void Init()
    {
        Model!.Ready = true;

        OnGameListUpdate(null, null);
        GameManager.Games.CollectionChanged += OnGameListUpdate;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        GameManager.Games.CollectionChanged -= OnGameListUpdate;
    }

    private void OnGameListUpdate(object? sender, NotifyCollectionChangedEventArgs? e)
    {
        NoGamesText.IsVisible = GameManager.Games.Count == 0;
    }

    public async void AddGameManuallyHandler(object sender, RoutedEventArgs args)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new()
        {
            Title = "Select a Unity game executable...",
            AllowMultiple = false
        });

        if (files.Count is 0 or > 1)
            return;

        var path = files[0].Path.LocalPath;
        GameManager.TryAddGame(path, null, null, null, out var error);
        if (error != null)
        {
            ErrorBox.Open(error);
            return;
        }

        GameManager.SaveManualGameList();
    }

    private async void OpenURL(Uri url)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        await topLevel.Launcher.LaunchUriAsync(url);
    }

    private void MelonWikiLink(object sender, RoutedEventArgs args)
    {
        OpenURL(Config.MelonWiki);
    }

    private void DiscordLink(object sender, RoutedEventArgs args)
    {
        OpenURL(Config.Discord);
    }

    private void GithubLink(object sender, RoutedEventArgs args)
    {
        OpenURL(Config.Github);
    }

    private void TwitterLink(object sender, RoutedEventArgs args)
    {
        OpenURL(Config.Twitter);
    }
}
