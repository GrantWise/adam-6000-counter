using System.Data;
using Dapper;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Infrastructure;
using Industrial.Adam.Oee.Infrastructure.Repositories;
using Industrial.Adam.Oee.Infrastructure.Services;
using Industrial.Adam.Oee.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for WorkOrderRepository
/// Tests full CRUD operations on the OEE-specific work_orders table
/// Uses centralized container management for proper port allocation and cleanup
/// </summary>
public sealed class WorkOrderRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private IDbConnectionFactory _connectionFactory = null!;
    private IWorkOrderRepository _repository = null!;
    private IServiceProvider _serviceProvider = null!;
    private const string TestClassName = nameof(WorkOrderRepositoryTests);

    public WorkOrderRepositoryTests()
    {
        _postgresContainer = TestContainerManager.CreateContainer(TestClassName);
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<DataAccessMetrics>();

        _serviceProvider = services.BuildServiceProvider();
        _connectionFactory = TestContainerManager.CreateConnectionFactory(_postgresContainer, _serviceProvider);

        var logger = _serviceProvider.GetRequiredService<ILogger<WorkOrderRepository>>();
        _repository = new WorkOrderRepository(_connectionFactory, logger);

        await TestContainerManager.SetupOeeDatabaseAsync(_connectionFactory);
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        await TestContainerManager.DisposeContainerAsync(TestClassName);
    }

    [Fact]
    public async Task CreateAsync_WithValidWorkOrder_CreatesSuccessfully()
    {
        // Arrange
        var workOrder = CreateTestWorkOrder("WO-001", "TEST_DEVICE_001");

        // Act
        var result = await _repository.CreateAsync(workOrder);

        // Assert
        Assert.Equal(workOrder.Id, result);

        // Verify work order was created
        var retrieved = await _repository.GetByIdAsync(workOrder.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(workOrder.Id, retrieved.Id);
        Assert.Equal(workOrder.ProductDescription, retrieved.ProductDescription);
        Assert.Equal(workOrder.PlannedQuantity, retrieved.PlannedQuantity);
        Assert.Equal(workOrder.ResourceReference, retrieved.ResourceReference);
    }

    [Fact]
    public async Task CreateAsync_WithNullWorkOrder_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.CreateAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingWorkOrder_ReturnsWorkOrder()
    {
        // Arrange
        var workOrder = CreateTestWorkOrder("WO-002", "TEST_DEVICE_002");
        await _repository.CreateAsync(workOrder);

        // Act
        var result = await _repository.GetByIdAsync(workOrder.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workOrder.Id, result.Id);
        Assert.Equal(workOrder.WorkOrderDescription, result.WorkOrderDescription);
        Assert.Equal(workOrder.Status, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentWorkOrder_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("NON_EXISTENT_WO");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetByIdAsync(""));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetByIdAsync("   "));
    }

    [Fact]
    public async Task GetActiveByDeviceAsync_WithActiveWorkOrder_ReturnsWorkOrder()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_003";
        var workOrder = CreateTestWorkOrder("WO-003", deviceId);
        workOrder.Start(); // Make it active
        await _repository.CreateAsync(workOrder);

        // Act
        var result = await _repository.GetActiveByDeviceAsync(deviceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workOrder.Id, result.Id);
        Assert.Equal(WorkOrderStatus.Active, result.Status);
        Assert.Equal(deviceId, result.ResourceReference);
    }

    [Fact]
    public async Task GetActiveByDeviceAsync_WithNoActiveWorkOrder_ReturnsNull()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_004";
        var workOrder = CreateTestWorkOrder("WO-004", deviceId);
        // Leave as Pending (not active)
        await _repository.CreateAsync(workOrder);

        // Act
        var result = await _repository.GetActiveByDeviceAsync(deviceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByStatusAsync_WithMatchingStatus_ReturnsFilteredWorkOrders()
    {
        // Arrange
        var device1 = "TEST_DEVICE_005";
        var device2 = "TEST_DEVICE_006";

        var workOrder1 = CreateTestWorkOrder("WO-005", device1);
        workOrder1.Start(); // Active
        await _repository.CreateAsync(workOrder1);

        var workOrder2 = CreateTestWorkOrder("WO-006", device2);
        // Leave as Pending
        await _repository.CreateAsync(workOrder2);

        var workOrder3 = CreateTestWorkOrder("WO-007", device1);
        workOrder3.Start(); // Active
        await _repository.CreateAsync(workOrder3);

        // Act
        var activeWorkOrders = await _repository.GetByStatusAsync(WorkOrderStatus.Active);
        var pendingWorkOrders = await _repository.GetByStatusAsync(WorkOrderStatus.Pending);

        // Assert
        var activeList = activeWorkOrders.ToList();
        var pendingList = pendingWorkOrders.ToList();

        Assert.Equal(2, activeList.Count);
        Assert.All(activeList, wo => Assert.Equal(WorkOrderStatus.Active, wo.Status));

        Assert.Single(pendingList);
        Assert.All(pendingList, wo => Assert.Equal(WorkOrderStatus.Pending, wo.Status));
    }

    [Fact]
    public async Task GetByDeviceAndTimeRangeAsync_WithOverlappingTimeRange_ReturnsMatchingWorkOrders()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_007";
        var baseTime = DateTime.UtcNow;

        var workOrder1 = CreateTestWorkOrder("WO-008", deviceId,
            baseTime.AddHours(-2), baseTime.AddHours(-1));
        var workOrder2 = CreateTestWorkOrder("WO-009", deviceId,
            baseTime.AddHours(-1), baseTime.AddHours(1));
        var workOrder3 = CreateTestWorkOrder("WO-010", deviceId,
            baseTime.AddHours(1), baseTime.AddHours(2));

        await _repository.CreateAsync(workOrder1);
        await _repository.CreateAsync(workOrder2);
        await _repository.CreateAsync(workOrder3);

        // Act - Query for range that overlaps with workOrder1 and workOrder2
        var queryStart = baseTime.AddHours(-1.5);
        var queryEnd = baseTime.AddHours(0.5);
        var result = await _repository.GetByDeviceAndTimeRangeAsync(deviceId, queryStart, queryEnd);

        // Assert
        var workOrders = result.ToList();
        Assert.Equal(2, workOrders.Count);
        Assert.Contains(workOrders, wo => wo.Id == workOrder1.Id);
        Assert.Contains(workOrders, wo => wo.Id == workOrder2.Id);
        Assert.DoesNotContain(workOrders, wo => wo.Id == workOrder3.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithValidWorkOrder_UpdatesSuccessfully()
    {
        // Arrange
        var workOrder = CreateTestWorkOrder("WO-011", "TEST_DEVICE_008");
        await _repository.CreateAsync(workOrder);

        // Update work order
        workOrder.Start();
        workOrder.UpdateFromCounterData(50, 5); // Add some production

        // Act
        var result = await _repository.UpdateAsync(workOrder);

        // Assert
        Assert.True(result);

        // Verify updates
        var retrieved = await _repository.GetByIdAsync(workOrder.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(WorkOrderStatus.Active, retrieved.Status);
        Assert.Equal(50, retrieved.ActualQuantityGood);
        Assert.Equal(5, retrieved.ActualQuantityScrap);
        Assert.NotNull(retrieved.ActualStartTime);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentWorkOrder_ReturnsFalse()
    {
        // Arrange
        var workOrder = CreateTestWorkOrder("NON_EXISTENT_WO", "TEST_DEVICE_009");

        // Act
        var result = await _repository.UpdateAsync(workOrder);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingWorkOrder_DeletesSuccessfully()
    {
        // Arrange
        var workOrder = CreateTestWorkOrder("WO-012", "TEST_DEVICE_010");
        await _repository.CreateAsync(workOrder);

        // Act
        var result = await _repository.DeleteAsync(workOrder.Id);

        // Assert
        Assert.True(result);

        // Verify deletion
        var retrieved = await _repository.GetByIdAsync(workOrder.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentWorkOrder_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeleteAsync("NON_EXISTENT_WO");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingWorkOrder_ReturnsTrue()
    {
        // Arrange
        var workOrder = CreateTestWorkOrder("WO-013", "TEST_DEVICE_011");
        await _repository.CreateAsync(workOrder);

        // Act
        var result = await _repository.ExistsAsync(workOrder.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentWorkOrder_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("NON_EXISTENT_WO");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetWorkOrdersRequiringAttentionAsync_WithLowQualityWorkOrder_ReturnsWorkOrder()
    {
        // Arrange
        var workOrder = CreateTestWorkOrder("WO-014", "TEST_DEVICE_012");
        workOrder.Start();

        // Simulate low quality (high scrap rate)
        workOrder.UpdateFromCounterData(10, 90); // 10 good, 90 scrap = 10% quality

        await _repository.CreateAsync(workOrder);

        // Act
        var result = await _repository.GetWorkOrdersRequiringAttentionAsync(qualityThreshold: 95m);

        // Assert
        var workOrders = result.ToList();
        Assert.Contains(workOrders, wo => wo.Id == workOrder.Id);
    }

    [Fact]
    public async Task ConcurrentOperations_WithMultipleThreads_MaintainDataIntegrity()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_013";
        var tasks = new List<Task>();
        var workOrderIds = new List<string>();

        // Act - Create multiple work orders concurrently
        for (int i = 0; i < 10; i++)
        {
            var workOrderId = $"WO-CONCURRENT-{i:D3}";
            workOrderIds.Add(workOrderId);

            tasks.Add(Task.Run(async () =>
            {
                var workOrder = CreateTestWorkOrder(workOrderId, deviceId);
                await _repository.CreateAsync(workOrder);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify all work orders were created
        foreach (var workOrderId in workOrderIds)
        {
            var workOrder = await _repository.GetByIdAsync(workOrderId);
            Assert.NotNull(workOrder);
            Assert.Equal(workOrderId, workOrder.Id);
        }
    }


    /// <summary>
    /// Create a test work order with default values
    /// </summary>
    private static WorkOrder CreateTestWorkOrder(
        string workOrderId,
        string deviceId,
        DateTime? scheduledStart = null,
        DateTime? scheduledEnd = null)
    {
        var start = scheduledStart ?? DateTime.UtcNow;
        var end = scheduledEnd ?? start.AddHours(8);

        return new WorkOrder(
            workOrderId,
            $"Test Work Order {workOrderId}",
            "TEST_PRODUCT_001",
            "Test Product Description",
            100m, // Planned quantity
            start,
            end,
            deviceId,
            "pieces",
            0m, // Initial good quantity
            0m, // Initial scrap quantity
            null, // No actual start time yet
            null, // No actual end time yet
            WorkOrderStatus.Pending
        );
    }
}
