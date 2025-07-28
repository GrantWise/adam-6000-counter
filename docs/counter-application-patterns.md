# Counter Application Patterns

This document outlines proven patterns and best practices for implementing industrial counter applications using the Industrial Counter Data Acquisition Platform. These patterns are based on real-world manufacturing scenarios and provide guidance for common use cases.

## Overview

Industrial counter applications fall into several categories, each with specific requirements and patterns. This guide covers the most common patterns and provides implementation guidance for each.

## Application Categories

### 1. Production Line Monitoring
**Use Case**: Track production output, cycle times, and efficiency metrics  
**Typical Counters**: Production count, cycle complete, quality pass/fail

### 2. Overall Equipment Effectiveness (OEE)
**Use Case**: Calculate availability, performance, and quality metrics  
**Typical Counters**: Good parts, total parts, downtime events, target rate

### 3. Quality Management Systems
**Use Case**: Track defects, rework, and quality metrics  
**Typical Counters**: Inspect count, pass count, fail count, rework count

### 4. Inventory and Material Tracking
**Use Case**: Monitor material consumption and inventory levels  
**Typical Counters**: Material dispensed, containers filled, packages shipped

### 5. Energy and Utility Monitoring
**Use Case**: Track energy consumption and utility usage  
**Typical Counters**: kWh consumed, cubic meters gas, gallons water

---

## Pattern 1: Production Line Monitoring

### Architecture

```
Production Line → ADAM Counters → Platform → Analytics Dashboard
    ↓               ↓                ↓            ↓
  Sensors      Count Pulses    Real-time     Production KPIs
              Quality Gates    Processing     Shift Reports
```

### Implementation

```csharp
public class ProductionLineApplication
{
    private readonly IAdamLoggerService _loggerService;
    private readonly IProductionCalculator _calculator;
    private readonly IShiftManager _shiftManager;

    public async Task<ProductionMetrics> GetCurrentProductionAsync()
    {
        var readings = await _loggerService.GetLatestReadingsAsync();
        
        var productionData = readings
            .Where(r => r.CounterType == "production")
            .GroupBy(r => r.DeviceId)
            .Select(g => new LineProduction
            {
                LineId = g.Key,
                CurrentCount = g.Max(r => r.CounterValue),
                Rate = _calculator.CalculateRate(g.ToList()),
                Efficiency = _calculator.CalculateEfficiency(g.ToList())
            });

        return new ProductionMetrics
        {
            Lines = productionData.ToList(),
            TotalProduction = productionData.Sum(p => p.CurrentCount),
            AverageEfficiency = productionData.Average(p => p.Efficiency),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
```

### Configuration

```json
{
  "ProductionLine": {
    "LineId": "LINE_01",
    "TargetRate": 1000,
    "ShiftDuration": 8.0,
    "Devices": [
      {
        "DeviceId": "ADAM_LINE01_01",
        "IpAddress": "192.168.1.100",
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "ProductionCounter",
            "CounterType": "production",
            "Description": "Main production line output",
            "OeeCalculation": true
          },
          {
            "ChannelNumber": 1,
            "Name": "CycleCounter", 
            "CounterType": "cycle",
            "Description": "Machine cycle completions"
          }
        ]
      }
    ]
  }
}
```

### Key Metrics

- **Production Rate**: Parts per hour
- **Cycle Time**: Time between completions
- **Efficiency**: Actual vs. target production
- **Downtime**: Periods with no counter activity
- **Trend Analysis**: Production trends over time

---

## Pattern 2: Overall Equipment Effectiveness (OEE)

### OEE Calculation Components

```csharp
public class OeeCalculator
{
    public OeeMetrics CalculateOee(
        IReadOnlyList<CounterReading> productionData,
        OeeConfiguration config)
    {
        var availability = CalculateAvailability(productionData, config);
        var performance = CalculatePerformance(productionData, config);  
        var quality = CalculateQuality(productionData, config);

        return new OeeMetrics
        {
            Availability = availability,
            Performance = performance,
            Quality = quality,
            OverallOee = availability * performance * quality,
            CalculatedAt = DateTimeOffset.UtcNow
        };
    }

    private double CalculateAvailability(
        IReadOnlyList<CounterReading> data, 
        OeeConfiguration config)
    {
        var plannedProductionTime = config.ShiftDuration * 60; // minutes
        var actualRunTime = GetActualRunTime(data);
        return actualRunTime / plannedProductionTime;
    }

    private double CalculatePerformance(
        IReadOnlyList<CounterReading> data,
        OeeConfiguration config)
    {
        var actualOutput = GetActualOutput(data);
        var targetOutput = config.TargetRate * config.ShiftDuration;
        return Math.Min(1.0, actualOutput / targetOutput);
    }

    private double CalculateQuality(
        IReadOnlyList<CounterReading> data,
        OeeConfiguration config)
    {
        var goodParts = GetGoodParts(data);
        var totalParts = GetTotalParts(data);
        return totalParts > 0 ? goodParts / totalParts : 0;
    }
}
```

### Configuration

```json
{
  "OeeConfiguration": {
    "LineId": "LINE_01",
    "TargetRate": 100,
    "ShiftDuration": 8.0,
    "PlannedDowntime": [
      {
        "Name": "Lunch Break",
        "StartTime": "12:00",
        "Duration": 30
      },
      {
        "Name": "Shift Change", 
        "StartTime": "16:00",
        "Duration": 15
      }
    ],
    "CounterMapping": {
      "GoodParts": "QualityPassCounter",
      "TotalParts": "ProductionCounter",
      "CycleTime": "CycleCounter"
    }
  }
}
```

### Implementation

```csharp
public class OeeApplication
{
    private readonly ICounterDataService _dataService;
    private readonly IOeeCalculator _oeeCalculator;
    private readonly IShiftSchedule _shiftSchedule;

    public async Task<OeeReport> GenerateShiftReportAsync(
        string lineId, 
        DateTimeOffset shiftStart)
    {
        var shiftEnd = shiftStart.AddHours(8);
        var counterData = await _dataService.GetCounterDataAsync(
            lineId, shiftStart, shiftEnd);

        var oeeMetrics = _oeeCalculator.CalculateOee(
            counterData, 
            GetOeeConfig(lineId));

        var downtimeEvents = await AnalyzeDowntimeEvents(counterData);
        var qualityIssues = await AnalyzeQualityIssues(counterData);

        return new OeeReport
        {
            LineId = lineId,
            ShiftStart = shiftStart,
            ShiftEnd = shiftEnd,
            OeeMetrics = oeeMetrics,
            DowntimeEvents = downtimeEvents,
            QualityIssues = qualityIssues,
            Recommendations = GenerateRecommendations(oeeMetrics)
        };
    }
}
```

---

## Pattern 3: Quality Management Systems

### Implementation

```csharp
public class QualityMonitoringApplication
{
    private readonly ICounterDataService _dataService;
    private readonly IQualityAnalyzer _qualityAnalyzer;
    private readonly IAlertService _alertService;

    public async Task<QualityMetrics> GetRealTimeQualityAsync()
    {
        var readings = await _dataService.GetLatestReadingsAsync();
        
        var qualityData = readings
            .Where(r => r.CounterType.Contains("quality"))
            .GroupBy(r => r.DeviceId)
            .Select(g => CalculateStationQuality(g))
            .ToList();

        var overallQuality = CalculateOverallQuality(qualityData);
        
        // Check for quality alerts
        await CheckQualityThresholds(overallQuality);
        
        return new QualityMetrics
        {
            StationQuality = qualityData,
            OverallQuality = overallQuality,
            TrendAnalysis = await _qualityAnalyzer.AnalyzeTrends(readings),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private StationQualityMetrics CalculateStationQuality(
        IGrouping<string, CounterReading> stationData)
    {
        var inspected = GetCounterValue(stationData, "InspectCounter");
        var passed = GetCounterValue(stationData, "PassCounter");
        var failed = GetCounterValue(stationData, "FailCounter");
        
        var passRate = inspected > 0 ? (double)passed / inspected : 0;
        var defectRate = inspected > 0 ? (double)failed / inspected : 0;

        return new StationQualityMetrics
        {
            StationId = stationData.Key,
            InspectedCount = inspected,
            PassedCount = passed,
            FailedCount = failed,
            PassRate = passRate,
            DefectRate = defectRate,
            QualityGrade = GetQualityGrade(passRate)
        };
    }
}
```

### Configuration

```json
{
  "QualityConfiguration": {
    "StationId": "QC_STATION_01",
    "QualityThresholds": {
      "PassRateMinimum": 0.95,
      "DefectRateMaximum": 0.05,
      "AlertOnConsecutiveFailures": 5
    },
    "Devices": [
      {
        "DeviceId": "ADAM_QC_01",
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "InspectCounter",
            "Description": "Total parts inspected"
          },
          {
            "ChannelNumber": 1,  
            "Name": "PassCounter",
            "Description": "Parts passed inspection"
          },
          {
            "ChannelNumber": 2,
            "Name": "FailCounter", 
            "Description": "Parts failed inspection"
          }
        ]
      }
    ]
  }
}
```

---

## Pattern 4: Inventory and Material Tracking

### Implementation

```csharp
public class InventoryTrackingApplication
{
    private readonly ICounterDataService _dataService;
    private readonly IInventoryCalculator _calculator;
    private readonly IMaterialDatabase _materialDb;

    public async Task<InventoryStatus> GetCurrentInventoryAsync()
    {
        var readings = await _dataService.GetLatestReadingsAsync();
        
        var materialFlows = readings
            .Where(r => r.CounterType == "material")
            .GroupBy(r => r.MaterialType)
            .Select(g => CalculateMaterialFlow(g))
            .ToList();

        var currentLevels = await _calculator.CalculateCurrentLevels(materialFlows);
        var consumption = await _calculator.CalculateConsumptionRates(readings);
        
        return new InventoryStatus
        {
            MaterialFlows = materialFlows,
            CurrentLevels = currentLevels,
            ConsumptionRates = consumption,
            ReorderAlerts = CheckReorderPoints(currentLevels),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private MaterialFlow CalculateMaterialFlow(
        IGrouping<string, CounterReading> materialData)
    {
        var incoming = GetCounterValue(materialData, "IncomingCounter");
        var outgoing = GetCounterValue(materialData, "OutgoingCounter");
        var waste = GetCounterValue(materialData, "WasteCounter");

        return new MaterialFlow
        {
            MaterialType = materialData.Key,
            IncomingCount = incoming,
            OutgoingCount = outgoing,
            WasteCount = waste,
            NetFlow = incoming - outgoing - waste,
            FlowRate = CalculateFlowRate(materialData.ToList())
        };
    }
}
```

---

## Pattern 5: Energy and Utility Monitoring

### Implementation

```csharp
public class EnergyMonitoringApplication
{
    private readonly ICounterDataService _dataService;
    private readonly IEnergyCalculator _energyCalculator;
    private readonly ICostCalculator _costCalculator;

    public async Task<EnergyReport> GenerateEnergyReportAsync(
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        var energyData = await _dataService.GetCounterDataAsync(
            "energy", startTime, endTime);

        var consumption = _energyCalculator.CalculateConsumption(energyData);
        var demand = _energyCalculator.CalculateDemandProfile(energyData);
        var costs = await _costCalculator.CalculateEnergyCosts(consumption);
        var efficiency = CalculateEnergyEfficiency(energyData);

        return new EnergyReport
        {
            Period = new DateRange(startTime, endTime),
            TotalConsumption = consumption.Sum(c => c.Value),
            PeakDemand = demand.Max(d => d.Value),
            TotalCost = costs.Sum(c => c.Amount),
            EfficiencyMetrics = efficiency,
            ConsumptionBySource = consumption.GroupBy(c => c.Source),
            DemandProfile = demand,
            CostBreakdown = costs
        };
    }
}
```

---

## Cross-Cutting Patterns

### 1. Real-Time Alerting

```csharp
public class AlertingService
{
    public async Task ProcessCounterUpdate(CounterReading reading)
    {
        var rules = await GetAlertRules(reading.DeviceId, reading.ChannelNumber);
        
        foreach (var rule in rules)
        {
            if (await EvaluateRule(rule, reading))
            {
                await TriggerAlert(rule, reading);
            }
        }
    }

    private async Task<bool> EvaluateRule(AlertRule rule, CounterReading reading)
    {
        return rule.Condition switch
        {
            AlertCondition.Threshold => reading.CounterValue > rule.ThresholdValue,
            AlertCondition.Rate => await CheckRateCondition(rule, reading),
            AlertCondition.Stalled => await CheckStalledCondition(rule, reading),
            AlertCondition.Quality => await CheckQualityCondition(rule, reading),
            _ => false
        };
    }
}
```

### 2. Data Aggregation

```csharp
public class DataAggregationService
{
    public async Task<AggregatedData> AggregateCounterData(
        string deviceId,
        TimeSpan aggregationWindow,
        AggregationType type)
    {
        var windowStart = DateTimeOffset.UtcNow.Subtract(aggregationWindow);
        var readings = await GetCounterData(deviceId, windowStart);

        return type switch
        {
            AggregationType.Sum => AggregateSum(readings),
            AggregationType.Average => AggregateAverage(readings),
            AggregationType.Rate => AggregateRate(readings),
            AggregationType.Min => AggregateMin(readings),
            AggregationType.Max => AggregateMax(readings),
            _ => throw new ArgumentException($"Unsupported aggregation type: {type}")
        };
    }
}
```

### 3. Historical Analysis

```csharp
public class HistoricalAnalysisService
{
    public async Task<TrendAnalysis> AnalyzeTrends(
        string counterType,
        TimeSpan analysisWindow)
    {
        var historical = await GetHistoricalData(counterType, analysisWindow);
        
        var trends = new TrendAnalysis
        {
            LinearTrend = CalculateLinearTrend(historical),
            SeasonalPatterns = DetectSeasonalPatterns(historical),
            Anomalies = DetectAnomalies(historical),
            Forecasts = GenerateForecasts(historical),
            PerformanceIndicators = CalculateKPIs(historical)
        };

        return trends;
    }
}
```

## Best Practices

### Configuration Management
- Use environment-specific configurations
- Implement configuration validation
- Support hot-reloading for non-critical settings
- Document all configuration options

### Error Handling
- Implement circuit breaker patterns for device communication
- Use retry policies with exponential backoff
- Provide meaningful error messages
- Log all error conditions with context

### Performance Optimization
- Implement data caching for frequently accessed metrics
- Use batch processing for historical analysis
- Optimize database queries with proper indexing
- Consider data compression for long-term storage

### Security
- Implement role-based access control
- Encrypt sensitive configuration data
- Use secure communication protocols
- Audit all data access and modifications

### Monitoring and Observability
- Implement comprehensive health checks
- Use structured logging throughout
- Export metrics to monitoring systems
- Set up alerting for critical conditions

### Testing
- Write unit tests for business logic
- Implement integration tests for data flows
- Use test doubles for external dependencies
- Perform load testing for production scenarios

## Integration Patterns

### MES Integration
```csharp
public class MesIntegrationService
{
    public async Task SendProductionData(ProductionMetrics metrics)
    {
        var mesData = new MesProductionReport
        {
            WorkOrder = metrics.WorkOrderNumber,
            ProductionCount = metrics.TotalProduction,
            QualityMetrics = metrics.QualityData,
            Timestamp = metrics.Timestamp
        };

        await _mesClient.SendProductionReportAsync(mesData);
    }
}
```

### ERP Integration
```csharp
public class ErpIntegrationService
{
    public async Task UpdateInventoryLevels(InventoryStatus inventory)
    {
        foreach (var material in inventory.MaterialFlows)
        {
            await _erpClient.UpdateMaterialQuantityAsync(
                material.MaterialCode,
                material.CurrentLevel,
                material.LastUpdated);
        }
    }
}
```

These patterns provide a foundation for building robust, scalable counter applications. Adapt them to your specific requirements while maintaining the architectural principles and quality standards of the platform.