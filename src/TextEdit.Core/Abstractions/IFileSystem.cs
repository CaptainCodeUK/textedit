namespace TextEdit.Core.Abstractions;

using System.Text;

/// <summary>
/// Abstraction for basic file system operations used by core services.
/// Implemented in Infrastructure.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    long GetFileSize(string path);
    Task<string> ReadAllTextAsync(string path, Encoding encoding);
    Task WriteAllTextAsync(string path, string contents, Encoding encoding);
    
    /// <summary>
    /// Read a large file in chunks with progress reporting.
    /// For files >10MB, this provides better memory efficiency.
    /// </summary>
    Task<string> ReadLargeFileAsync(string path, Encoding encoding, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Write a large file in chunks with progress reporting.
    /// For content >10MB, this provides better memory efficiency.
    /// </summary>
    Task WriteLargeFileAsync(string path, string contents, Encoding encoding, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}
