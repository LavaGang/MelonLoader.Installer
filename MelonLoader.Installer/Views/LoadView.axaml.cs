using Avalonia.Controls;

namespace MelonLoader.Installer.Views;

public partial class LoadView : UserControl
{
    public LoadView()
    {
        InitializeComponent();

        Gif.Source = new($"avares://{typeof(LoadView).Assembly.GetName().Name}/Assets/loading-anim.gif");
    }

    public void SetProgress(double progress, string? newStatus)
    {
        Progress.Value = progress * 100;

        if (newStatus != null)
            Text.Text = newStatus;
    }
}