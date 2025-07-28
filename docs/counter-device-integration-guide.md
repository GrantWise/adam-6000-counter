# Counter Device Integration Guide

This guide explains how to integrate new counter device types into the Industrial Counter Data Acquisition Platform. The platform is designed with extensibility in mind, following established architectural patterns that make it straightforward to add support for additional counter devices.

## Architecture Overview

The platform uses a plugin-based architecture that separates device communication from data processing, allowing new devices to be integrated without modifying core platform logic.

### Core Components

```
┌─────────────────────────────────────┐
│     Industrial Counter Platform     │
├─────────────────────────────────────┤
│  IAdamLoggerService (Orchestration) │
├─────────────────────────────────────┤
│  IDataProcessor (Business Logic)    │
├─────────────────────────────────────┤
│  IDeviceManager (Communication)     │  ← Extensible
├─────────────────────────────────────┤
│  IDataValidator (Validation)        │
│  IDataTransformer (Processing)      │
│  IRetryPolicyService (Reliability)  │
├─────────────────────────────────────┤
│  Device-Specific Implementations    │  ← Your Code Here
└─────────────────────────────────────┘
```

## Integration Patterns

### 1. Device Manager Implementation

Create a device manager for your counter device by implementing `IDeviceManager<T>`:

```csharp
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Utilities;

public class MyCounterDeviceManager : IDeviceManager<MyCounterReading>
{
    private readonly ILogger<MyCounterDeviceManager> _logger;
    private readonly IRetryPolicyService _retryPolicy;
    private readonly MyCounterDeviceConfig _config;

    public MyCounterDeviceManager(
        IOptions<MyCounterDeviceConfig> config,
        ILogger<MyCounterDeviceManager> logger,
        IRetryPolicyService retryPolicy)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
    }

    public async Task<OperationResult<IReadOnlyList<MyCounterReading>>> ReadDataAsync(
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            // Your device-specific communication logic here
            var readings = await ReadFromDevice(cancellationToken);
            return OperationResult<IReadOnlyList<MyCounterReading>>.Success(readings);
        });
    }

    public async Task<OperationResult<DeviceHealth>> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        // Device-specific health check implementation
        var isHealthy = await PingDevice(cancellationToken);
        return OperationResult<DeviceHealth>.Success(
            new DeviceHealth 
            { 
                Status = isHealthy ? DeviceStatus.Online : DeviceStatus.Offline,
                LastSeen = DateTimeOffset.UtcNow
            });
    }

    private async Task<IReadOnlyList<MyCounterReading>> ReadFromDevice(
        CancellationToken cancellationToken)
    {
        // Implement your device communication protocol
        // Examples: Modbus TCP, OPC UA, Ethernet/IP, Serial, HTTP REST API
        
        var readings = new List<MyCounterReading>();
        
        foreach (var channel in _config.Channels.Where(c => c.Enabled))
        {
            var rawValue = await ReadCounterValue(channel, cancellationToken);
            
            readings.Add(new MyCounterReading
            {
                DeviceId = _config.DeviceId,
                ChannelNumber = channel.ChannelNumber,
                CounterValue = rawValue,
                Timestamp = DateTimeOffset.UtcNow,
                Quality = DataQuality.Good
            });
        }
        
        return readings.AsReadOnly();
    }
}
```

### 2. Data Model Definition

Define your counter reading model:

```csharp
using Industrial.Adam.Logger.Models;

public sealed record MyCounterReading : ICounterReading
{
    public required string DeviceId { get; init; }
    public required int ChannelNumber { get; init; }
    public required uint CounterValue { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required DataQuality Quality { get; init; }
    
    // Device-specific properties
    public string? CounterType { get; init; }
    public double? Rate { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
```

### 3. Configuration Model

Create a configuration model for your device:

```csharp
using System.ComponentModel.DataAnnotations;
using Industrial.Adam.Logger.Configuration;

public sealed class MyCounterDeviceConfig : IDeviceConfig
{
    [Required]
    public required string DeviceId { get; init; }
    
    [Required]
    public required string ConnectionString { get; init; }
    
    [Range(100, 60000)]
    public int PollIntervalMs { get; init; } = 1000;
    
    [Range(1000, 30000)]
    public int TimeoutMs { get; init; } = 5000;
    
    [Range(0, 10)]
    public int MaxRetries { get; init; } = 3;
    
    public required IReadOnlyList<MyCounterChannelConfig> Channels { get; init; }
}

public sealed class MyCounterChannelConfig : IChannelConfig
{
    [Required]
    public required int ChannelNumber { get; init; }
    
    [Required]
    public required string Name { get; init; }
    
    public string? Description { get; init; }
    
    public bool Enabled { get; init; } = true;
    
    // Device-specific channel properties
    public required string CounterAddress { get; init; }
    public string CounterType { get; init; } = "production";
    public bool OeeCalculation { get; init; } = false;
}
```

### 4. Service Registration

Register your device manager with the DI container:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyCounterDevice(
        this IServiceCollection services,
        Action<MyCounterDeviceConfig> configureOptions)
    {
        // Register configuration
        services.Configure(configureOptions);
        
        // Register device manager
        services.AddSingleton<IDeviceManager<MyCounterReading>, MyCounterDeviceManager>();
        
        // Register data processor if custom logic needed
        services.AddSingleton<IDataProcessor<MyCounterReading>, MyCounterDataProcessor>();
        
        return services;
    }

    public static IServiceCollection AddMyCounterDeviceFromConfiguration(
        this IServiceCollection services,
        string configurationSection = "MyCounterDevice")
    {
        services.AddOptions<MyCounterDeviceConfig>()
            .BindConfiguration(configurationSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        services.AddSingleton<IDeviceManager<MyCounterReading>, MyCounterDeviceManager>();
        
        return services;
    }
}
```

## Communication Protocol Examples

### Modbus TCP Implementation

```csharp
private async Task<uint> ReadModbusCounter(MyCounterChannelConfig channel, 
    CancellationToken cancellationToken)
{
    using var client = new ModbusTcpClient(_config.IpAddress, _config.Port);
    await client.ConnectAsync();
    
    var registers = await client.ReadHoldingRegistersAsync(
        _config.UnitId, 
        ushort.Parse(channel.CounterAddress), 
        2); // Assuming 32-bit counter
    
    return (uint)(registers[0] << 16 | registers[1]);
}
```

### OPC UA Implementation

```csharp
private async Task<uint> ReadOpcUaCounter(MyCounterChannelConfig channel, 
    CancellationToken cancellationToken)
{
    var session = await Session.Create(
        _config.ConnectionString,
        new SessionConfiguration(),
        cancellationToken);
    
    var nodeId = new NodeId(channel.CounterAddress);
    var result = await session.ReadValueAsync(nodeId);
    
    return Convert.ToUInt32(result.Value);
}
```

### HTTP REST API Implementation

```csharp
private async Task<uint> ReadHttpCounter(MyCounterChannelConfig channel, 
    CancellationToken cancellationToken)
{
    using var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(_config.ConnectionString);
    
    var response = await httpClient.GetStringAsync(
        $"/api/counters/{channel.CounterAddress}", 
        cancellationToken);
    
    var counterData = JsonSerializer.Deserialize<CounterResponse>(response);
    return counterData.Value;
}
```

## Data Processing Integration

### Custom Data Processor

If your device requires special data processing logic:

```csharp
public class MyCounterDataProcessor : IDataProcessor<MyCounterReading>
{
    private readonly IDataValidator _validator;
    private readonly IDataTransformer _transformer;
    private readonly ILogger<MyCounterDataProcessor> _logger;

    public async Task<OperationResult<ProcessedData>> ProcessAsync(
        IReadOnlyList<MyCounterReading> readings,
        CancellationToken cancellationToken = default)
    {
        var validatedReadings = await _validator.ValidateAsync(readings, cancellationToken);
        
        if (!validatedReadings.IsSuccess)
        {
            return OperationResult<ProcessedData>.Failure(validatedReadings.Exception);
        }

        // Apply device-specific transformations
        var transformedData = await ApplyMyCounterTransformations(
            validatedReadings.Value, 
            cancellationToken);

        return OperationResult<ProcessedData>.Success(transformedData);
    }

    private async Task<ProcessedData> ApplyMyCounterTransformations(
        IReadOnlyList<MyCounterReading> readings,
        CancellationToken cancellationToken)
    {
        // Device-specific transformations:
        // - Rate calculations
        // - Unit conversions  
        // - Data aggregation
        // - Quality assessments
        
        var processedReadings = new List<ProcessedReading>();
        
        foreach (var reading in readings)
        {
            // Apply transformations
            var processed = new ProcessedReading
            {
                OriginalReading = reading,
                ProcessedValue = ApplyScalingFactor(reading.CounterValue),
                CalculatedRate = await CalculateRate(reading),
                QualityScore = AssessDataQuality(reading)
            };
            
            processedReadings.Add(processed);
        }
        
        return new ProcessedData { Readings = processedReadings };
    }
}
```

## Health Check Integration

Integrate your device with the health monitoring system:

```csharp
public class MyCounterHealthCheck : IComponentHealthCheck
{
    private readonly IDeviceManager<MyCounterReading> _deviceManager;
    
    public async Task<ComponentHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthResult = await _deviceManager.CheckHealthAsync(cancellationToken);
        
        if (!healthResult.IsSuccess)
        {
            return ComponentHealth.Critical(
                "MyCounterDevice",
                TimeSpan.FromMilliseconds(100),
                $"Device health check failed: {healthResult.ErrorMessage}");
        }

        var health = healthResult.Value;
        
        return health.Status switch
        {
            DeviceStatus.Online => ComponentHealth.Healthy(
                "MyCounterDevice", 
                TimeSpan.FromMilliseconds(100),
                "Device is online and responding"),
            DeviceStatus.Degraded => ComponentHealth.Degraded(
                "MyCounterDevice",
                TimeSpan.FromMilliseconds(100),
                75,
                "Device is responding but with degraded performance",
                new[] { "Performance issues detected" },
                new[] { "Check network connectivity and device load" }),
            _ => ComponentHealth.Unhealthy(
                "MyCounterDevice",
                TimeSpan.FromMilliseconds(100),
                "Device is offline or not responding")
        };
    }
}
```

## Testing Your Integration

### Unit Tests

```csharp
[Test]
public async Task ReadDataAsync_ValidDevice_ReturnsCounterReadings()
{
    // Arrange
    var config = CreateTestConfig();
    var logger = Mock.Of<ILogger<MyCounterDeviceManager>>();
    var retryPolicy = Mock.Of<IRetryPolicyService>();
    
    var deviceManager = new MyCounterDeviceManager(config, logger, retryPolicy);
    
    // Act
    var result = await deviceManager.ReadDataAsync();
    
    // Assert
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Value, Is.Not.Empty);
    Assert.That(result.Value.First().DeviceId, Is.EqualTo("TestDevice"));
}
```

### Integration Tests

```csharp
[Test]
public async Task EndToEnd_DeviceToInfluxDb_DataFlowsCorrectly()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddMyCounterDevice(config => /* test config */);
    services.AddAdamLoggerHealthMonitoring();
    
    var serviceProvider = services.BuildServiceProvider();
    var deviceManager = serviceProvider.GetService<IDeviceManager<MyCounterReading>>();
    
    // Act
    var readings = await deviceManager.ReadDataAsync();
    
    // Assert
    Assert.That(readings.IsSuccess, Is.True);
    // Verify data reaches InfluxDB
}
```

## Deployment Considerations

### Docker Integration

Create a Dockerfile for your device integration:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY bin/Release/net8.0/ .

# Device-specific dependencies
# Example: Install device drivers, certificates, etc.

ENTRYPOINT ["dotnet", "MyCounter.Industrial.Logger.dll"]
```

### Configuration Management

Use environment variables for device-specific settings:

```json
{
  "MyCounterDevice": {
    "DeviceId": "${MY_COUNTER_DEVICE_ID:DefaultDevice}",
    "ConnectionString": "${MY_COUNTER_CONNECTION:localhost:502}",
    "PollIntervalMs": "${MY_COUNTER_POLL_INTERVAL:1000}",
    "Channels": [
      {
        "ChannelNumber": 0,
        "Name": "ProductionCounter",
        "CounterAddress": "${MY_COUNTER_ADDRESS_0:40001}",
        "Enabled": true
      }
    ]
  }
}
```

## Best Practices

### Error Handling
- Use the `OperationResult<T>` pattern consistently
- Integrate with `IIndustrialErrorService` for standardized error messages
- Implement proper retry logic using `IRetryPolicyService`

### Performance
- Implement connection pooling for network devices
- Use async/await patterns throughout
- Consider batching multiple counter reads for efficiency

### Reliability
- Implement health checks for your device
- Handle device disconnections gracefully
- Provide meaningful error messages and troubleshooting guidance

### Security
- Never hardcode credentials or connection strings
- Use secure communication protocols when available
- Implement proper authentication and authorization

### Documentation
- Document device-specific configuration options
- Provide integration examples and troubleshooting guides
- Include performance characteristics and limitations

## Example Integrations

The platform architecture supports various counter device types:

- **ADAM-6051**: Reference implementation (included)
- **Allen-Bradley CompactLogix**: Ethernet/IP protocol
- **Siemens S7**: S7 communication protocol  
- **Schneider Electric**: Modbus TCP/RTU
- **Omron**: FINS protocol
- **Mitsubishi**: MC protocol
- **Custom REST APIs**: HTTP-based counter services
- **OPC UA Servers**: Standard OPC UA counter nodes

For each device type, follow the patterns outlined in this guide to create a clean, maintainable integration that leverages the platform's industrial-grade infrastructure.