using Xunit;
using Industrial.Adam.Security.Authorization;

namespace Industrial.Adam.Security.Tests;

/// <summary>
/// Security tests for authorization functionality
/// </summary>
public class AuthorizationTests
{
    [Fact]
    public void RoleConstants_ShouldHaveValidRoleNames()
    {
        // Assert - Verify role constants are defined and not empty
        Assert.NotNull(RoleConstants.SystemAdmin);
        Assert.NotEmpty(RoleConstants.SystemAdmin);
        
        Assert.NotNull(RoleConstants.Supervisor);
        Assert.NotEmpty(RoleConstants.Supervisor);
        
        Assert.NotNull(RoleConstants.Operator);
        Assert.NotEmpty(RoleConstants.Operator);
    }

    [Fact]
    public void RoleConstants_ShouldHaveUniqueValues()
    {
        // Arrange
        var roles = new[] { RoleConstants.SystemAdmin, RoleConstants.Supervisor, RoleConstants.Operator };

        // Assert - All roles should be unique
        Assert.Equal(roles.Length, roles.Distinct().Count());
    }

    [Fact]
    public void RoleConstants_ShouldFollowNamingConvention()
    {
        // Assert - Role names should follow expected format
        Assert.Matches(@"^[A-Z][a-zA-Z]*$", RoleConstants.SystemAdmin);
        Assert.Matches(@"^[A-Z][a-zA-Z]*$", RoleConstants.Supervisor);
        Assert.Matches(@"^[A-Z][a-zA-Z]*$", RoleConstants.Operator);
    }

    [Theory]
    [InlineData("SystemAdmin")]
    [InlineData("Supervisor")]
    [InlineData("Operator")]
    public void RoleConstants_ShouldContainExpectedRoles(string expectedRole)
    {
        // Arrange
        var allRoles = new[] { RoleConstants.SystemAdmin, RoleConstants.Supervisor, RoleConstants.Operator };

        // Assert
        Assert.Contains(expectedRole, allRoles);
    }

    [Fact]
    public void RoleConstants_ShouldNotContainSensitiveInformation()
    {
        // Arrange
        var roles = new[] { RoleConstants.SystemAdmin, RoleConstants.Supervisor, RoleConstants.Operator };

        // Assert - Roles should not contain passwords, tokens, or other sensitive data
        foreach (var role in roles)
        {
            Assert.DoesNotContain("password", role.ToLowerInvariant());
            Assert.DoesNotContain("token", role.ToLowerInvariant());
            Assert.DoesNotContain("secret", role.ToLowerInvariant());
            Assert.DoesNotContain("key", role.ToLowerInvariant());
        }
    }
}