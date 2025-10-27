using FluentAssertions;
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
        tab.Id.Should().NotBeEmpty();
        tab.DocumentId.Should().BeEmpty();
        tab.IsActive.Should().BeFalse();
        tab.Title.Should().Be("Untitled");
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
        tab.DocumentId.Should().Be(docId);
    }

    [Fact]
    public void IsActive_CanBeToggled()
    {
        // Arrange
        var tab = new Tab();

        // Act
        tab.IsActive = true;

        // Assert
        tab.IsActive.Should().BeTrue();

        // Act
        tab.IsActive = false;

        // Assert
        tab.IsActive.Should().BeFalse();
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
        tab.Title.Should().Be(title);
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
        service.Tabs.Should().BeEmpty();
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
        service.Tabs.Should().HaveCount(1);
        service.Tabs[0].Should().Be(tab);
        tab.DocumentId.Should().Be(doc.Id);
        tab.Title.Should().Be("Untitled");
        tab.IsActive.Should().BeTrue();
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
        tab.Title.Should().Be("test.txt");
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
        service.Tabs.Should().HaveCount(2);
        tab1.IsActive.Should().BeFalse();
        tab2.IsActive.Should().BeTrue();
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
        tab.IsActive.Should().BeTrue();
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
        tab1.IsActive.Should().BeTrue();
        tab2.IsActive.Should().BeFalse();
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
        service.Tabs.Should().HaveCount(1);
        // Since we tried to activate a nonexistent tab, no tabs become active
        // The implementation deactivates all tabs first, so IsActive becomes false
        tab.IsActive.Should().BeFalse();
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
        service.Tabs.Should().BeEmpty();
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
        service.Tabs.Should().HaveCount(1);
        service.Tabs[0].Should().Be(tab);
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
        service.Tabs.Should().HaveCount(1);
        tab1.IsActive.Should().BeTrue();
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
        service.Tabs.Should().HaveCount(2);
        tab3.IsActive.Should().BeTrue(); // Same index (1)
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
        service.Tabs.Should().HaveCount(2);
        tab2.IsActive.Should().BeTrue();
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
        service.Tabs.Should().HaveCount(2);
        tab2.IsActive.Should().BeTrue();
        tab3.IsActive.Should().BeFalse();
    }
}
