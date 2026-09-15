using BudgetManager.Domain.Entities;
using BudgetManager.Domain.ValueObjects;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence;

[Collection(SqlServerCollection.Name)]
public sealed class ModelConfigurationTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public void Account_Iban_IsMappedAsComplexTypeWithExpectedColumnAndLength()
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(Account));

        Assert.NotNull(entityType);

        var complexProperty =
            entityType.FindComplexProperty(
                nameof(Account.Iban));

        Assert.NotNull(complexProperty);

        var valueProperty =
            complexProperty.ComplexType.FindProperty(
                nameof(Iban.Value));

        Assert.NotNull(valueProperty);

        // Assert

        Assert.Equal(
            "Iban",
            valueProperty.GetColumnName());

        Assert.Equal(
            34,
            valueProperty.GetMaxLength());

        Assert.Equal(
            ApplicationDbContext.StringComparisonCollation,
            valueProperty.GetCollation());
    }

    [Fact]
    public void Bank_Bic_IsMappedAsComplexTypeWithExpectedColumnAndLength()
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(Bank));

        Assert.NotNull(entityType);

        var complexProperty =
            entityType.FindComplexProperty(
                nameof(Bank.Bic));

        Assert.NotNull(complexProperty);

        var valueProperty =
            complexProperty.ComplexType.FindProperty(
                nameof(Bic.Value));

        Assert.NotNull(valueProperty);

        // Assert

        Assert.Equal(
            "Bic",
            valueProperty.GetColumnName());

        Assert.Equal(
            11,
            valueProperty.GetMaxLength());

        Assert.Equal(
            ApplicationDbContext.StringComparisonCollation,
            valueProperty.GetCollation());
    }

    [Theory]
    [InlineData(typeof(Account), nameof(Account.Name))]
    [InlineData(typeof(Bank), nameof(Bank.Name))]
    [InlineData(typeof(Budget), nameof(Budget.Name))]
    [InlineData(typeof(BudgetCategory), nameof(BudgetCategory.Name))]
    [InlineData(typeof(BudgetCategory), nameof(BudgetCategory.Description))]
    [InlineData(typeof(Transaction), nameof(Transaction.Name))]
    [InlineData(typeof(ApplicationUser), nameof(ApplicationUser.UserName))]
    [InlineData(typeof(ApplicationUser), nameof(ApplicationUser.Email))]
    [InlineData(typeof(ApplicationUser), nameof(ApplicationUser.LastName))]
    [InlineData(typeof(ApplicationUser), nameof(ApplicationUser.FirstName))]
    [InlineData(typeof(ApplicationRole), nameof(ApplicationRole.Name))]
    public void SearchableStringProperty_UsesExpectedCollation(
    Type entityType,
    string propertyName)
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var metadata =
            model.FindEntityType(
                entityType);

        Assert.NotNull(metadata);

        var property =
            metadata.FindProperty(
                propertyName);

        Assert.NotNull(property);

        // Assert

        Assert.Equal(
            ApplicationDbContext.StringComparisonCollation,
            property.GetCollation());
    }

    [Theory]
    [InlineData(typeof(Account))]
    [InlineData(typeof(Bank))]
    [InlineData(typeof(Budget))]
    [InlineData(typeof(BudgetCategory))]
    public void AggregateRoot_RowVersion_IsConfiguredAsConcurrencyToken(
        Type entityType)
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var metadata =
            model.FindEntityType(
                entityType);

        Assert.NotNull(metadata);

        var rowVersion =
            metadata.FindProperty(
                "RowVersion");

        Assert.NotNull(rowVersion);

        // Assert

        Assert.True(
            rowVersion.IsConcurrencyToken);

        Assert.Equal(
            ValueGenerated.OnAddOrUpdate,
            rowVersion.ValueGenerated);
    }

    [Fact]
    public void Transaction_Amount_UsesExpectedPrecisionAndScale()
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(Transaction));

        Assert.NotNull(entityType);

        var amount =
            entityType.FindProperty(
                nameof(Transaction.Amount));

        Assert.NotNull(amount);

        // Assert

        Assert.Equal(
            18,
            amount.GetPrecision());

        Assert.Equal(
            2,
            amount.GetScale());
    }

    [Fact]
    public void BudgetAccess_OwnerIndex_IsUniqueAndFiltered()
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(BudgetAccess));

        Assert.NotNull(entityType);

        var index =
            entityType.GetIndexes()
                .Single(x =>
                    x.Properties.Count == 1 &&
                    x.Properties[0].Name == nameof(BudgetAccess.BudgetId) &&
                    x.IsUnique);

        // Assert

        Assert.Equal(
            "[IsOwner] = 1",
            index.GetFilter());
    }

    [Fact]
    public void Transaction_BudgetRelationship_UsesCascadeDeleteBehavior()
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(Transaction));

        Assert.NotNull(entityType);

        var foreignKey =
            entityType.GetForeignKeys()
                .Single(x =>
                    x.Properties.Single().Name ==
                    nameof(Transaction.BudgetId));

        // Assert

        Assert.Equal(
            DeleteBehavior.Cascade,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Transaction_NonAggregateRelationships_UseRestrictDeleteBehavior()
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(Transaction));

        Assert.NotNull(entityType);

        var foreignKeys =
            entityType.GetForeignKeys()
                .Where(x =>
                    x.Properties.Single().Name !=
                    nameof(Transaction.BudgetId))
                .ToArray();

        // Assert

        Assert.Equal(
            3,
            foreignKeys.Length);

        Assert.All(
            foreignKeys,
            foreignKey => Assert.Equal(
                DeleteBehavior.Restrict,
                foreignKey.DeleteBehavior));
    }

    [Fact]
    public void BudgetAccess_UserRelationship_UsesRestrictDeleteBehavior()
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(BudgetAccess));

        Assert.NotNull(entityType);

        var userForeignKey =
            entityType.GetForeignKeys()
                .Single(x =>
                    x.Properties.Single().Name ==
                    nameof(BudgetAccess.UserId));

        // Assert

        Assert.Equal(
            DeleteBehavior.Restrict,
            userForeignKey.DeleteBehavior);
    }

    [Fact]
    public void BudgetAccess_BudgetRelationship_UsesCascadeDeleteBehavior()
    {
        // Arrange

        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(BudgetAccess));

        Assert.NotNull(entityType);

        var budgetForeignKey =
            entityType.GetForeignKeys()
                .Single(x =>
                    x.Properties.Single().Name ==
                    nameof(BudgetAccess.BudgetId));

        // Assert

        Assert.Equal(
            DeleteBehavior.Cascade,
            budgetForeignKey.DeleteBehavior);
    }

    [Fact]
    public void ApplicationUser_Preferences_HaveExpectedLengthsAndRequiredness()
    {
        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(ApplicationUser));

        Assert.NotNull(entityType);

        var culture =
            entityType.FindProperty(
                nameof(ApplicationUser.PreferredCulture));

        var theme =
            entityType.FindProperty(
                nameof(ApplicationUser.PreferredTheme));

        Assert.NotNull(culture);
        Assert.NotNull(theme);

        Assert.Equal(10, culture.GetMaxLength());
        Assert.False(culture.IsNullable);

        Assert.Equal(10, theme.GetMaxLength());
        Assert.True(theme.IsNullable);
    }

    [Fact]
    public void ApplicationUserProfilePicture_IsOneToOneWithUserAndUsesCascadeDelete()
    {
        var model =
            Context
                .GetService<IDesignTimeModel>()
                .Model;

        var entityType =
            model.FindEntityType(
                typeof(ApplicationUserProfilePicture));

        Assert.NotNull(entityType);

        var primaryKey =
            entityType.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Equal(
            nameof(ApplicationUserProfilePicture.UserId),
            Assert.Single(primaryKey.Properties).Name);

        var contentType =
            entityType.FindProperty(
                nameof(ApplicationUserProfilePicture.ContentType));

        Assert.NotNull(contentType);
        Assert.Equal(50, contentType.GetMaxLength());
        Assert.False(contentType.IsNullable);

        var foreignKey =
            Assert.Single(entityType.GetForeignKeys());

        Assert.Equal(
            DeleteBehavior.Cascade,
            foreignKey.DeleteBehavior);

        Assert.True(
            foreignKey.IsUnique);
    }


}
