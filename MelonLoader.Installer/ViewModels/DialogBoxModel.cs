namespace MelonLoader.Installer.ViewModels;

public class DialogBoxModel : ViewModelBase
{
    public required string Message { get; init; }
    public string ConfirmText { get; init; } = "YES";
    public string CancelText { get; init; } = "NO";
    public bool IsError { get; init; }
    public bool IsConfirmation { get; init; }
}
