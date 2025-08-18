using Industrial.Adam.Oee.Domain.ValueObjects;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.ValueObjects;

public class CanonicalReferenceTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateReference()
    {
        // Arrange
        var type = "product";
        var id = "WIDGET-001";

        // Act
        var reference = new CanonicalReference(type, id);

        // Assert
        Assert.Equal("product", reference.Type);
        Assert.Equal("WIDGET-001", reference.Id);
        Assert.True(reference.IsProduct);
        Assert.False(reference.IsWorkOrder);
    }

    [Fact]
    public void Constructor_WithEmptyType_ShouldThrowArgumentException()
    {
        // Arrange
        var type = string.Empty;
        var id = "WIDGET-001";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CanonicalReference(type, id));
    }

    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var type = "product";
        var id = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CanonicalReference(type, id));
    }

    [Fact]
    public void ToProduct_ShouldCreateProductReference()
    {
        // Arrange
        var productId = "PUMP-100GPM";

        // Act
        var reference = CanonicalReference.ToProduct(productId);

        // Assert
        Assert.Equal("product", reference.Type);
        Assert.Equal(productId, reference.Id);
        Assert.True(reference.IsProduct);
    }

    [Fact]
    public void ToWorkOrder_ShouldCreateWorkOrderReference()
    {
        // Arrange
        var workOrderId = "WO-12345";

        // Act
        var reference = CanonicalReference.ToWorkOrder(workOrderId);

        // Assert
        Assert.Equal("work_order", reference.Type);
        Assert.Equal(workOrderId, reference.Id);
        Assert.True(reference.IsWorkOrder);
    }

    [Fact]
    public void ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var reference = new CanonicalReference("batch", "BATCH-001");

        // Act
        var result = reference.ToString();

        // Assert
        Assert.Equal("batch:BATCH-001", result);
    }

    [Fact]
    public void Parse_WithValidString_ShouldCreateReference()
    {
        // Arrange
        var referenceString = "resource:MILL-5";

        // Act
        var reference = CanonicalReference.Parse(referenceString);

        // Assert
        Assert.Equal("resource", reference.Type);
        Assert.Equal("MILL-5", reference.Id);
        Assert.True(reference.IsResource);
    }

    [Fact]
    public void Parse_WithInvalidFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var referenceString = "invalid-format";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CanonicalReference.Parse(referenceString));
    }

    [Fact]
    public void TryParse_WithValidString_ShouldReturnTrueAndReference()
    {
        // Arrange
        var referenceString = "person:OPERATOR-123";

        // Act
        var success = CanonicalReference.TryParse(referenceString, out var reference);

        // Assert
        Assert.True(success);
        Assert.NotNull(reference);
        Assert.Equal("person", reference.Type);
        Assert.Equal("OPERATOR-123", reference.Id);
        Assert.True(reference.IsPerson);
    }

    [Fact]
    public void TryParse_WithInvalidString_ShouldReturnFalse()
    {
        // Arrange
        var referenceString = "invalid";

        // Act
        var success = CanonicalReference.TryParse(referenceString, out var reference);

        // Assert
        Assert.False(success);
        Assert.Null(reference);
    }

    [Fact]
    public void Equality_WithSameTypeAndId_ShouldBeEqual()
    {
        // Arrange
        var reference1 = new CanonicalReference("product", "WIDGET-001");
        var reference2 = new CanonicalReference("product", "WIDGET-001");

        // Act & Assert
        Assert.Equal(reference1, reference2);
        Assert.True(reference1 == reference2);
        Assert.False(reference1 != reference2);
        Assert.Equal(reference1.GetHashCode(), reference2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentTypeOrId_ShouldNotBeEqual()
    {
        // Arrange
        var reference1 = new CanonicalReference("product", "WIDGET-001");
        var reference2 = new CanonicalReference("product", "WIDGET-002");
        var reference3 = new CanonicalReference("batch", "WIDGET-001");

        // Act & Assert
        Assert.NotEqual(reference1, reference2);
        Assert.NotEqual(reference1, reference3);
        Assert.False(reference1 == reference2);
        Assert.True(reference1 != reference2);
    }

    [Fact]
    public void TypeNormalization_ShouldConvertToLowercase()
    {
        // Arrange
        var reference = new CanonicalReference("PRODUCT", "WIDGET-001");

        // Act & Assert
        Assert.Equal("product", reference.Type);
        Assert.True(reference.IsProduct);
    }
}
