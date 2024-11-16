using Avalonia.Controls;
using Avalonia.Threading;

namespace MelonLoader.Installer.Views;

public partial class UpdaterView : UserControl
{
    public UpdaterView()
    {
        InitializeComponent();

        Updater.Progress += (progress, _) => Dispatcher.UIThread.Post(() => OnProgress(progress));
    }

    private void OnProgress(double progress)
    {
        Progress.Value = progress * 100;
    }
}