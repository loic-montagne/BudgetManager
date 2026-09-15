using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities;
using Xunit;

namespace BudgetManager.Domain.Tests.Entities;

public sealed class BudgetCategoryTests
{
    [Fact]
    public void Category_TrimsEditableValues()
    {
        var category = BudgetCategory.Create("  Courses ", "  Alimentation ");

        category.Rename("  Quotidien ");
        category.ChangeDescription("  Dépenses courantes ");

        Assert.Equal("Quotidien", category.Name);
        Assert.Equal("Dépenses courantes", category.Description);
    }

    [Fact]
    public void Create_WhenNameHasMaximumLength_CreatesCategory()
    {
        // Arrange

        var name = new string(
            'C',
            StringPropertyLengths.NameLength);

        // Act

        var category = BudgetCategory.Create(
            name,
            null);

        // Assert

        Assert.Equal(
            name,
            category.Name);
    }

    [Fact]
    public void Create_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var name = new string(
            'C',
            StringPropertyLengths.NameLength + 1);

        // Act

        var action = () =>
            BudgetCategory.Create(
                name,
                null);

        // Assert

        Assert.Throws<ArgumentException>(
            action);
    }

    [Fact]
    public void Create_WhenDescriptionHasMaximumLength_CreatesCategory()
    {
        // Arrange

        var description = new string(
            'D',
            StringPropertyLengths.DescriptionLength);

        // Act

        var category = BudgetCategory.Create(
            "Catégorie",
            description);

        // Assert

        Assert.Equal(
            description,
            category.Description);
    }

    [Fact]
    public void Create_WhenDescriptionExceedsMaximumLength_Throws()
    {
        // Arrange

        var description = new string(
            'D',
            StringPropertyLengths.DescriptionLength + 1);

        // Act

        var action = () =>
            BudgetCategory.Create(
                "Catégorie",
                description);

        // Assert

        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "description",
            exception.ParamName);
    }

    [Fact]
    public void Rename_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var category = BudgetCategory.Create(
            "Catégorie",
            null);

        var name = new string(
            'C',
            StringPropertyLengths.NameLength + 1);

        // Act

        var action = () =>
            category.Rename(name);

        // Assert

        Assert.Throws<ArgumentException>(
            action);
    }

    [Fact]
    public void ChangeDescription_WhenDescriptionHasMaximumLength_ChangesDescription()
    {
        // Arrange

        var category = BudgetCategory.Create(
            "Catégorie",
            null);

        var description = new string(
            'D',
            StringPropertyLengths.DescriptionLength);

        // Act

        category.ChangeDescription(
            description);

        // Assert

        Assert.Equal(
            description,
            category.Description);
    }

    [Fact]
    public void ChangeDescription_WhenDescriptionExceedsMaximumLength_Throws()
    {
        // Arrange

        var category = BudgetCategory.Create(
            "Catégorie",
            null);

        var description = new string(
            'D',
            StringPropertyLengths.DescriptionLength + 1);

        // Act

        var action = () =>
            category.ChangeDescription(
                description);

        // Assert

        Assert.Throws<ArgumentException>(
            action);
    }
}
