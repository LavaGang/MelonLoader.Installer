﻿using Avalonia.Controls;
using Avalonia.Threading;
using MelonLoader.Installer.ViewModels;

namespace MelonLoader.Installer.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    public MainWindow()
    {
        Instance = this;

        if (Updater.CurrentState != Updater.State.None)
            GameManager.Init();

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
            // TODO: Show error message
            ShowMainView();
            return;
        }

        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (Updater.CurrentState == Updater.State.Updating || (Content is DetailsView view && view.Model != null && view.Model.Installing))
            e.Cancel = true;

        base.OnClosing(e);
    }

    public void ShowMainView()
    {
        MLManager.Init();
        GameManager.Init();
        Content = new MainView();
    }

    public void ShowDetailsView(GameModel game)
    {
        Content = new DetailsView()
        {
            DataContext = game
        };
    }
}