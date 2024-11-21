using Avalonia.Controls;
using Avalonia.Interactivity;
using MelonLoader.Installer.ViewModels;
using System.Collections.Specialized;

namespace MelonLoader.Installer.Views;

public partial class MainView : UserControl
{
    private static bool showedNotice;
    
    public MainViewModel? Model => (MainViewModel?)DataContext;

    public MainView()
    {
        InitializeComponent();
    }

    protected override async void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (Model == null)
            return;

        // if the updater has already ran, we already did all initialization
        if (Updater.State == Updater.UpdateState.None)
        {
            Model.Ready = false;
            if (!await DoInit())
            {
                return;
            }
            Model.Ready = true;
        }

        OnGameListUpdate(null, null);
        GameManager.Games.CollectionChanged += OnGameListUpdate;

        if (!showedNotice && Program.Version.Revision > 0)
        {
            showedNotice = true;
            DialogBox.ShowNotice("""
                                 You're currently using a bleeding-edge CI build.
                                 Please note that this build will not auto-update, so it's recommended to use a stable release instead.
                                 """);
        }
    }

    private static async Task<bool> DoInit()
    {
        try
        {
            var checkUpdate = Task.Run(Updater.UpdateIfPossible);
            var otherInit = Task.WhenAll(Task.Run(MLManager.Init), Task.Factory.StartNew(GameManager.Init, TaskCreationOptions.LongRunning));
            if (await checkUpdate is { } updateTask)
            {
                _ = MainWindow.Instance.HandleUpdate(updateTask);
                return false;
            }
            await otherInit;
        }
        catch (Exception ex)
        {
            CrashException(ex);
        }
        return true;
    }

    private static void CrashException(Exception ex)
    {
        Program.LogCrashException(ex);

        DialogBox.ShowError("""
                            An error has occurred while loading the game library!
                            Please report this issue in the official Discord server in the #ml-support channel.
                            Include the crash log named 'melonloader-installer-crash.log', located next to the executable.
                            """, () => MainWindow.Instance.Close());
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
            DialogBox.ShowError(error);
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
