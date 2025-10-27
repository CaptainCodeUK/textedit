namespace TextEdit.UI.App;

/// <summary>
/// Manages dialog display state for ErrorDialog and ConfirmDialog components
/// </summary>
public class DialogService
{
    public bool ShowError { get; private set; }
    public string ErrorTitle { get; private set; } = "Error";
    public string ErrorMessage { get; private set; } = "";

    public bool ShowConfirm { get; private set; }
    public string ConfirmTitle { get; private set; } = "Confirm";
    public string ConfirmMessage { get; private set; } = "";
    
    private TaskCompletionSource<bool>? _confirmTcs;

    public event Action? Changed;

    /// <summary>
    /// Show an error dialog with the given title and message
    /// </summary>
    public void ShowErrorDialog(string title, string message)
    {
        ErrorTitle = title;
        ErrorMessage = message;
        ShowError = true;
        Changed?.Invoke();
    }

    /// <summary>
    /// Hide the error dialog
    /// </summary>
    public void HideErrorDialog()
    {
        ShowError = false;
        Changed?.Invoke();
    }

    /// <summary>
    /// Show a confirmation dialog and return user's choice
    /// </summary>
    public Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        ConfirmTitle = title;
        ConfirmMessage = message;
        ShowConfirm = true;
        _confirmTcs = new TaskCompletionSource<bool>();
        Changed?.Invoke();
        return _confirmTcs.Task;
    }

    /// <summary>
    /// Handle result from confirm dialog
    /// </summary>
    public void HandleConfirmResult(bool result)
    {
        ShowConfirm = false;
        _confirmTcs?.SetResult(result);
        _confirmTcs = null;
        Changed?.Invoke();
    }
}
