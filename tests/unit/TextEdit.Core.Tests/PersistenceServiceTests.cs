using FluentAssertions;
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
        _service = new PersistenceService();
        _testSessionDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TextEdit", "Session");
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
        File.Exists(sessionFile).Should().BeTrue();
        var json = await File.ReadAllTextAsync(sessionFile);
        json.Should().Contain("Unsaved content");
        json.Should().Contain($"\"Id\":\"{doc.Id}\"");
        json.Should().Contain("\"IsDirty\":true");
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
        File.Exists(sessionFile).Should().BeTrue();
        var json = await File.ReadAllTextAsync(sessionFile);
        json.Should().Contain($"\"{doc.Id}\"");
        json.Should().Contain("/test/file.txt");
        json.Should().Contain("\"IsDirty\":false");
        // Content should be null or empty for saved files
        json.Should().Match(s => s.Contains("\"Content\":null") || s.Contains("\"Content\":\"\""));
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
        restored.Should().BeEmpty();
        File.Exists(emptyGuidFile).Should().BeFalse(); // Should be deleted
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
        restored.Should().HaveCount(1);
        var restoredDoc = restored[0];
        restoredDoc.Content.Should().Be("More modifications");
        restoredDoc.IsDirty.Should().BeTrue();
        restoredDoc.FilePath.Should().Be(tempFile);

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
        restored.Should().HaveCount(1);
        var restoredDoc = restored[0];
        restoredDoc.Content.Should().Be("Untitled content");
        restoredDoc.IsDirty.Should().BeTrue();
        restoredDoc.FilePath.Should().BeNull();
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
        restored.Should().HaveCount(3);
        restored[0].Content.Should().Be("First");
        restored[1].Content.Should().Be("Second");
        restored[2].Content.Should().Be("Third");
    }

    [Fact]
    public void PersistEditorPreferences_SavesPreferences()
    {
        // Act
        _service.PersistEditorPreferences(wordWrap: true, showPreview: true);

        // Assert
        var prefsFile = Path.Combine(_testSessionDir, "editor-prefs.json");
        File.Exists(prefsFile).Should().BeTrue();
        var json = File.ReadAllText(prefsFile);
        json.Should().Contain("\"WordWrap\":true");
        json.Should().Contain("\"ShowPreview\":true");
    }

    [Fact]
    public void RestoreEditorPreferences_WithExistingFile_ReturnsValues()
    {
        // Arrange
        _service.PersistEditorPreferences(wordWrap: false, showPreview: true);

        // Act
        var (wordWrap, showPreview) = _service.RestoreEditorPreferences();

        // Assert
        wordWrap.Should().BeFalse();
        showPreview.Should().BeTrue();
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
        wordWrap.Should().BeTrue(); // Default
        showPreview.Should().BeFalse(); // Default
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
        File.Exists(Path.Combine(_testSessionDir, $"{doc1.Id}.json")).Should().BeTrue();
        File.Exists(Path.Combine(_testSessionDir, $"{doc2.Id}.json")).Should().BeTrue();
        File.Exists(Path.Combine(_testSessionDir, $"{doc3.Id}.json")).Should().BeTrue();
    }

    [Fact]
    public void DeleteSessionFile_RemovesFile()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");
        _service.PersistAsync(new[] { doc }).Wait();
        var sessionFile = Path.Combine(_testSessionDir, $"{doc.Id}.json");
        File.Exists(sessionFile).Should().BeTrue();

        // Act
        _service.DeleteSessionFile(doc.Id);

        // Assert
        File.Exists(sessionFile).Should().BeFalse();
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
        sessionFiles.Should().BeEmpty();
    }

    public void Dispose()
    {
        // Clean up test files
        try
        {
            _service.ClearAllSessions();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
