using FluentAssertions;
using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Xunit;

namespace Industrial.Adam.EquipmentScheduling.Tests.Domain;

public sealed class ResourceTests
{
    [Fact]
    public void Resource_Creation_Should_Set_Properties_Correctly()
    {
        // Arrange
        const string name = "Test Resource";
        const string code = "TEST-001";
        const ResourceType type = ResourceType.WorkUnit;
        const bool requiresScheduling = true;
        const string description = "Test resource description";

        // Act
        var resource = new Resource(name, code, type, requiresScheduling, description);

        // Assert
        resource.Name.Should().Be(name);
        resource.Code.Should().Be("TEST-001"); // Should be upper case
        resource.Type.Should().Be(type);
        resource.RequiresScheduling.Should().Be(requiresScheduling);
        resource.Description.Should().Be(description);
        resource.IsActive.Should().BeTrue();
        resource.ParentId.Should().BeNull();
        resource.HierarchyPath.Should().BeNull();
        resource.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Resource_Creation_With_Invalid_Name_Should_Throw_Exception()
    {
        // Arrange
        const string invalidName = "";
        const string code = "TEST-001";
        const ResourceType type = ResourceType.WorkUnit;

        // Act
        Action act = () => new Resource(invalidName, code, type);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Resource name cannot be empty*");
    }

    [Fact]
    public void Resource_Creation_With_Invalid_Code_Should_Throw_Exception()
    {
        // Arrange
        const string name = "Test Resource";
        const string invalidCode = "";
        const ResourceType type = ResourceType.WorkUnit;

        // Act
        Action act = () => new Resource(name, invalidCode, type);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Resource code cannot be empty*");
    }

    [Fact]
    public void Resource_SetParent_Should_Update_Hierarchy_Path()
    {
        // Arrange
        var resource = new Resource("Child Resource", "CHILD-001", ResourceType.WorkUnit);
        const long parentId = 123L;
        const string parentHierarchyPath = "/1/123/";

        // Act
        resource.SetParent(parentId, parentHierarchyPath);

        // Assert
        resource.ParentId.Should().Be(parentId);
        resource.HierarchyPath.Should().Be("/1/123/0/");
        resource.DomainEvents.Should().HaveCount(2); // Creation + hierarchy change
    }

    [Fact]
    public void Resource_SetParent_With_Same_Id_Should_Throw_Exception()
    {
        // Arrange
        var resource = new Resource("Test Resource", "TEST-001", ResourceType.WorkUnit);
        // Since we can't get the actual ID (it's generated), we'll use reflection or test with a known value
        var resourceId = resource.Id;

        // Act
        Action act = () => resource.SetParent(resourceId, "/1/");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be its own parent*");
    }

    [Fact]
    public void Resource_UpdateResource_Should_Modify_Properties()
    {
        // Arrange
        var resource = new Resource("Original Name", "TEST-001", ResourceType.WorkUnit);
        const string newName = "Updated Name";
        const bool newRequiresScheduling = true;
        const string newDescription = "Updated description";

        // Act
        resource.UpdateResource(newName, newRequiresScheduling, newDescription);

        // Assert
        resource.Name.Should().Be(newName);
        resource.RequiresScheduling.Should().Be(newRequiresScheduling);
        resource.Description.Should().Be(newDescription);
        resource.DomainEvents.Should().HaveCount(2); // Creation + update
    }

    [Fact]
    public void Resource_Deactivate_Should_Set_IsActive_False()
    {
        // Arrange
        var resource = new Resource("Test Resource", "TEST-001", ResourceType.WorkUnit);
        resource.IsActive.Should().BeTrue();

        // Act
        resource.Deactivate();

        // Assert
        resource.IsActive.Should().BeFalse();
        resource.DomainEvents.Should().HaveCount(2); // Creation + deactivation
    }

    [Fact]
    public void Resource_Activate_Should_Set_IsActive_True()
    {
        // Arrange
        var resource = new Resource("Test Resource", "TEST-001", ResourceType.WorkUnit);
        resource.Deactivate();
        resource.IsActive.Should().BeFalse();

        // Act
        resource.Activate();

        // Assert
        resource.IsActive.Should().BeTrue();
        resource.DomainEvents.Should().HaveCount(3); // Creation + deactivation + activation
    }

    [Fact]
    public void Resource_IsAncestorOf_Should_Return_True_For_Descendant()
    {
        // Arrange
        var parent = new Resource("Parent", "PARENT-001", ResourceType.Area);
        var child = new Resource("Child", "CHILD-001", ResourceType.WorkCenter);

        // Set up hierarchy
        parent.SetParent(1L, "/1/");
        child.SetParent(parent.Id, parent.HierarchyPath);

        // Act
        var isAncestor = parent.IsAncestorOf(child);

        // Assert
        isAncestor.Should().BeTrue();
    }

    [Fact]
    public void Resource_IsAncestorOf_Should_Return_False_For_Non_Descendant()
    {
        // Arrange
        var resource1 = new Resource("Resource 1", "RES-001", ResourceType.Area);
        var resource2 = new Resource("Resource 2", "RES-002", ResourceType.Area);

        resource1.SetParent(1L, "/1/");
        resource2.SetParent(2L, "/2/");

        // Act
        var isAncestor = resource1.IsAncestorOf(resource2);

        // Assert
        isAncestor.Should().BeFalse();
    }

    [Fact]
    public void Resource_RemoveParent_Should_Clear_Parent_And_Set_Root_Path()
    {
        // Arrange
        var resource = new Resource("Test Resource", "TEST-001", ResourceType.WorkUnit);
        resource.SetParent(123L, "/1/123/");
        resource.ParentId.Should().Be(123L);

        // Act
        resource.RemoveParent();

        // Assert
        resource.ParentId.Should().BeNull();
        resource.HierarchyPath.Should().Be($"/{resource.Id}/");
        resource.DomainEvents.Should().HaveCount(3); // Creation + set parent + remove parent
    }
}
