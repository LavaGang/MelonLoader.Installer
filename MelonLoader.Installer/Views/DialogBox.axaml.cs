using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MelonLoader.Installer.ViewModels;

namespace MelonLoader.Installer.Views;

public partial class DialogBox : Window
{
    private Action? OnConfirm;
    private Action? OnCancel;

    public DialogBox()
        => InitializeComponent();

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        OnCancel?.Invoke();
    }

    public static void ShowError(string message, Action? onClose = null)
        => ShowError("ERROR!", message, onClose);
    public static void ShowError(string title, string message, Action? onClose = null)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrWhiteSpace(title))
            return;
        if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
            return;
        new DialogBox
        {
            Title = title,
            DataContext = new DialogBoxModel
            {
                Message = message,
                IsError = true
            },
            OnCancel = onClose
        }.Open();
    }

    public static void ShowNotice(string message)
        => ShowNotice("NOTICE", message);
    public static void ShowNotice(string title, string message)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrWhiteSpace(title))
            return;
        if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
            return;
        new DialogBox
        {
            Title = title,
            DataContext = new DialogBoxModel
            {
                Message = message
            }
        }.Open();
    }

    public static void ShowConfirmation(
        string message,
        Action? onConfirm = null,
        Action? onCancel = null,
        string confirmText = "Yes",
        string cancelText = "No")
        => ShowConfirmation("Confirmation",
            message,
            onConfirm,
            onCancel,
            confirmText,
            cancelText);

    public static void ShowConfirmation(
        string title,
        string message,
        Action? onConfirm = null,
        Action? onCancel = null,
        string confirmText = "Yes",
        string cancelText = "No")
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrWhiteSpace(title))
            return;
        if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
            return;
        if (string.IsNullOrEmpty(confirmText) || string.IsNullOrWhiteSpace(confirmText))
            return;
        if (string.IsNullOrEmpty(cancelText) || string.IsNullOrWhiteSpace(cancelText))
            return;
        new DialogBox
        {
            Title = title,
            DataContext = new DialogBoxModel
            {
                Message = message,
                IsConfirmation = true,
                ConfirmText = confirmText,
                CancelText = cancelText
            },
            OnConfirm = onConfirm,
            OnCancel = onCancel
        }.Open();
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
#if WINDOWS
        Program.GrabAttention();
#endif
        Focus();
    }

    private void ConfirmHandler(object sender, RoutedEventArgs args)
    {
        OnConfirm?.Invoke();
        OnConfirm = null;
        OnCancel = null;
        Close();
    }

    private void CancelHandler(object sender, RoutedEventArgs args)
    {
        OnCancel?.Invoke();
        OnConfirm = null;
        OnCancel = null;
        Close();
    }
}