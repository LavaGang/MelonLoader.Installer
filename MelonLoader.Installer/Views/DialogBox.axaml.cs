using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MelonLoader.Installer.Views;

public partial class DialogBox : Window
{
    public delegate void dCallback(object sender, RoutedEventArgs args);
    private dCallback? OnConfirm;
    private dCallback? OnCancel;

    private static Bitmap? ErrorIcon = new Bitmap(AssetLoader.Open(new("avares://" + typeof(GameControl).Assembly.GetName().Name + "/Assets/error.png")));

    public DialogBox()
        => InitializeComponent();

    public static void ShowError(string message)
    {
        DialogBox dialogBox = new DialogBox();
        dialogBox.Title = "Error";
        dialogBox.Message.Text = message;
        dialogBox.HeaderImage.Source = ErrorIcon;
        dialogBox.Open();
    }

    public static void ShowNotice(string message)
        => ShowNotice("Notice", message);
    public static void ShowNotice(string title, string message)
    {
        DialogBox dialogBox = new DialogBox();
        dialogBox.Title = title;
        dialogBox.Message.Text = message;
        dialogBox.Open();
    }

    public static void ShowConfirmation(
        string message, 
        dCallback? onConfirm = null, 
        dCallback? onCancel = null,
        string confirmText = "YES",
        string cancelText = "NO")
        => ShowConfirmation("Confirmation", 
            message, 
            onConfirm, 
            onCancel, 
            confirmText, 
            cancelText);

    public static void ShowConfirmation(
        string title,
        string message, 
        dCallback? onConfirm = null, 
        dCallback? onCancel = null,
        string confirmText = "YES",
        string cancelText = "NO")
    {
        DialogBox dialogBox = new DialogBox();
        
        dialogBox.Title = title;
        dialogBox.Message.Text = message;

        dialogBox.ConfirmButton.Content = confirmText;

        dialogBox.CancelButton.IsVisible = true;
        dialogBox.CancelButton.Content = cancelText;

        dialogBox.OnConfirm = onConfirm;
        dialogBox.OnCancel = onCancel;

        dialogBox.NoticeGrid.IsVisible = false;
        dialogBox.ConfirmationGrid.IsVisible = true;

        dialogBox.Open();
    }

    private void Open()
    {
        BringToFront();

        if (MainWindow.Instance.IsVisible)
            ShowDialog(MainWindow.Instance);
        else
            Show();
    }

    private void BringToFront()
    {
        Topmost = true;
        Topmost = false;
        Program.GrabAttention();
        Focus();
    }

    private void ConfirmHandler(object sender, RoutedEventArgs args)
    {
        Close();
        OnConfirm?.Invoke(sender, args);
    }

    private void CancelHandler(object sender, RoutedEventArgs args)
    {
        Close();
        OnCancel?.Invoke(sender, args);
    }
}