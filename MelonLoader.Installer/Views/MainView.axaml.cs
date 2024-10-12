using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Collections.Specialized;

namespace MelonLoader.Installer.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        PointerPressed += ResetErrorMessage;

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

    private void ResetErrorMessage(object? sender, PointerPressedEventArgs e)
    {
        ErrorText.Text = string.Empty;
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
        GameManager.TryAddGame(path, null, GameSource.Manual, null, out var error);
        if (error != null)
        {
            SetErrorMessage(error);
            return;
        }

        GameManager.SaveManualGameList();
    }

    public void SetErrorMessage(string errorMessage)
    {
        ErrorText.Text = errorMessage;
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
