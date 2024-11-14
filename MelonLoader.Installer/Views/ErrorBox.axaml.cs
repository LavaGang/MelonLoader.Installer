using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MelonLoader.Installer.Views;

public partial class ErrorBox : Window
{
    public ErrorBox()
    {
        InitializeComponent();
    }

    public static void Open(string message)
    {
        var box = new ErrorBox();
        box.Message.Text = message;
        box.Topmost = true;
        box.Topmost = false;

        Program.GrabAttention();
        box.Focus();

        if (MainWindow.Instance.IsVisible)
            box.ShowDialog(MainWindow.Instance);
        else
            box.Show();
    }

    private void OkHandler(object sender, RoutedEventArgs args)
    {
        Close();
    }
}