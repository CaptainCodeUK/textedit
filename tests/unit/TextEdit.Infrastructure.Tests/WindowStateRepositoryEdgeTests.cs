using System;
using System.IO;
using System.Threading.Tasks;
using TextEdit.Infrastructure.Persistence;
using TextEdit.Core.Preferences;
using Xunit;

namespace TextEdit.Infrastructure.Tests;

public class WindowStateRepositoryEdgeTests : IDisposable
{
    private readonly WindowStateRepository _repository;
    private readonly string _statePath;
    private readonly string _stateDir;

    public WindowStateRepositoryEdgeTests()
    {
        _repository = new WindowStateRepository();
        _statePath = Path.Combine(AppPaths.BaseDir, "window-state.json");
        _stateDir = Path.GetDirectoryName(_statePath)!;
        if (File.Exists(_statePath)) File.Delete(_statePath);
    }

    [Fact]
    public async Task SaveAsync_WhenDirectoryReadOnly_DoesNotLeaveTempFile()
    {
        Directory.CreateDirectory(_stateDir);
        var originalMode = new DirectoryInfo(_stateDir).Attributes;
        try
        {
            new DirectoryInfo(_stateDir).Attributes |= FileAttributes.ReadOnly;
            var state = new WindowState { Width = 900, Height = 700 };
            await _repository.SaveAsync(state); // Should not throw, but temp file should be cleaned up
            Assert.False(File.Exists(_statePath + ".tmp"));
        }
        finally
        {
            new DirectoryInfo(_stateDir).Attributes = FileAttributes.Normal;
        }
    }

    public void Dispose()
    {
        try { if (File.Exists(_statePath)) File.Delete(_statePath); } catch { }
    }
}
