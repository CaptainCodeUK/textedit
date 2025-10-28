namespace TextEdit.Infrastructure.FileSystem;

using System.Text;
using System.IO;
using TextEdit.Core.Abstractions;

/// <summary>
/// Concrete file system operations with optimized large file handling.
/// </summary>
public class FileSystemService : IFileSystem
{
    private const int ChunkSize = 4096 * 16; // 64KB chunks for streaming
    
    public bool FileExists(string path) => System.IO.File.Exists(path);

    public long GetFileSize(string path)
    {
        var info = new FileInfo(path);
        return info.Exists ? info.Length : 0;
    }

    public Task<string> ReadAllTextAsync(string path, Encoding encoding)
        => System.IO.File.ReadAllTextAsync(path, encoding);

    public Task WriteAllTextAsync(string path, string contents, Encoding encoding)
        => System.IO.File.WriteAllTextAsync(path, contents, encoding);

    /// <summary>
    /// Read large file in chunks with progress reporting for better memory efficiency.
    /// </summary>
    public async Task<string> ReadLargeFileAsync(string path, Encoding encoding, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("File not found", path);
        
        var fileSize = fileInfo.Length;
        var buffer = new char[ChunkSize];
        var sb = new StringBuilder((int)Math.Min(fileSize, int.MaxValue / 2));
        
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSize, useAsync: true);
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, ChunkSize);
        
        int charsRead;
        
        while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            sb.Append(buffer, 0, charsRead);
            
            // Report progress as percentage
            if (progress != null && fileSize > 0)
            {
                    var percentage = (int)((stream.Position * 100) / fileSize);
                progress.Report(Math.Min(percentage, 100));
            }
        }
        
        progress?.Report(100);
        return sb.ToString();
    }

    /// <summary>
    /// Write large file in chunks with progress reporting for better memory efficiency.
    /// </summary>
    public async Task WriteLargeFileAsync(string path, string contents, Encoding encoding, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var totalLength = contents.Length;
        
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, ChunkSize, useAsync: true);
        using var writer = new StreamWriter(stream, encoding, ChunkSize);
        
        int offset = 0;
        while (offset < totalLength)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var remaining = totalLength - offset;
            var chunkLength = Math.Min(ChunkSize, remaining);
            
            await writer.WriteAsync(contents.AsMemory(offset, chunkLength), cancellationToken);
            offset += chunkLength;
            
            // Report progress as percentage
            if (progress != null)
            {
                var percentage = (int)((offset * 100L) / totalLength);
                progress.Report(Math.Min(percentage, 100));
            }
        }
        
        await writer.FlushAsync();
        progress?.Report(100);
    }
}
