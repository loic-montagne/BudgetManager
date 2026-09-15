using BudgetManager.Application.Common;
using BudgetManager.Infrastructure.Configuration;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Seed;

public sealed class UserSeedOptionsTests
{
    [Fact]
    public void EnsureIsConfigured_WhenDisabled_DoesNotThrow()
    {
        // Arrange

        var options = new UserSeedOptions
        {
            Enabled = false
        };

        // Act

        var exception = Record.Exception(
            options.EnsureIsConfigured);

        // Assert

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void EnsureIsConfigured_WhenRoleIsMissing_Throws(string? role)
    {
        // Arrange

        var options = CreateOptions(
            role: role);

        // Act

        var exception = Assert.Throws<InvalidOperationException>(
            options.EnsureIsConfigured);

        // Assert

        Assert.Equal(
            "User seed role is not configured.",
            exception.Message);
    }

    [Fact]
    public void EnsureIsConfigured_WhenRoleIsUnknown_Throws()
    {
        // Arrange

        var options = CreateOptions(
            role: "Unknown");

        // Act

        var exception = Assert.Throws<InvalidOperationException>(
            options.EnsureIsConfigured);

        // Assert

        Assert.Contains(
            ApplicationRoles.Administrator,
            exception.Message,
            StringComparison.Ordinal);

        Assert.Contains(
            ApplicationRoles.User,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureIsConfigured_WhenEmailIsMissing_Throws()
    {
        // Arrange

        var options = CreateOptions(
            email: null);

        // Act

        var exception = Assert.Throws<InvalidOperationException>(
            options.EnsureIsConfigured);

        // Assert

        Assert.Equal(
            "User seed email is not configured.",
            exception.Message);
    }

    [Fact]
    public void EnsureIsConfigured_WhenPasswordIsMissing_Throws()
    {
        // Arrange

        var options = CreateOptions(
            password: null);

        // Act

        var exception = Assert.Throws<InvalidOperationException>(
            options.EnsureIsConfigured);

        // Assert

        Assert.Equal(
            "User seed password is not configured.",
            exception.Message);
    }

    [Fact]
    public void EnsureIsConfigured_WhenLastNameIsMissing_Throws()
    {
        // Arrange

        var options = CreateOptions(
            lastName: null);

        // Act

        var exception = Assert.Throws<InvalidOperationException>(
            options.EnsureIsConfigured);

        // Assert

        Assert.Equal(
            "User seed last name is not configured.",
            exception.Message);
    }

    [Fact]
    public void EnsureIsConfigured_WhenFirstNameIsMissing_Throws()
    {
        // Arrange

        var options = CreateOptions(
            firstName: null);

        // Act

        var exception = Assert.Throws<InvalidOperationException>(
            options.EnsureIsConfigured);

        // Assert

        Assert.Equal(
            "User seed first name is not configured.",
            exception.Message);
    }

    [Fact]
    public void EnsureIsConfigured_WhenConfigurationIsValid_DoesNotThrow()
    {
        // Arrange

        var options = CreateOptions();

        // Act

        var exception = Record.Exception(
            options.EnsureIsConfigured);

        // Assert

        Assert.Null(exception);
    }


    [Theory]
    [InlineData("Email")]
    [InlineData("Password")]
    [InlineData("LastName")]
    [InlineData("FirstName")]
    public void EnsureIsConfigured_WhenRequiredTextValueIsWhitespace_Throws(string field)
    {
        var options = new UserSeedOptions
        {
            Enabled = true,
            Role = ApplicationRoles.Administrator,
            Email = field == "Email" ? "   " : "admin@example.test",
            Password = field == "Password" ? "   " : "ValidPassword!123",
            LastName = field == "LastName" ? "   " : "Admin",
            FirstName = field == "FirstName" ? "   " : "Bootstrap"
        };

        Assert.Throws<InvalidOperationException>(options.EnsureIsConfigured);
    }

    private static UserSeedOptions CreateOptions(
        string? role = ApplicationRoles.Administrator,
        string? email = "admin@example.test",
        string? password = "ValidPassword!123",
        string? lastName = "Admin",
        string? firstName = "Bootstrap")
    {
        return new UserSeedOptions
        {
            Enabled = true,
            Role = role,
            Email = email,
            Password = password,
            LastName = lastName,
            FirstName = firstName
        };
    }
}
