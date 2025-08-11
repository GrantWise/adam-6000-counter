using BenchmarkDotNet.Attributes;
using Industrial.Adam.Logger.Core.Configuration;
using Industrial.Adam.Logger.Core.Models;
using Industrial.Adam.Logger.Core.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Industrial.Adam.Logger.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class InfluxDbStorageBenchmarks
{
    private InfluxDbStorage _storage = null!;
    private List<DeviceReading> _readings = null!;
    private DeviceReading _singleReading = null!;

    [GlobalSetup]
    public void Setup()
    {
        var settings = new InfluxDbSettings
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            MeasurementName = "counter_data",
            BatchSize = 100,
            FlushIntervalMs = 1000,
            Tags = new Dictionary<string, string> { ["location"] = "test" }
        };

        _storage = new InfluxDbStorage(NullLogger<InfluxDbStorage>.Instance, settings);

        // Create test readings
        _readings = new List<DeviceReading>();
        var timestamp = DateTimeOffset.UtcNow;
        for (int i = 0; i < 100; i++)
        {
            _readings.Add(new DeviceReading
            {
                DeviceId = $"Device{i % 10:000}",
                Channel = i % 4,
                RawValue = (long)(i * 1000),
                ProcessedValue = i * 1000,
                Rate = i * 0.5,
                Timestamp = timestamp.AddSeconds(i),
                Quality = DataQuality.Good
            });
        }

        _singleReading = _readings[0];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _storage?.Dispose();
    }

    [Benchmark]
    public async Task WriteSingleReading()
    {
        // Note: This will fail without actual InfluxDB connection
        // but we can still measure the overhead of preparing the write
        try
        {
            await _storage.WriteReadingAsync(_singleReading);
        }
        catch
        {
            // Expected to fail without InfluxDB
        }
    }

    [Benchmark]
    public async Task WriteBatch()
    {
        try
        {
            await _storage.WriteBatchAsync(_readings);
        }
        catch
        {
            // Expected to fail without InfluxDB
        }
    }

    [Benchmark]
    public void CreatePointData()
    {
        // Benchmark just the point creation logic using reflection
        var method = typeof(InfluxDbStorage).GetMethod("CreatePointData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var reading in _readings)
        {
            _ = method?.Invoke(_storage, new object[] { reading });
        }
    }
}
