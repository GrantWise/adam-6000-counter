# InfluxDB Counter Data - Developer Access Guide

**Version:** 2.0  
**Updated:** January 2025  
**Target Platform:** .NET 9+ Applications  

---

## Table of Contents

1. [Overview](#1-overview)
2. [Data Schema and Structure](#2-data-schema-and-structure)
3. [Direct InfluxDB Access](#3-direct-influxdb-access)
4. [C# Client Integration](#4-c-client-integration)
5. [Query Patterns and Examples](#5-query-patterns-and-examples)
6. [Data Processing Patterns](#6-data-processing-patterns)
7. [Performance Optimization](#7-performance-optimization)
8. [Security and Best Practices](#8-security-and-best-practices)
9. [Troubleshooting](#9-troubleshooting)

---

## 1. Overview

### 1.1 Purpose

This guide provides comprehensive information for developers who need to access and process industrial counter data stored in InfluxDB by the Industrial Adam Logger system. The data contains high-frequency counter readings from ADAM-6050/6051 devices, optimized for time-series analysis and reporting applications.

### 1.2 Data Characteristics

**Counter Data Properties:**
- **High Frequency**: Readings captured every 1-5 seconds per channel
- **Multi-Device**: Support for multiple ADAM devices simultaneously
- **Multi-Channel**: Each device can have multiple counter channels
- **Quality Tracking**: Data quality indicators for reliability assessment
- **Rate Calculations**: Automatic rate-of-change calculations
- **Overflow Handling**: Automatic detection and handling of counter overflow

**Typical Use Cases:**
- Production monitoring and reporting
- Overall Equipment Effectiveness (OEE) calculations
- Quality analysis and trending
- Downtime analysis and reporting
- Performance benchmarking
- Predictive maintenance indicators

---

## 2. Data Schema and Structure

### 2.1 InfluxDB Database Structure

**Database Configuration:**
```
Database: adam_counters (default)
Organization: adam_org
Bucket: adam_counters
Measurement: device_readings
```

### 2.2 Data Point Structure

Each data point contains the following fields and tags:

**Tags (Indexed for Fast Queries):**
- `device_id`: Unique identifier for the ADAM device
- `channel`: Channel number (0-based)

**Fields (Actual Data Values):**
- `raw_value` (integer): Original counter value from device
- `processed_value` (integer): Processed value after overflow handling
- `quality` (string): Data quality indicator ("Good", "Degraded", "Bad")
- `rate` (float, optional): Rate of change per second

**Timestamp:**
- UTC timestamp with millisecond precision

### 2.3 Data Quality Indicators

**Quality Values:**
- **"Good"**: Normal reading, data is reliable
- **"Degraded"**: Reading suspect but usable (rate anomalies, communication issues)
- **"Bad"**: Reading unreliable (connection failures, invalid values)

### 2.4 Example Data Points

```json
{
  "measurement": "device_readings",
  "tags": {
    "device_id": "Device001",
    "channel": "0"
  },
  "fields": {
    "raw_value": 1234567,
    "processed_value": 1234567,
    "quality": "Good",
    "rate": 12.5
  },
  "timestamp": "2025-01-15T14:30:25.123Z"
}
```

---

## 3. Direct InfluxDB Access

### 3.1 Connection Information

**Default Docker Configuration:**
- **URL**: `http://localhost:8086`
- **Username**: `admin`
- **Password**: `admin123`
- **Organization**: `adam_org`
- **Bucket**: `adam_counters`

**Production Configuration:**
- Use secure authentication tokens
- Enable HTTPS connections
- Implement proper network security

### 3.2 Flux Query Language

InfluxDB 2.x uses Flux query language for data access:

**Basic Counter Data Query:**
```flux
from(bucket: "adam_counters")
  |> range(start: -1h)
  |> filter(fn: (r) => r._measurement == "device_readings")
  |> filter(fn: (r) => r.device_id == "Device001")
  |> filter(fn: (r) => r.channel == "0")
  |> filter(fn: (r) => r._field == "processed_value")
```

**Multi-Device Query:**
```flux
from(bucket: "adam_counters")
  |> range(start: -24h)
  |> filter(fn: (r) => r._measurement == "device_readings")
  |> filter(fn: (r) => r.device_id =~ /Device.*/)
  |> filter(fn: (r) => r._field == "processed_value")
  |> group(columns: ["device_id", "channel"])
```

### 3.3 InfluxDB Web Interface

Access the InfluxDB web interface at `http://localhost:8086`:

1. Navigate to Data Explorer
2. Select bucket: `adam_counters`
3. Use query builder or write Flux queries
4. Export results as CSV or visualize directly

---

## 4. C# Client Integration

### 4.1 Package Installation

```xml
<PackageReference Include="InfluxDB.Client" Version="4.17.0" />
<PackageReference Include="InfluxDB.Client.Linq" Version="4.17.0" />
```

### 4.2 Basic Client Setup

```csharp
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;

public class CounterDataService
{
    private readonly IInfluxDBClient _client;
    private readonly string _bucket = "adam_counters";
    private readonly string _org = "adam_org";

    public CounterDataService()
    {
        _client = new InfluxDBClient(
            "http://localhost:8086",
            "your-auth-token"
        );
    }

    public async Task<List<CounterReading>> GetCounterDataAsync(
        string deviceId, 
        int channel, 
        DateTime start, 
        DateTime end)
    {
        var query = $@"
            from(bucket: ""{_bucket}"")
              |> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, stop: {end:yyyy-MM-ddTHH:mm:ssZ})
              |> filter(fn: (r) => r._measurement == ""device_readings"")
              |> filter(fn: (r) => r.device_id == ""{deviceId}"")
              |> filter(fn: (r) => r.channel == ""{channel}"")
              |> pivot(rowKey:[""_time""], columnKey: [""_field""], valueColumn: ""_value"")
        ";

        var queryApi = _client.GetQueryApi();
        var result = await queryApi.QueryAsync<CounterReading>(query, _org);
        return result.ToList();
    }
}

public class CounterReading
{
    [Column("_time")] 
    public DateTime Timestamp { get; set; }
    
    [Column("device_id")] 
    public string DeviceId { get; set; }
    
    [Column("channel")] 
    public string Channel { get; set; }
    
    [Column("raw_value")] 
    public long RawValue { get; set; }
    
    [Column("processed_value")] 
    public long ProcessedValue { get; set; }
    
    [Column("quality")] 
    public string Quality { get; set; }
    
    [Column("rate")] 
    public double? Rate { get; set; }
}
```

### 4.3 Advanced Client Configuration

```csharp
public class CounterDataServiceConfiguration
{
    public string InfluxDbUrl { get; set; } = "http://localhost:8086";
    public string Token { get; set; }
    public string Organization { get; set; } = "adam_org";
    public string Bucket { get; set; } = "adam_counters";
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxBatchSize { get; set; } = 1000;
}

public class CounterDataService : IDisposable
{
    private readonly IInfluxDBClient _client;
    private readonly CounterDataServiceConfiguration _config;
    private readonly ILogger<CounterDataService> _logger;

    public CounterDataService(
        CounterDataServiceConfiguration config,
        ILogger<CounterDataService> logger)
    {
        _config = config;
        _logger = logger;
        
        var options = new InfluxDBClientOptions(_config.InfluxDbUrl)
        {
            Token = _config.Token,
            Org = _config.Organization,
            Bucket = _config.Bucket,
            Timeout = _config.QueryTimeout
        };
        
        _client = new InfluxDBClient(options);
    }

    public void Dispose() => _client?.Dispose();
}
```

---

## 5. Query Patterns and Examples

### 5.1 Time-Based Queries

**Last Hour of Data:**
```csharp
public async Task<List<CounterReading>> GetLastHourDataAsync(string deviceId)
{
    var query = $@"
        from(bucket: ""{_bucket}"")
          |> range(start: -1h)
          |> filter(fn: (r) => r._measurement == ""device_readings"")
          |> filter(fn: (r) => r.device_id == ""{deviceId}"")
          |> filter(fn: (r) => r.quality == ""Good"")
          |> pivot(rowKey:[""_time""], columnKey: [""_field""], valueColumn: ""_value"")
          |> sort(columns: [""_time""])
    ";
    
    return await ExecuteQueryAsync<CounterReading>(query);
}
```

**Production Shift Data:**
```csharp
public async Task<List<CounterReading>> GetShiftDataAsync(
    string deviceId, 
    DateTime shiftStart, 
    DateTime shiftEnd)
{
    var query = $@"
        from(bucket: ""{_bucket}"")
          |> range(start: {shiftStart:yyyy-MM-ddTHH:mm:ssZ}, 
                   stop: {shiftEnd:yyyy-MM-ddTHH:mm:ssZ})
          |> filter(fn: (r) => r._measurement == ""device_readings"")
          |> filter(fn: (r) => r.device_id == ""{deviceId}"")
          |> pivot(rowKey:[""_time""], columnKey: [""_field""], valueColumn: ""_value"")
          |> sort(columns: [""_time""])
    ";
    
    return await ExecuteQueryAsync<CounterReading>(query);
}
```

### 5.2 Aggregation Queries

**Hourly Production Totals:**
```csharp
public async Task<List<HourlyProduction>> GetHourlyProductionAsync(
    string deviceId, 
    DateTime start, 
    DateTime end)
{
    var query = $@"
        from(bucket: ""{_bucket}"")
          |> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, 
                   stop: {end:yyyy-MM-ddTHH:mm:ssZ})
          |> filter(fn: (r) => r._measurement == ""device_readings"")
          |> filter(fn: (r) => r.device_id == ""{deviceId}"")
          |> filter(fn: (r) => r._field == ""processed_value"")
          |> aggregateWindow(every: 1h, fn: mean)
          |> difference()
          |> map(fn: (r) => ({{ r with _value: if r._value < 0 then 0 else r._value }}))
    ";
    
    return await ExecuteQueryAsync<HourlyProduction>(query);
}

public class HourlyProduction
{
    [Column("_time")] 
    public DateTime Hour { get; set; }
    
    [Column("device_id")] 
    public string DeviceId { get; set; }
    
    [Column("_value")] 
    public double Production { get; set; }
}
```

**Peak Production Rates:**
```csharp
public async Task<List<ProductionRate>> GetPeakRatesAsync(
    string deviceId, 
    DateTime start, 
    DateTime end)
{
    var query = $@"
        from(bucket: ""{_bucket}"")
          |> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, 
                   stop: {end:yyyy-MM-ddTHH:mm:ssZ})
          |> filter(fn: (r) => r._measurement == ""device_readings"")
          |> filter(fn: (r) => r.device_id == ""{deviceId}"")
          |> filter(fn: (r) => r._field == ""rate"")
          |> filter(fn: (r) => r._value > 0)
          |> aggregateWindow(every: 10m, fn: max)
          |> sort(columns: [""_value""], desc: true)
          |> limit(n: 10)
    ";
    
    return await ExecuteQueryAsync<ProductionRate>(query);
}
```

### 5.3 Multi-Device Queries

**All Device Status:**
```csharp
public async Task<List<DeviceStatus>> GetAllDeviceStatusAsync()
{
    var query = $@"
        from(bucket: ""{_bucket}"")
          |> range(start: -5m)
          |> filter(fn: (r) => r._measurement == ""device_readings"")
          |> filter(fn: (r) => r._field == ""quality"")
          |> group(columns: [""device_id""])
          |> last()
          |> group()
    ";
    
    return await ExecuteQueryAsync<DeviceStatus>(query);
}

public class DeviceStatus
{
    [Column("device_id")] 
    public string DeviceId { get; set; }
    
    [Column("_time")] 
    public DateTime LastUpdate { get; set; }
    
    [Column("_value")] 
    public string Quality { get; set; }
}
```

### 5.4 Data Quality Analysis

**Quality Distribution:**
```csharp
public async Task<List<QualityMetrics>> GetQualityMetricsAsync(
    DateTime start, 
    DateTime end)
{
    var query = $@"
        from(bucket: ""{_bucket}"")
          |> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, 
                   stop: {end:yyyy-MM-ddTHH:mm:ssZ})
          |> filter(fn: (r) => r._measurement == ""device_readings"")
          |> filter(fn: (r) => r._field == ""quality"")
          |> group(columns: [""device_id"", ""_value""])
          |> count()
          |> rename(columns: {{_value: ""quality"", _field: ""count""}})
          |> group()
    ";
    
    return await ExecuteQueryAsync<QualityMetrics>(query);
}

public class QualityMetrics
{
    [Column("device_id")] 
    public string DeviceId { get; set; }
    
    [Column("quality")] 
    public string Quality { get; set; }
    
    [Column("count")] 
    public long Count { get; set; }
}
```

---

## 6. Data Processing Patterns

### 6.1 Counter Difference Calculations

Since ADAM devices provide cumulative counter values, you often need to calculate differences for production rates:

```csharp
public class ProductionCalculator
{
    public async Task<List<ProductionPeriod>> CalculateProductionAsync(
        string deviceId,
        int channel,
        DateTime start,
        DateTime end,
        TimeSpan aggregationWindow)
    {
        var readings = await GetCounterDataAsync(deviceId, channel, start, end);
        
        return readings
            .Where(r => r.Quality == "Good")
            .GroupBy(r => GetTimeWindow(r.Timestamp, aggregationWindow))
            .Select(g => new ProductionPeriod
            {
                WindowStart = g.Key,
                WindowEnd = g.Key.Add(aggregationWindow),
                DeviceId = deviceId,
                Channel = channel,
                StartValue = g.OrderBy(r => r.Timestamp).First().ProcessedValue,
                EndValue = g.OrderBy(r => r.Timestamp).Last().ProcessedValue,
                TotalProduction = g.OrderBy(r => r.Timestamp).Last().ProcessedValue - 
                                g.OrderBy(r => r.Timestamp).First().ProcessedValue,
                AverageRate = g.Where(r => r.Rate.HasValue).Select(r => r.Rate.Value).DefaultIfEmpty(0).Average(),
                DataQuality = CalculateQualityScore(g.ToList())
            })
            .ToList();
    }

    private DateTime GetTimeWindow(DateTime timestamp, TimeSpan window)
    {
        var ticks = timestamp.Ticks / window.Ticks;
        return new DateTime(ticks * window.Ticks);
    }

    private double CalculateQualityScore(List<CounterReading> readings)
    {
        if (!readings.Any()) return 0.0;
        
        var goodCount = readings.Count(r => r.Quality == "Good");
        return (double)goodCount / readings.Count * 100.0;
    }
}

public class ProductionPeriod
{
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public string DeviceId { get; set; }
    public int Channel { get; set; }
    public long StartValue { get; set; }
    public long EndValue { get; set; }
    public long TotalProduction { get; set; }
    public double AverageRate { get; set; }
    public double DataQuality { get; set; }
}
```

### 6.2 Downtime Detection

```csharp
public class DowntimeAnalyzer
{
    private readonly TimeSpan _expectedReadingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _downtimeThreshold = TimeSpan.FromMinutes(2);

    public async Task<List<DowntimePeriod>> DetectDowntimeAsync(
        string deviceId,
        DateTime start,
        DateTime end)
    {
        var readings = await GetCounterDataAsync(deviceId, 0, start, end);
        var downtimePeriods = new List<DowntimePeriod>();
        
        for (int i = 1; i < readings.Count; i++)
        {
            var gap = readings[i].Timestamp - readings[i-1].Timestamp;
            
            if (gap > _downtimeThreshold)
            {
                downtimePeriods.Add(new DowntimePeriod
                {
                    DeviceId = deviceId,
                    StartTime = readings[i-1].Timestamp,
                    EndTime = readings[i].Timestamp,
                    Duration = gap,
                    Type = ClassifyDowntime(gap, readings[i-1], readings[i])
                });
            }
        }
        
        return downtimePeriods;
    }

    private DowntimeType ClassifyDowntime(TimeSpan duration, CounterReading before, CounterReading after)
    {
        if (duration > TimeSpan.FromHours(1))
            return DowntimeType.PlannedMaintenance;
        else if (after.Quality != "Good")
            return DowntimeType.CommunicationFailure;
        else if (before.Rate > 0 && (after.Rate ?? 0) == 0)
            return DowntimeType.ProductionStop;
        else
            return DowntimeType.Unknown;
    }
}

public class DowntimePeriod
{
    public string DeviceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public DowntimeType Type { get; set; }
}

public enum DowntimeType
{
    Unknown,
    PlannedMaintenance,
    CommunicationFailure,
    ProductionStop,
    QualityIssue
}
```

### 6.3 Rate Anomaly Detection

```csharp
public class AnomalyDetector
{
    public async Task<List<RateAnomaly>> DetectRateAnomaliesAsync(
        string deviceId,
        DateTime start,
        DateTime end,
        double thresholdMultiplier = 3.0)
    {
        var readings = await GetCounterDataAsync(deviceId, 0, start, end);
        var rates = readings
            .Where(r => r.Rate.HasValue && r.Quality == "Good")
            .Select(r => r.Rate.Value)
            .ToList();
        
        if (rates.Count < 10) return new List<RateAnomaly>();
        
        var mean = rates.Average();
        var stdDev = Math.Sqrt(rates.Select(r => Math.Pow(r - mean, 2)).Average());
        var upperThreshold = mean + (stdDev * thresholdMultiplier);
        var lowerThreshold = mean - (stdDev * thresholdMultiplier);
        
        return readings
            .Where(r => r.Rate.HasValue && 
                       (r.Rate.Value > upperThreshold || r.Rate.Value < lowerThreshold))
            .Select(r => new RateAnomaly
            {
                DeviceId = deviceId,
                Timestamp = r.Timestamp,
                ActualRate = r.Rate.Value,
                ExpectedRange = new { Min = lowerThreshold, Max = upperThreshold },
                Severity = CalculateSeverity(r.Rate.Value, mean, stdDev)
            })
            .ToList();
    }

    private AnomalySeverity CalculateSeverity(double rate, double mean, double stdDev)
    {
        var deviations = Math.Abs(rate - mean) / stdDev;
        return deviations switch
        {
            > 5 => AnomalySeverity.Critical,
            > 3 => AnomalySeverity.High,
            > 2 => AnomalySeverity.Medium,
            _ => AnomalySeverity.Low
        };
    }
}

public class RateAnomaly
{
    public string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public double ActualRate { get; set; }
    public object ExpectedRange { get; set; }
    public AnomalySeverity Severity { get; set; }
}

public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

---

## 7. Performance Optimization

### 7.1 Query Optimization

**Use Specific Time Ranges:**
```csharp
// Good: Specific time range
from(bucket: "adam_counters")
  |> range(start: -1h, stop: now())

// Avoid: Open-ended queries
from(bucket: "adam_counters")
  |> range(start: -30d)
```

**Leverage Tags for Filtering:**
```csharp
// Good: Filter by indexed tags first
from(bucket: "adam_counters")
  |> range(start: -1h)
  |> filter(fn: (r) => r.device_id == "Device001")
  |> filter(fn: (r) => r.channel == "0")

// Avoid: Filtering on field values first
from(bucket: "adam_counters")
  |> range(start: -1h)
  |> filter(fn: (r) => r._value > 1000)
```

**Use Appropriate Sampling:**
```csharp
public async Task<List<CounterReading>> GetSampledDataAsync(
    string deviceId,
    DateTime start,
    DateTime end,
    TimeSpan sampleInterval)
{
    var query = $@"
        from(bucket: ""{_bucket}"")
          |> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, 
                   stop: {end:yyyy-MM-ddTHH:mm:ssZ})
          |> filter(fn: (r) => r._measurement == ""device_readings"")
          |> filter(fn: (r) => r.device_id == ""{deviceId}"")
          |> aggregateWindow(every: {sampleInterval.TotalSeconds}s, fn: last)
          |> pivot(rowKey:[""_time""], columnKey: [""_field""], valueColumn: ""_value"")
    ";
    
    return await ExecuteQueryAsync<CounterReading>(query);
}
```

### 7.2 Batching and Pagination

```csharp
public async IAsyncEnumerable<List<CounterReading>> GetDataBatchesAsync(
    string deviceId,
    DateTime start,
    DateTime end,
    int batchSize = 1000)
{
    var currentStart = start;
    
    while (currentStart < end)
    {
        var batchEnd = currentStart.AddHours(1);
        if (batchEnd > end) batchEnd = end;
        
        var batch = await GetCounterDataAsync(deviceId, 0, currentStart, batchEnd);
        
        if (batch.Any())
            yield return batch;
        
        currentStart = batchEnd;
    }
}
```

### 7.3 Caching Strategies

```csharp
public class CachedCounterDataService
{
    private readonly IMemoryCache _cache;
    private readonly CounterDataService _dataService;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public async Task<List<CounterReading>> GetRecentDataAsync(string deviceId)
    {
        var cacheKey = $"recent_data_{deviceId}";
        
        if (_cache.TryGetValue(cacheKey, out List<CounterReading> cachedData))
        {
            return cachedData;
        }
        
        var data = await _dataService.GetLastHourDataAsync(deviceId);
        
        _cache.Set(cacheKey, data, _cacheExpiry);
        return data;
    }
}
```

---

## 8. Security and Best Practices

### 8.1 Authentication and Authorization

**Token-Based Authentication:**
```csharp
public class SecureInfluxDbService
{
    private readonly IInfluxDBClient _client;
    
    public SecureInfluxDbService(IConfiguration config)
    {
        var token = config["InfluxDb:Token"];
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("InfluxDB token is required");
            
        _client = new InfluxDBClient(config["InfluxDb:Url"], token);
    }
}
```

**Configuration Security:**
```json
{
  "InfluxDb": {
    "Url": "https://your-influxdb-instance.com",
    "Token": "use-azure-key-vault-or-environment-variables",
    "Organization": "your-org",
    "Bucket": "adam_counters"
  }
}
```

### 8.2 Error Handling

```csharp
public async Task<List<CounterReading>> GetDataWithRetryAsync(
    string deviceId,
    DateTime start,
    DateTime end,
    int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await GetCounterDataAsync(deviceId, 0, start, end);
        }
        catch (InfluxException ex) when (ex.Status == 429) // Rate limited
        {
            if (attempt == maxRetries) throw;
            
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            await Task.Delay(delay);
        }
        catch (InfluxException ex) when (ex.Status >= 500) // Server error
        {
            if (attempt == maxRetries) throw;
            
            _logger.LogWarning(ex, "InfluxDB server error on attempt {Attempt}", attempt);
            await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
        }
    }
    
    return new List<CounterReading>();
}
```

### 8.3 Resource Management

```csharp
public class CounterDataService : IAsyncDisposable
{
    private readonly IInfluxDBClient _client;
    
    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            _client.Dispose();
        }
    }
}
```

---

## 9. Troubleshooting

### 9.1 Common Issues

**Connection Problems:**
```csharp
public async Task<bool> TestConnectionAsync()
{
    try
    {
        var health = await _client.HealthAsync();
        return health.Status == HealthCheck.StatusEnum.Pass;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to connect to InfluxDB");
        return false;
    }
}
```

**Query Timeouts:**
```csharp
// Increase timeout for large queries
var options = new InfluxDBClientOptions(url)
{
    Token = token,
    Timeout = TimeSpan.FromMinutes(10)
};
```

**Memory Issues with Large Datasets:**
```csharp
public async IAsyncEnumerable<CounterReading> StreamLargeDatasetAsync(
    string deviceId,
    DateTime start,
    DateTime end)
{
    var query = BuildQuery(deviceId, start, end);
    
    await foreach (var record in _client.GetQueryApi().QueryAsyncEnumerable<CounterReading>(query, _org))
    {
        yield return record;
    }
}
```

### 9.2 Performance Monitoring

```csharp
public class PerformanceMetrics
{
    public TimeSpan QueryDuration { get; set; }
    public int RecordCount { get; set; }
    public long MemoryUsage { get; set; }
}

public async Task<(List<CounterReading> Data, PerformanceMetrics Metrics)> GetDataWithMetricsAsync(
    string deviceId,
    DateTime start,
    DateTime end)
{
    var stopwatch = Stopwatch.StartNew();
    var initialMemory = GC.GetTotalMemory(false);
    
    var data = await GetCounterDataAsync(deviceId, 0, start, end);
    
    stopwatch.Stop();
    var finalMemory = GC.GetTotalMemory(false);
    
    var metrics = new PerformanceMetrics
    {
        QueryDuration = stopwatch.Elapsed,
        RecordCount = data.Count,
        MemoryUsage = finalMemory - initialMemory
    };
    
    return (data, metrics);
}
```

### 9.3 Debugging Queries

```csharp
public async Task<string> ExplainQueryAsync(string fluxQuery)
{
    try
    {
        var queryApi = _client.GetQueryApi();
        var result = await queryApi.QueryRawAsync(fluxQuery, _org);
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Query failed: {Query}", fluxQuery);
        throw;
    }
}
```

---

## Next Steps

This guide provides the foundation for accessing and processing industrial counter data from InfluxDB. For specific application patterns such as OEE calculations, predictive maintenance, or custom reporting solutions, you can build upon these examples to create specialized data processing pipelines.

**Additional Resources:**
- [InfluxDB Flux Documentation](https://docs.influxdata.com/flux/)
- [InfluxDB .NET Client Documentation](https://github.com/influxdata/influxdb-client-csharp)
- [Industrial Adam Logger API Documentation](../src/Industrial.Adam.Logger.WebApi/README.md)

For questions or support, please refer to the main project documentation or create an issue in the repository.