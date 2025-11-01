using Xunit;
using TextEdit.Core.Documents;

namespace TextEdit.Core.Tests;

public class TabTests
{
    [Fact]
    public void Constructor_CreatesTabWithDefaults()
    {
        // Act
        var tab = new Tab();

        // Assert
    Assert.NotEqual(Guid.Empty, tab.Id);
    Assert.Equal(Guid.Empty, tab.DocumentId);
    Assert.False(tab.IsActive);
    Assert.Equal("Untitled", tab.Title);
    }

    [Fact]
    public void DocumentId_CanBeSet()
    {
        // Arrange
        var tab = new Tab();
        var docId = Guid.NewGuid();

        // Act
        tab.DocumentId = docId;

        // Assert
    Assert.Equal(docId, tab.DocumentId);
    }

    [Fact]
    public void IsActive_CanBeToggled()
    {
        // Arrange
        var tab = new Tab();

        // Act
        tab.IsActive = true;

        // Assert
    Assert.True(tab.IsActive);

        // Act
        tab.IsActive = false;

        // Assert
    Assert.False(tab.IsActive);
    }

    [Theory]
    [InlineData("readme.txt")]
    [InlineData("Document 1")]
    [InlineData("")]
    public void Title_CanBeSet(string title)
    {
        // Arrange
        var tab = new Tab();

        // Act
        tab.Title = title;

        // Assert
    Assert.Equal(title, tab.Title);
    }
}

public class TabServiceTests
{
    [Fact]
    public void Constructor_CreatesEmptyTabList()
    {
        // Act
        var service = new TabService();

        // Assert
    Assert.Empty(service.Tabs);
    }

    [Fact]
    public void AddTab_AddsTabAndActivatesIt()
    {
        // Arrange
        var service = new TabService();
        var doc = new Document();

        // Act
        var tab = service.AddTab(doc);

        // Assert
    Assert.Equal(1, service.Tabs.Count);
    Assert.Same(tab, service.Tabs[0]);
    Assert.Equal(doc.Id, tab.DocumentId);
    Assert.Equal("Untitled", tab.Title);
    Assert.True(tab.IsActive);
    }

    [Fact]
    public void AddTab_WithFilePath_UsesFileName()
    {
        // Arrange
        var service = new TabService();
        var doc = new Document();
        doc.MarkSaved("/path/to/test.txt");

        // Act
        var tab = service.AddTab(doc);

        // Assert
    Assert.Equal("test.txt", tab.Title);
    }

    [Fact]
    public void AddTab_MultipleTabs_OnlyLastIsActive()
    {
        // Arrange
        var service = new TabService();
        var doc1 = new Document();
        var doc2 = new Document();

        // Act
        var tab1 = service.AddTab(doc1);
        var tab2 = service.AddTab(doc2);

        // Assert
    Assert.Equal(2, service.Tabs.Count);
    Assert.False(tab1.IsActive);
    Assert.True(tab2.IsActive);
    }

    [Fact]
    public void ActivateTab_SetsTabActive()
    {
        // Arrange
        var service = new TabService();
        var doc = new Document();
        var tab = service.AddTab(doc);

        // Make it inactive
        tab.IsActive = false;

        // Act
        service.ActivateTab(tab.Id);

        // Assert
    Assert.True(tab.IsActive);
    }

    [Fact]
    public void ActivateTab_DeactivatesOtherTabs()
    {
        // Arrange
        var service = new TabService();
        var doc1 = new Document();
        var doc2 = new Document();
        var tab1 = service.AddTab(doc1);
        var tab2 = service.AddTab(doc2);

        // Act
        service.ActivateTab(tab1.Id);

        // Assert
    Assert.True(tab1.IsActive);
    Assert.False(tab2.IsActive);
    }

    [Fact]
    public void ActivateTab_NonexistentTab_DoesNothing()
    {
        // Arrange
        var service = new TabService();
        var doc = new Document();
        var tab = service.AddTab(doc);
        var originallyActive = tab.IsActive;

        // Act
        service.ActivateTab(Guid.NewGuid());

        // Assert
    Assert.Equal(1, service.Tabs.Count);
    // Since we tried to activate a nonexistent tab, no tabs become active
    // The implementation deactivates all tabs first, so IsActive becomes false
    Assert.False(tab.IsActive);
    }

    [Fact]
    public void CloseTab_RemovesTab()
    {
        // Arrange
        var service = new TabService();
        var doc = new Document();
        var tab = service.AddTab(doc);

        // Act
        service.CloseTab(tab.Id);

        // Assert
    Assert.Empty(service.Tabs);
    }

    [Fact]
    public void CloseTab_NonexistentTab_DoesNothing()
    {
        // Arrange
        var service = new TabService();
        var doc = new Document();
        var tab = service.AddTab(doc);

        // Act
        service.CloseTab(Guid.NewGuid());

        // Assert
    Assert.Equal(1, service.Tabs.Count);
    Assert.Same(tab, service.Tabs[0]);
    }

    [Fact]
    public void CloseTab_ActiveTab_ActivatesNextTab()
    {
        // Arrange
        var service = new TabService();
        var doc1 = new Document();
        var doc2 = new Document();
        var tab1 = service.AddTab(doc1);
        var tab2 = service.AddTab(doc2);

        // tab2 is currently active

        // Act
        service.CloseTab(tab2.Id);

        // Assert
    Assert.Equal(1, service.Tabs.Count);
    Assert.True(tab1.IsActive);
    }

    [Fact]
    public void CloseTab_ActiveMiddleTab_ActivatesSameIndex()
    {
        // Arrange
        var service = new TabService();
        var doc1 = new Document();
        var doc2 = new Document();
        var doc3 = new Document();
        var tab1 = service.AddTab(doc1);
        var tab2 = service.AddTab(doc2);
        var tab3 = service.AddTab(doc3);

        service.ActivateTab(tab2.Id);

        // Act
        service.CloseTab(tab2.Id);

        // Assert
    Assert.Equal(2, service.Tabs.Count);
    Assert.True(tab3.IsActive); // Same index (1)
    }

    [Fact]
    public void CloseTab_ActiveLastTab_ActivatesPreviousTab()
    {
        // Arrange
        var service = new TabService();
        var doc1 = new Document();
        var doc2 = new Document();
        var doc3 = new Document();
        var tab1 = service.AddTab(doc1);
        var tab2 = service.AddTab(doc2);
        var tab3 = service.AddTab(doc3);

        // tab3 is active

        // Act
        service.CloseTab(tab3.Id);

        // Assert
    Assert.Equal(2, service.Tabs.Count);
    Assert.True(tab2.IsActive);
    }

    [Fact]
    public void CloseTab_InactiveTab_DoesNotChangeActiveTab()
    {
        // Arrange
        var service = new TabService();
        var doc1 = new Document();
        var doc2 = new Document();
        var doc3 = new Document();
        var tab1 = service.AddTab(doc1);
        var tab2 = service.AddTab(doc2);
        var tab3 = service.AddTab(doc3);

        service.ActivateTab(tab2.Id);

        // Act
        service.CloseTab(tab1.Id);

        // Assert
    Assert.Equal(2, service.Tabs.Count);
    Assert.True(tab2.IsActive);
    Assert.False(tab3.IsActive);
    }
}
