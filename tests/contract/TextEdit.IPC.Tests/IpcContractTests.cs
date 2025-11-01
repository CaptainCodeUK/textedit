using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace TextEdit.IPC.Tests;

/// <summary>
/// Contract tests for IPC message schemas.
/// Validates that request/response messages conform to JSON schemas in specs/001-text-editor/contracts/
/// </summary>
public class IpcContractTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void OpenFileDialog_Request_ValidatesAgainstSchema()
    {
        // Arrange
        var request = new { filters = new[] { "txt", "md" }, multi = false };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<OpenFileDialogRequest>(json, _jsonOptions);

        // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(new[] { "txt", "md" }, deserialized!.Filters);
    Assert.False(deserialized.Multi);
    }

    [Fact]
    public void OpenFileDialog_Response_ValidatesAgainstSchema()
    {
        // Arrange
        var response = new { canceled = false, filePaths = new[] { "/path/to/file.txt" } };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<OpenFileDialogResponse>(json, _jsonOptions);

        // Assert
    Assert.NotNull(deserialized);
    Assert.False(deserialized!.Canceled);
    var single = Assert.Single(deserialized.FilePaths);
    Assert.Equal("/path/to/file.txt", single);
    }

    [Fact]
    public void OpenFileDialog_Response_Canceled_ReturnsEmptyPaths()
    {
        // Arrange
        var response = new { canceled = true, filePaths = Array.Empty<string>() };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<OpenFileDialogResponse>(json, _jsonOptions);

        // Assert
    Assert.NotNull(deserialized);
    Assert.True(deserialized!.Canceled);
    Assert.Empty(deserialized.FilePaths);
    }

    [Fact]
    public void SaveFileDialog_Request_ValidatesAgainstSchema()
    {
        // Arrange
        var request = new { defaultPath = "/home/user/document.txt", filters = new[] { "txt", "md" } };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SaveFileDialogRequest>(json, _jsonOptions);

        // Assert
    Assert.NotNull(deserialized);
    Assert.Equal("/home/user/document.txt", deserialized!.DefaultPath);
    Assert.Equal(new[] { "txt", "md" }, deserialized.Filters);
    }

    [Fact]
    public void SaveFileDialog_Response_ValidatesAgainstSchema()
    {
        // Arrange
        var response = new { canceled = false, filePath = "/path/to/saved/file.txt" };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SaveFileDialogResponse>(json, _jsonOptions);

        // Assert
    Assert.NotNull(deserialized);
    Assert.False(deserialized!.Canceled);
    Assert.Equal("/path/to/saved/file.txt", deserialized.FilePath);
    }

    [Fact]
    public void SaveFileDialog_Response_Canceled_ReturnsNull()
    {
        // Arrange
        var response = new { canceled = true, filePath = (string?)null };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SaveFileDialogResponse>(json, _jsonOptions);

        // Assert
    Assert.NotNull(deserialized);
    Assert.True(deserialized!.Canceled);
    Assert.Null(deserialized.FilePath);
    }

    [Fact]
    public void PersistUnsaved_Request_ValidatesAgainstSchema()
    {
        // Arrange
        var request = new
        {
            records = new object[]
            {
                new { kind = "NewDocument", originalFilePath = (string?)null, content = "Unsaved text" },
                new { kind = "ExistingFilePatch", originalFilePath = (string?)"/path/to/file.txt", content = "Modified content" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<PersistUnsavedRequest>(json, _jsonOptions);

        // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(2, deserialized!.Records.Length);
    Assert.Equal("NewDocument", deserialized.Records[0].Kind);
    Assert.Equal("ExistingFilePatch", deserialized.Records[1].Kind);
    }

    [Fact]
    public void RestoreSession_Response_ValidatesAgainstSchema()
    {
        // Arrange
        var response = new
        {
            records = new object[]
            {
                new { kind = "NewDocument", originalFilePath = (string?)null, content = "Restored text" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<RestoreSessionResponse>(json, _jsonOptions);

        // Assert
    Assert.NotNull(deserialized);
    Assert.Single(deserialized!.Records);
    Assert.Equal("NewDocument", deserialized.Records[0].Kind);
    Assert.Equal("Restored text", deserialized.Records[0].Content);
    }
}

// DTOs matching contract schemas
public record OpenFileDialogRequest(
    [property: JsonPropertyName("filters")] string[]? Filters = null, 
    [property: JsonPropertyName("multi")] bool Multi = false);

public record OpenFileDialogResponse(
    [property: JsonPropertyName("canceled")] bool Canceled, 
    [property: JsonPropertyName("filePaths")] string[] FilePaths);

public record SaveFileDialogRequest(
    [property: JsonPropertyName("defaultPath")] string? DefaultPath = null, 
    [property: JsonPropertyName("filters")] string[]? Filters = null);

public record SaveFileDialogResponse(
    [property: JsonPropertyName("canceled")] bool Canceled, 
    [property: JsonPropertyName("filePath")] string? FilePath);

public record PersistUnsavedRequest(
    [property: JsonPropertyName("records")] PersistenceRecord[] Records);

public record RestoreSessionResponse(
    [property: JsonPropertyName("records")] PersistenceRecord[] Records);

public record PersistenceRecord(
    [property: JsonPropertyName("kind")] string Kind, 
    [property: JsonPropertyName("originalFilePath")] string? OriginalFilePath, 
    [property: JsonPropertyName("content")] string Content);
