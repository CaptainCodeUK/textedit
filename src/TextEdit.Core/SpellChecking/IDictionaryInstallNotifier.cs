namespace TextEdit.Core.SpellChecking;

/// <summary>
/// Represents the state of a background dictionary download/install operation.
/// Shared between UI and background services so infrastructure doesn't need to reference UI types.
/// </summary>
public enum DictionaryDownloadState
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

/// <summary>
/// Lightweight notifier abstraction implemented by UI-facing AppState so background services
/// can report progress without depending on the UI assembly.
/// </summary>
public interface IDictionaryInstallNotifier
{
    /// <summary>
    /// Update the current dictionary install state and optionally provide a human-readable message.
    /// </summary>
    System.Threading.Tasks.Task SetDictionaryInstallStateAsync(DictionaryDownloadState state, string? message = null);
}
