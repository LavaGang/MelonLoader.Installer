using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MelonLoader.Installer.ViewModels;
using System.ComponentModel;

namespace MelonLoader.Installer.Views;

public partial class DetailsView : UserControl
{
    public GameModel? Model => (GameModel?)DataContext;

    public DetailsView()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (Model == null)
            return;

        Model.PropertyChanged -= PropertyChangedHandler;
    }

    private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs change)
    {
        if (change.PropertyName == "MLVersion")
        {
            UpdateVersionInfo();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (Model == null)
            return;

        Model.PropertyChanged += PropertyChangedHandler;

        UpdateVersionList();
    }

    private void NightlyToggleHandler(object sender, RoutedEventArgs args)
    {
        UpdateVersionList();
    }

    public void UpdateVersionList()
    {
        if (Model == null)
            return;

        var en = MLManager.Versions.Where(x => x.IsX86 == Model.Is32Bit);
        if (NightlyCheck.IsChecked != true)
            en = en.Where(x => !x.IsArtifact);

        VersionCombobox.ItemsSource = en;
        VersionCombobox.SelectedIndex = 0;
    }

    private void BackClickHandler(object sender, RoutedEventArgs args)
    {
        if (Model != null && Model.Installing)
            return;

        MainWindow.Instance.ShowMainView();
    }

    private void VersionSelectHandler(object? sender, SelectionChangedEventArgs args)
    {
        UpdateVersionInfo();
    }

    public void UpdateVersionInfo()
    {
        if (Model == null || VersionCombobox.SelectedItem == null)
            return;

        MelonIcon.Opacity = Model.MLInstalled ? 1 : 0.3;

        if (Model.MLVersion == null)
        {
            InstallButton.Content = "Install";
            return;
        }

        var versionName = ((MLVersion)VersionCombobox.SelectedItem).VersionName;
        if (versionName.StartsWith('v'))
            versionName = versionName[1..];

        var comp = 1;
        if (Version.TryParse(versionName, out var selVer))
        {
            comp = selVer.CompareTo(Model.MLVersion);
        }

        InstallButton.Content = comp switch
        {
            < 0 => "Downgrade",
            0 => "Reinstall",
            > 0 => "Upgrade"
        };
    }

    private void InstallHandler(object sender, RoutedEventArgs args)
    {
        if (Model == null || !Model.ValidateGame())
        {
            MainWindow.Instance.ShowMainView();
            return;
        }

        Model.Installing = true;
        _ = MLManager.InstallAsync(Path.GetDirectoryName(Model.Path)!, Model.MLInstalled && !KeepFilesCheck.IsChecked!.Value,
            (MLVersion)VersionCombobox.SelectedItem!, Model.Is32Bit,
            (progress, newStatus) => Dispatcher.UIThread.Post(() => OnInstallProgress(progress, newStatus)),
            (errorMessage) => Dispatcher.UIThread.Post(() => OnInstallFinished(errorMessage)));
    }

    private void OnInstallProgress(double progress, string? newStatus)
    {
        if (newStatus != null)
            InstallStatus.Text = newStatus;

        Progress.Value = progress * 100;
        MelonIcon.Opacity = progress * 0.7 + 0.3;
    }

    private void OnInstallFinished(string? errorMessage)
    {
        if (Model == null)
            return;

        Model.Installing = false;

        Model.ValidateGame();
    }

    private void OpenDirHandler(object sender, RoutedEventArgs args)
    {
        if (Model == null)
            return;

        TopLevel.GetTopLevel(this)!.Launcher.LaunchDirectoryInfoAsync(new(Path.GetDirectoryName(Model.Path)!));
    }

    private void UninstallHandler(object sender, RoutedEventArgs args)
    {
        if (Model == null || !Model.ValidateGame())
        {
            MainWindow.Instance.ShowMainView();
            return;
        }

        if (!Model.MLInstalled)
            return;

        if (!MLManager.Uninstall(Path.GetDirectoryName(Model.Path)!, !KeepFilesCheck.IsChecked!.Value, out var error))
        {
            throw new Exception(error);
            // TODO: Display error message
        }

        Model.ValidateGame();
    }
}