using TextEdit.Core.Documents;
using TextEdit.Infrastructure.Ipc;

namespace TextEdit.UI.Components.Editor;

/// <summary>
/// Encapsulates common editor commands for reuse (menu wiring, toolbar, etc).
/// </summary>
public class EditorCommands
{
    private readonly DocumentService _docs;
    private readonly IpcBridge _ipc;

    public EditorCommands(DocumentService docs, IpcBridge ipc)
    {
        _docs = docs;
        _ipc = ipc;
    }

    public Document New() => _docs.NewDocument();

    public async Task<Document?> OpenAsync()
    {
        var path = await _ipc.ShowOpenFileDialogAsync();
        if (string.IsNullOrWhiteSpace(path)) return null;
        return await _docs.OpenAsync(path!);
    }

    public Task SaveAsync(Document doc) => _docs.SaveAsync(doc);

    public async Task<bool> SaveAsAsync(Document doc)
    {
        var path = await _ipc.ShowSaveFileDialogAsync();
        if (string.IsNullOrWhiteSpace(path)) return false;
        await _docs.SaveAsync(doc, path);
        return true;
    }
}
