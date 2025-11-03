using Xunit;
using TextEdit.Core.Documents;
using TextEdit.Infrastructure.Persistence;
using System.Text;

namespace TextEdit.Core.Tests;

public class PersistenceServiceTests : IDisposable
{
    private readonly PersistenceService _service;
    private readonly string _testSessionDir;

    public PersistenceServiceTests()
    {
        // Create a unique temp directory per test run to avoid touching real user config
        _testSessionDir = Path.Combine(Path.GetTempPath(), "scrappy-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testSessionDir);
        _service = new PersistenceService(_testSessionDir);
    }

    [Fact]
    public async Task PersistAsync_WithDirtyDocument_SavesFullContent()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Unsaved content");
        var documents = new[] { doc };

        // Act
        await _service.PersistAsync(documents);

        // Assert
        var sessionFile = Path.Combine(_testSessionDir, $"{doc.Id}.json");
    Assert.True(File.Exists(sessionFile));
        var json = await File.ReadAllTextAsync(sessionFile);
    Assert.Contains("Unsaved content", json);
    Assert.Contains($"\"Id\":\"{doc.Id}\"", json);
    Assert.Contains("\"IsDirty\":true", json);
    }

    [Fact]
    public async Task PersistAsync_WithSavedDocument_SavesMetadataOnly()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");
        doc.MarkSaved("/test/file.txt");
        var documents = new[] { doc };

        // Act
        await _service.PersistAsync(documents);

        // Assert
        var sessionFile = Path.Combine(_testSessionDir, $"{doc.Id}.json");
    Assert.True(File.Exists(sessionFile));
        var json = await File.ReadAllTextAsync(sessionFile);
    Assert.Contains($"\"{doc.Id}\"", json);
    Assert.Contains("/test/file.txt", json);
    Assert.Contains("\"IsDirty\":false", json);
    // Content should be null or empty for saved files
    Assert.True(json.Contains("\"Content\":null") || json.Contains("\"Content\":\"\""));
    }

    [Fact]
    public async Task RestoreAsync_SkipsDocumentsWithEmptyGuid()
    {
        // Arrange
        var emptyGuidFile = Path.Combine(_testSessionDir, "00000000-0000-0000-0000-000000000000.json");
        var corruptDoc = new
        {
            Id = Guid.Empty,
            FilePath = (string?)null,
            Content = "Corrupt content",
            IsDirty = true,
            Encoding = "utf-8",
            Eol = "\n",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Order = 0
        };
        var json = System.Text.Json.JsonSerializer.Serialize(corruptDoc);
        await File.WriteAllTextAsync(emptyGuidFile, json);

        // Act
        var restored = await _service.RestoreAsync();

        // Assert
    Assert.Empty(restored);
    Assert.False(File.Exists(emptyGuidFile)); // Should be deleted
    }

    [Fact]
    public async Task RestoreAsync_WithDirtyFile_RestoresContentAndDirtyFlag()
    {
        // Arrange - create a temporary file so it exists during restore
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_modified_{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(tempFile, "Original content");

        var doc = new Document();
        doc.SetContent("Modified content");
        doc.MarkSaved(tempFile);
        doc.SetContent("More modifications");
        await _service.PersistAsync(new[] { doc });

        // Act
        var restored = (await _service.RestoreAsync()).ToList();

        // Assert
    Assert.Single(restored);
        var restoredDoc = restored[0];
    Assert.Equal("More modifications", restoredDoc.Content);
    Assert.True(restoredDoc.IsDirty);
    Assert.Equal(tempFile, restoredDoc.FilePath);

        // Cleanup
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RestoreAsync_WithUntitledDocument_RestoresContent()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Untitled content");
        await _service.PersistAsync(new[] { doc });

        // Act
        var restored = (await _service.RestoreAsync()).ToList();

        // Assert
    Assert.Single(restored);
        var restoredDoc = restored[0];
    Assert.Equal("Untitled content", restoredDoc.Content);
    Assert.True(restoredDoc.IsDirty);
    Assert.Null(restoredDoc.FilePath);
    }

    [Fact]
    public async Task RestoreAsync_PreservesDocumentOrder()
    {
        // Arrange
        var doc1 = new Document();
        doc1.SetContent("First");
        var doc2 = new Document();
        doc2.SetContent("Second");
        var doc3 = new Document();
        doc3.SetContent("Third");
        
        var tabOrder = new List<Guid> { doc1.Id, doc2.Id, doc3.Id };
        await _service.PersistAsync(new[] { doc1, doc2, doc3 }, tabOrder);

        // Act
        var restored = (await _service.RestoreAsync()).ToList();

        // Assert
    Assert.Equal(3, restored.Count);
    Assert.Equal("First", restored[0].Content);
    Assert.Equal("Second", restored[1].Content);
    Assert.Equal("Third", restored[2].Content);
    }

    [Fact]
    public void PersistEditorPreferences_SavesPreferences()
    {
        // Act
        _service.PersistEditorPreferences(wordWrap: true, showPreview: true);

        // Assert
        var prefsFile = Path.Combine(_testSessionDir, "editor-prefs.json");
    Assert.True(File.Exists(prefsFile));
        var json = File.ReadAllText(prefsFile);
    Assert.Contains("\"WordWrap\":true", json);
    Assert.Contains("\"ShowPreview\":true", json);
    }

    [Fact]
    public void RestoreEditorPreferences_WithExistingFile_ReturnsValues()
    {
        // Arrange
        _service.PersistEditorPreferences(wordWrap: false, showPreview: true);

        // Act
        var (wordWrap, showPreview) = _service.RestoreEditorPreferences();

        // Assert
    Assert.False(wordWrap);
    Assert.True(showPreview);
    }

    [Fact]
    public void RestoreEditorPreferences_WithoutFile_ReturnsDefaults()
    {
        // Arrange
        var prefsFile = Path.Combine(_testSessionDir, "editor-prefs.json");
        if (File.Exists(prefsFile))
        {
            File.Delete(prefsFile);
        }

        // Act
        var (wordWrap, showPreview) = _service.RestoreEditorPreferences();

        // Assert
    Assert.True(wordWrap); // Default
    Assert.False(showPreview); // Default
    }

    [Fact]
    public async Task PersistAsync_WithMultipleDocuments_SavesAll()
    {
        // Arrange
        var doc1 = new Document();
        doc1.SetContent("Doc 1");
        var doc2 = new Document();
        doc2.SetContent("Doc 2");
        var doc3 = new Document();
        doc3.SetContent("Doc 3");

        // Act
        await _service.PersistAsync(new[] { doc1, doc2, doc3 });

        // Assert
    Assert.True(File.Exists(Path.Combine(_testSessionDir, $"{doc1.Id}.json")));
    Assert.True(File.Exists(Path.Combine(_testSessionDir, $"{doc2.Id}.json")));
    Assert.True(File.Exists(Path.Combine(_testSessionDir, $"{doc3.Id}.json")));
    }

    [Fact]
    public void DeleteSessionFile_RemovesFile()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");
        _service.PersistAsync(new[] { doc }).Wait();
        var sessionFile = Path.Combine(_testSessionDir, $"{doc.Id}.json");
    Assert.True(File.Exists(sessionFile));

        // Act
        _service.DeleteSessionFile(doc.Id);

        // Assert
    Assert.False(File.Exists(sessionFile));
    }

    [Fact]
    public void ClearAllSessions_RemovesAllFiles()
    {
        // Arrange
        var doc1 = new Document();
        doc1.SetContent("Doc 1");
        var doc2 = new Document();
        doc2.SetContent("Doc 2");
        _service.PersistAsync(new[] { doc1, doc2 }).Wait();

        // Act
        _service.ClearAllSessions();

        // Assert
        var sessionFiles = Directory.GetFiles(_testSessionDir, "*.json")
            .Where(f => !f.EndsWith("editor-prefs.json"))
            .ToList();
    Assert.Empty(sessionFiles);
    }

    public void Dispose()
    {
        // Clean up test files
        try
        {
            _service.ClearAllSessions();
            if (Directory.Exists(_testSessionDir))
            {
                Directory.Delete(_testSessionDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
