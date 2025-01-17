namespace MelonLoader.Installer.ViewModels;

public class DialogBoxModel : ViewModelBase
{
    public required string Message { get; init; }
    public string ConfirmText { get; init; } = "Yes";
    public string CancelText { get; init; } = "No";
    public bool IsError { get; init; }
    public bool IsConfirmation { get; init; }
}
