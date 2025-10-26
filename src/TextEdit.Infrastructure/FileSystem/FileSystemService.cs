namespace TextEdit.Infrastructure.FileSystem;

using System.Text;
using System.IO;
using TextEdit.Core.Abstractions;

/// <summary>
/// Concrete file system operations.
/// </summary>
public class FileSystemService : IFileSystem
{
    public bool FileExists(string path) => System.IO.File.Exists(path);

    public Task<string> ReadAllTextAsync(string path, Encoding encoding)
        => System.IO.File.ReadAllTextAsync(path, encoding);

    public Task WriteAllTextAsync(string path, string contents, Encoding encoding)
        => System.IO.File.WriteAllTextAsync(path, contents, encoding);
}
