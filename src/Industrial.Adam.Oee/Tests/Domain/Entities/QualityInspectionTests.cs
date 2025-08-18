using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.Entities;

public class QualityInspectionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateQualityInspection()
    {
        // Arrange
        var inspectionId = "QI-001";
        var inspectionType = QualityInspectionType.Final;
        var inspectionLevel = QualityInspectionLevel.Product;
        var contextReference = CanonicalReference.ToProductionDeclaration("PD-001");
        var inspectorReference = CanonicalReference.ToPerson("INSPECTOR-001");

        // Act
        var inspection = new QualityInspection(
            inspectionId,
            inspectionType,
            inspectionLevel,
            contextReference,
            inspectorReference);

        // Assert
        Assert.Equal(inspectionId, inspection.Id);
        Assert.Equal(inspectionType, inspection.InspectionType);
        Assert.Equal(inspectionLevel, inspection.InspectionLevel);
        Assert.Equal(contextReference, inspection.ContextReference);
        Assert.Equal(inspectorReference, inspection.InspectorReference);
        Assert.Equal(QualityInspectionStatus.Planned, inspection.Status);
        Assert.Equal(QualityInspectionResult.Pending, inspection.OverallResult);
        Assert.Equal(QualityDisposition.Pending, inspection.Disposition);
        Assert.True(inspection.IsEffective);
    }

    [Fact]
    public void Constructor_WithNullContextReference_ShouldThrowArgumentNullException()
    {
        // Arrange
        var inspectionId = "QI-001";
        var inspectionType = QualityInspectionType.Final;
        var inspectionLevel = QualityInspectionLevel.Product;
        CanonicalReference? contextReference = null;
        var inspectorReference = CanonicalReference.ToPerson("INSPECTOR-001");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new QualityInspection(inspectionId, inspectionType, inspectionLevel, contextReference!, inspectorReference));
    }

    [Fact]
    public void Constructor_WithNonPersonInspectorReference_ShouldThrowArgumentException()
    {
        // Arrange
        var inspectionId = "QI-001";
        var inspectionType = QualityInspectionType.Final;
        var inspectionLevel = QualityInspectionLevel.Product;
        var contextReference = CanonicalReference.ToProductionDeclaration("PD-001");
        var inspectorReference = CanonicalReference.ToResource("MILL-001"); // Not a person

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new QualityInspection(inspectionId, inspectionType, inspectionLevel, contextReference, inspectorReference));
    }

    [Fact]
    public void Start_FromPlannedStatus_ShouldChangeStatusToInProgress()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();

        // Act
        inspection.Start();

        // Assert
        Assert.Equal(QualityInspectionStatus.InProgress, inspection.Status);
    }

    [Fact]
    public void Start_FromNonPlannedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start(); // Move to InProgress

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => inspection.Start());
    }

    [Fact]
    public void AddMeasurement_WithValidParameters_ShouldAddMeasurement()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start();
        var characteristic = "Diameter";
        var value = 10.05m;
        var units = "mm";

        // Act
        inspection.AddMeasurement(characteristic, value, units);

        // Assert
        Assert.Single(inspection.Measurements);
        var measurement = inspection.Measurements.First();
        Assert.Equal(characteristic, measurement.Characteristic);
        Assert.Equal(value, measurement.Value);
        Assert.Equal(units, measurement.Units);
        Assert.Equal(QualityMeasurementResult.InSpec, measurement.Result);
    }

    [Fact]
    public void AddMeasurement_WithEmptyCharacteristic_ShouldThrowArgumentException()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            inspection.AddMeasurement(string.Empty, 10.0m, "mm"));
    }

    [Fact]
    public void AddSpecificationReference_WithValidSpecification_ShouldAddReference()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        var specificationReference = CanonicalReference.ToSpecification("SPEC-001");

        // Act
        inspection.AddSpecificationReference(specificationReference);

        // Assert
        Assert.Single(inspection.SpecificationReferences);
        Assert.Contains(specificationReference, inspection.SpecificationReferences);
    }

    [Fact]
    public void AddSpecificationReference_WithNonSpecificationReference_ShouldThrowArgumentException()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        var invalidReference = CanonicalReference.ToProduct("PRODUCT-001");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            inspection.AddSpecificationReference(invalidReference));
    }

    [Fact]
    public void Complete_FromInProgressStatus_ShouldChangeStatusToComplete()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start();
        inspection.AddMeasurement("Diameter", 10.0m, "mm", result: QualityMeasurementResult.InSpec);

        // Act
        inspection.Complete("Final inspection completed successfully");

        // Assert
        Assert.Equal(QualityInspectionStatus.Complete, inspection.Status);
        Assert.Equal("Final inspection completed successfully", inspection.Notes);
        Assert.Equal(QualityInspectionResult.Passed, inspection.OverallResult);
        Assert.Equal(QualityDisposition.Accept, inspection.Disposition);
    }

    [Fact]
    public void Complete_WithFailedMeasurements_ShouldSetFailedResultAndAlert()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start();
        inspection.AddMeasurement("Diameter", 15.0m, "mm", result: QualityMeasurementResult.OutOfSpec);

        // Act
        inspection.Complete();

        // Assert
        Assert.Equal(QualityInspectionResult.Failed, inspection.OverallResult);
        Assert.Equal(QualityDisposition.Reject, inspection.Disposition);
        Assert.True(inspection.RequiresAlert);
    }

    [Fact]
    public void Approve_FromCompleteStatus_ShouldChangeStatusToApproved()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start();
        inspection.Complete();
        var approverId = "APPROVER-001";

        // Act
        inspection.Approve(approverId);

        // Assert
        Assert.Equal(QualityInspectionStatus.Approved, inspection.Status);
    }

    [Fact]
    public void Approve_FromNonCompleteStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            inspection.Approve("APPROVER-001"));
    }

    [Fact]
    public void SetDisposition_WithValidDisposition_ShouldSetDisposition()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start();
        var disposition = QualityDisposition.ConditionalAccept;
        var reason = "Minor defect, acceptable for intended use";

        // Act
        inspection.SetDisposition(disposition, reason);

        // Assert
        Assert.Equal(disposition, inspection.Disposition);
        Assert.Contains(reason, inspection.Notes);
    }

    [Fact]
    public void SetEffectiveToDate_WithValidDate_ShouldSetDate()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        var effectiveToDate = DateTime.UtcNow.AddDays(30);

        // Act
        inspection.SetEffectiveToDate(effectiveToDate);

        // Assert
        Assert.Equal(effectiveToDate, inspection.EffectiveToDate);
    }

    [Fact]
    public void SetEffectiveToDate_WithPastDate_ShouldThrowArgumentException()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            inspection.SetEffectiveToDate(pastDate));
    }

    [Fact]
    public void ToSummary_ShouldReturnValidSummary()
    {
        // Arrange
        var inspection = CreateValidQualityInspection();
        inspection.Start();
        inspection.AddMeasurement("Diameter", 10.0m, "mm");
        inspection.AddSpecificationReference(CanonicalReference.ToSpecification("SPEC-001"));
        inspection.Complete();

        // Act
        var summary = inspection.ToSummary();

        // Assert
        Assert.Equal(inspection.Id, summary.InspectionId);
        Assert.Equal(inspection.InspectionType.ToString(), summary.InspectionType);
        Assert.Equal(inspection.InspectionLevel.ToString(), summary.InspectionLevel);
        Assert.Equal(inspection.ContextReference, summary.ContextReference);
        Assert.Equal(1, summary.SpecificationCount);
        Assert.Equal(1, summary.MeasurementCount);
        Assert.Equal(1, summary.InSpecMeasurements);
        Assert.Equal(0, summary.OutOfSpecMeasurements);
    }

    private static QualityInspection CreateValidQualityInspection()
    {
        return new QualityInspection(
            "QI-001",
            QualityInspectionType.Final,
            QualityInspectionLevel.Product,
            CanonicalReference.ToProductionDeclaration("PD-001"),
            CanonicalReference.ToPerson("INSPECTOR-001"));
    }
}
