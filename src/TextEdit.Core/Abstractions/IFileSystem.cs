namespace TextEdit.Core.Abstractions;

using System.Text;

/// <summary>
/// Abstraction for basic file system operations used by core services.
/// Implemented in Infrastructure.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    Task<string> ReadAllTextAsync(string path, Encoding encoding);
    Task WriteAllTextAsync(string path, string contents, Encoding encoding);
}
