# Industrial Counter Data Acquisition Platform Architecture
## Extensible C# Framework for Industrial Counter Device Integration

**Version:** 1.0  
**Date:** July 2025  
**Target Platform:** .NET 9+ C# Industrial Solution  

---

## 1. Executive Summary

### 1.1 Purpose
Define a **focused, extensible** C# architecture for **Industrial Counter Data Acquisition Platform** that starts with ADAM counter devices but enables future growth:

**Current Implementation (Phase 1):**
- **ADAM-6050/6051** - Modbus TCP counter data collection for production lines
- **ADAM-6066** - High-density digital I/O for complex production monitoring
- **Time-Series Storage** - InfluxDB optimized for counter data and production metrics
- **Industrial APIs** - REST/WebSocket access to production counter data
- **OEE Integration** - Built-in Overall Equipment Effectiveness calculations

**Extensible Foundation (Future Phases):**
- **Interface-Driven Design** - Easy addition of new ADAM series devices (6050, 6066, etc.) and protocols
- **Plugin Architecture** - No core changes needed for new ADAM device capabilities
- **Counter-Focused Processing** - Specialized support for production counters, quality gates, and manufacturing metrics
- **Multi-Device Coordination** - Orchestration across multiple ADAM devices on production lines

### 1.2 Design Principles
- **Start Simple, Build Smart** - Implement only what's needed for production counter monitoring today
- **Interfaces Over Implementations** - Abstract away concrete counter device implementations
- **Composition Over Inheritance** - Flexible, testable component design for manufacturing systems
- **Data-Driven Decisions** - Let counter data characteristics guide architecture choices
- **Future-Proof Foundations** - Design for extensibility to additional counter device types
- **Industrial-Grade Reliability** - 24/7 operation with comprehensive error handling
- **Manufacturing Focus** - Optimized for production lines, OEE, and quality tracking

### 1.3 Python vs C# Strategy
- **Python Solutions:** Lightweight, single-purpose counter monitoring, rapid prototyping
- **C# Platform:** Industrial-grade, multi-counter device, enterprise manufacturing architecture
- **Migration Path:** Python counter prototypes evolve into C# production manufacturing modules

---

## 2. Core Architecture Overview

### 2.1 Layered Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway Layer                        │
│  [REST API] [WebSocket] [GraphQL] [Export Services]        │
├─────────────────────────────────────────────────────────────┤
│                 Data Processing Layer                       │
│  [Validation] [Transformation] [Basic Filtering]           │
│  [Calibration] [Data Classification] [Quality Checks]      │
├─────────────────────────────────────────────────────────────┤
│                   Data Ingestion Layer                      │
│  [Device Manager] [Data Router] [Discovery Engine]         │
│  [Connection Health] [Retry Logic] [Error Handling]        │
├─────────────────────────────────────────────────────────────┤
│                    Protocol Layer                          │
│  [Modbus] [TCP Raw] [MQTT] [OPC-UA] [HTTP] [Serial]       │
├─────────────────────────────────────────────────────────────┤
│                   Transport Layer                          │
│  [TCP/IP] [Serial] [USB] [Ethernet] [WiFi] [Bluetooth]    │
├─────────────────────────────────────────────────────────────┤
│                    Storage Layer                           │
│  [SQL Server] [InfluxDB] [Redis] [File System]            │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Domain Model
```
IoTDataIngestionPlatform
├── DeviceManagement/
│   ├── IDeviceProvider
│   ├── IDeviceConfiguration  
│   └── IDeviceHealthMonitor
├── ProtocolHandling/
│   ├── IProtocolProvider
│   ├── IProtocolDiscovery
│   └── IProtocolTemplate
├── DataProcessing/
│   ├── IDataProcessor
│   ├── IDataClassifier
│   ├── IDataValidator
│   └── IBasicCalibration
├── Storage/
│   ├── ITimeSeriesRepository
│   ├── ITransactionalRepository
│   └── IConfigurationRepository
└── Integration/
    ├── IApiGateway
    ├── IDataExportService
    └── INotificationService
```

---

## 3. Device Abstraction Layer

### 3.1 Universal Device Interface
```csharp
public interface IDeviceProvider
{
    string DeviceType { get; }
    string Manufacturer { get; }
    
    Task<bool> ConnectAsync(DeviceConfiguration config);
    Task<bool> DisconnectAsync();
    Task<DeviceStatus> GetHealthStatusAsync();
    Task<IDataPoint[]> ReadDataAsync();
    
    event EventHandler<DataReceivedEventArgs> DataReceived;
    event EventHandler<DeviceStatusEventArgs> StatusChanged;
}
```

### 3.2 Device Implementation Strategy

**Phase 1 (Current Implementation):**
```
AdamDeviceProvider
├── Adam6051Provider (Modbus TCP counters)
```

**Future Extensibility (Interface-Ready):**
```
IDeviceProvider (interface)
├── AdamDeviceProvider (current implementation)
├── [Future] SiemensDeviceProvider 
├── [Future] ModbusGenericProvider
├── [Future] MqttDeviceProvider  
└── [Future] HttpRestProvider
```

**Key Principle:** Each new device type implements `IDeviceProvider` - no changes to core platform needed.

### 3.3 Device Configuration Management
```csharp
public class DeviceConfiguration
{
    public Guid DeviceId { get; set; }
    public string Name { get; set; }
    public string DeviceType { get; set; }
    public Dictionary<string, object> ConnectionParameters { get; set; }
    public Dictionary<string, object> ProtocolSettings { get; set; }
    public DataClassification DataType { get; set; }
    public StoragePolicy StoragePolicy { get; set; }
}
```

---

## 4. Protocol Layer Architecture

### 4.1 Protocol Provider Framework
```csharp
public interface IProtocolProvider
{
    string ProtocolName { get; }
    string[] SupportedTransports { get; }
    
    Task<bool> InitializeAsync(ProtocolConfiguration config);
    Task<IDataFrame> ReadAsync();
    Task<bool> WriteAsync(IDataFrame data);
    
    bool SupportsDiscovery { get; }
    IProtocolDiscovery CreateDiscoveryEngine();
}
```

### 4.2 Protocol Implementation Strategy

**Phase 1 (Current Implementation):**
```
ModbusTcpProvider (for ADAM-6051)
├── Read holding registers
├── 32-bit counter reconstruction  
└── Connection management with retry logic
```

**Future Extensibility:**
```
IProtocolProvider (interface)
├── ModbusTcpProvider (implemented)
├── RawTcpProvider (implemented)
├── [Future] MqttProvider
├── [Future] OpcUaProvider
└── [Future] HttpRestProvider
```

### 4.3 Protocol Discovery Framework
```csharp
public interface IProtocolDiscovery
{
    Task<DiscoverySession> StartDiscoveryAsync(ITransportProvider transport);
    Task<DiscoveryResult> ProcessSampleAsync(DiscoverySession session, 
                                           string userInput, 
                                           byte[] streamData);
    Task<ProtocolTemplate> FinalizeTemplateAsync(DiscoverySession session);
    
    event EventHandler<DiscoveryProgressEventArgs> ProgressUpdated;
}
```

---

## 5. Data Processing Framework

### 5.1 Basic Data Processing Pipeline
```csharp
public class DataProcessingPipeline
{
    public List<IDataProcessor> Processors { get; set; }
    
    // Simple processing stages for IoT data ingestion
    public async Task<ProcessedData> ProcessAsync(RawData data)
    {
        var result = data;
        
        // Stage 1: Data validation (format, range checks)
        result = await ValidateAsync(result);
        
        // Stage 2: Basic calibration (conversion factors, offsets, rate calculations)
        result = await ApplyBasicCalibrationAsync(result);
        
        // Stage 3: Data classification (time-series vs discrete)
        result = await ClassifyDataAsync(result);
        
        // Stage 4: Quality checks (threshold alarms)
        result = await CheckQualityAsync(result);
        
        return result;
    }
}
```


---

## 6. Data Classification & Storage

### 5.1 Data Classification Engine
```csharp
public enum DataClassification
{
    TimeSeriesContinuous,    // Continuous monitoring (InfluxDB)
    TimeSeriesDiscrete,      // Periodic measurements (InfluxDB)
    TransactionalRecord,     // Business transactions (SQL Server)
    ConfigurationData,       // Settings and templates (SQL Server)
    CacheData               // Temporary/latest values (Redis)
}

public interface IDataClassifier
{
    DataClassification ClassifyData(IDataPoint dataPoint, DeviceContext context);
    StoragePolicy DetermineStoragePolicy(DataClassification classification);
}
```

### 5.2 Data Processing Pipeline
```
Raw Data → Validation → Classification → Transformation → Routing → Storage
    ↓           ↓            ↓             ↓           ↓        ↓
[Sensors] → [Rules] → [Time/Trans] → [Formats] → [Router] → [DB]
```

### 5.3 Data Processor Implementation Strategy

**Phase 1 (Current Implementation):**
```
CounterProcessor (ADAM-6051)
├── Rate calculation over time windows
├── 32-bit overflow detection
├── Data classification: TimeSeriesContinuous → InfluxDB

CounterProcessor (ADAM-6051 counters)  
├── Stability detection algorithms
├── Tare/gross/net handling
├── Data classification: TransactionalRecord → SQL Server
```

**Future Extensibility:**
```
IDataProcessor (interface)
├── CounterProcessor (implemented)
├── WeightProcessor (implemented)  
├── [Future] TemperatureProcessor
├── [Future] PressureProcessor
└── [Future] GenericAnalogProcessor
```

---

## 6. Storage Architecture

### 6.1 Multi-Database Strategy
```csharp
public interface IDataRepository
{
    string StorageType { get; }
    Task<bool> WriteAsync(IDataPoint[] dataPoints);
    Task<IDataPoint[]> QueryAsync(DataQuery query);
    Task<bool> ConfigureAsync(StorageConfiguration config);
}
```

### 6.2 Storage Implementation Strategy

**Phase 1 (Current Implementation):**
```
┌─────────────────────┬──────────────┬──────────────┐
│ Data Type           │ Storage      │ Rationale    │
├─────────────────────┼──────────────┼──────────────┤
│ ADAM-6051 Counters  │ InfluxDB     │ Time-series  │
│ Scale Weights       │ SQL Server   │ Discrete     │
│ Device Config       │ SQL Server   │ Relational   │
│ Protocol Templates  │ SQL Server   │ Structured   │
└─────────────────────┴──────────────┴──────────────┘
```

**Future Extensibility:**
- **Redis Cache** - When real-time performance becomes critical
- **File System** - When backup/export requirements emerge  
- **Cloud Storage** - When multi-site deployments are needed

**Key Principle:** Start with the two databases we need, add others when use cases demand them.

### 6.3 SQL Server Schema Design
```sql
-- Device Management
Devices (DeviceId, Name, Type, Configuration, CreatedAt)
DeviceConnections (ConnectionId, DeviceId, Status, LastSeen)

-- Protocol Templates  
ProtocolTemplates (TemplateId, Name, ProtocolType, Definition)
DeviceTemplates (DeviceId, TemplateId, IsActive)

-- Transactional Data
DiscreteReadings (ReadingId, DeviceId, Value, Unit, Timestamp, Metadata)
BatchRecords (BatchId, DeviceId, StartTime, EndTime, TotalItems)

-- Configuration & Audit
SystemConfiguration (ConfigKey, ConfigValue, ModifiedAt, ModifiedBy)
AuditLog (LogId, EntityType, EntityId, Action, OldValue, NewValue, Timestamp)
```

---

## 7. Service Layer Architecture

### 7.1 Service Implementation Strategy

**Phase 1 (Current Implementation):**
```csharp
// Core services we need TODAY
IDeviceManagementService
├── RegisterDevice(ADAM-6051 or future counter devices)
├── StartDevice / StopDevice  
└── GetDeviceStatus

IDataRoutingService
├── RouteCounterData → InfluxDB
├── RouteWeightData → SQL Server
└── RouteConfigData → SQL Server

IProtocolDiscoveryService (future devices)
├── StartDiscoverySession
├── ProcessWeightTest  
└── SaveScaleTemplate
```

**Future Extensibility:**
- Services implement interfaces, so new capabilities extend naturally
- Plugin architecture allows new services without core changes
- Configuration-driven service registration

### 7.2 Background Services
```
Windows Services / Linux Daemons:
├── DeviceMonitoringService (Connection health, auto-reconnect)
├── DataCollectionService (Continuous data acquisition)
├── DataRoutingService (Intelligent storage routing)
├── AlertingService (Threshold monitoring, notifications)
└── MaintenanceService (Cleanup, archival, health checks)
```

---

## 8. API Gateway & Integration

### 8.1 API Implementation Strategy

**Phase 1 (Current Implementation):**
```
Device Management:
GET    /api/devices                     # List ADAM devices
POST   /api/devices/6050                # Register ADAM-6050
POST   /api/devices/6051                # Register ADAM-6051  
POST   /api/devices/6066                # Register ADAM-6066
POST   /api/devices/counter             # Register counter devices
POST   /api/devices/{id}/start          # Start data collection
GET    /api/devices/{id}/status         # Connection health

Counter Discovery (future devices):
POST   /api/discovery/start             # Begin counter device discovery
POST   /api/discovery/counter-test      # Process counter validation test
POST   /api/discovery/save              # Save counter device template

Data Access:
GET    /api/data/counters               # Latest counter values
GET    /api/data/production             # Production line metrics
GET    /api/data/oee                    # OEE calculations
GET    /api/data/export                 # Export data (CSV)
```

**Future Extensibility:**
- RESTful design supports adding new device types naturally
- Generic `/api/devices` endpoint handles any device type
- Protocol discovery pattern works for any unknown device protocol

### 8.2 WebSocket Streams
```
Real-time Data Streams:
/ws/data/live                          # All device data
/ws/data/device/{id}                   # Single device stream  
/ws/discovery/{sessionId}              # Discovery progress
/ws/system/alerts                      # System notifications
/ws/system/health                      # Connection status
```

---

## 9. Extensibility Framework

### 9.1 Plugin Architecture
```csharp
public interface IDevicePlugin
{
    string PluginName { get; }
    string Version { get; }
    string[] SupportedDeviceTypes { get; }
    
    IDeviceProvider CreateProvider();
    IProtocolProvider CreateProtocol();
    IDataProcessor CreateProcessor();
}
```

### 9.2 Extension Points
```
Plugin Types:
├── DeviceProviderPlugin (New device types)
├── ProtocolProviderPlugin (New communication protocols)  
├── DataProcessorPlugin (Custom data processing)
├── StorageProviderPlugin (New database backends)
├── NotificationPlugin (Custom alerting channels)
└── ExportPlugin (Additional export formats)
```

### 9.3 Configuration-Driven Extensions
```json
{
  "plugins": [
    {
      "name": "CustomScaleProvider",
      "assembly": "CustomDevices.dll",
      "type": "CustomDevices.ScaleProvider",
      "enabled": true,
      "configuration": {
        "defaultPort": 4001,
        "timeout": 5000
      }
    }
  ]
}
```

---

## 10. Deployment & Scalability

### 10.1 Deployment Options
```
Deployment Scenarios:
├── Single Machine (Small facility)
│   └── All services in one process
├── Distributed (Multiple locations)  
│   └── Service per location, central database
├── Microservices (Enterprise)
│   └── Containerized services, orchestrated
└── Cloud Native (Global scale)
    └── Azure/AWS services, auto-scaling
```

### 10.2 Technology Stack
```
Core Platform:
├── .NET 9+ (Cross-platform runtime)
├── ASP.NET Core (Web API, WebSockets)
├── Entity Framework Core (ORM)
├── SignalR (Real-time communication)
└── Serilog (Structured logging)

Databases:
├── SQL Server (Primary transactional)
├── InfluxDB (Time-series data)  
├── Redis (Caching, pub/sub)
└── SQLite (Local configuration)

Infrastructure:
├── Docker (Containerization)
├── Kubernetes (Orchestration)
├── NGINX (Reverse proxy)
└── Grafana (Monitoring dashboards)
```

---

## 11. Migration Strategy

### 11.1 From Python Prototypes
```
Migration Path:
1. Python proof-of-concept validates approach
2. Extract core algorithms and data structures  
3. Implement as C# service with full architecture
4. Python becomes testing/validation tool
5. Retire Python for production use
```

### 11.2 Incremental Implementation
```
Phase 1: Core Framework + ADAM devices
Phase 2: Protocol discovery engine
Phase 3: Additional device types (Siemens, AB)
Phase 4: Advanced data processing & analytics  
Phase 5: Cloud integration & enterprise features
```

---

## 12. Quality & Operations

### 12.1 Testing Strategy
```
Test Types:
├── Unit Tests (Individual components)
├── Integration Tests (Protocol communication)
├── System Tests (End-to-end workflows)
├── Performance Tests (High-frequency data)
└── Reliability Tests (24/7 operation)
```

### 12.2 Monitoring & Observability
```
Monitoring Stack:
├── Application Performance (APM)
├── Infrastructure Monitoring  
├── Database Performance
├── Device Health Status
└── Business Metrics (Data quality, uptime)
```

---

## Conclusion

This architecture provides a comprehensive **Manufacturing Intelligence Platform** that serves as the foundation for Industry 4.0 implementations:

**Data Foundation:**
- **Universal Device Integration** - Any sensor, any protocol, any manufacturer
- **Advanced Processing** - Calibration, signal processing, ML inference
- **Intelligent Storage** - Optimal database selection based on data characteristics

**Business Applications:**
- **OEE Optimization** - Real-time equipment effectiveness monitoring
- **Quality Management** - Statistical process control and quality analytics  
- **Predictive Maintenance** - ML-driven failure prediction and optimization
- **Process Analytics** - Trend analysis, correlation discovery, optimization

**Platform Benefits:**
- **Rapid Deployment** - Protocol discovery eliminates configuration complexity
- **Infinite Extensibility** - Plugin architecture for any future requirements
- **Enterprise Scale** - Microservice architecture supports unlimited growth
- **Innovation Pipeline** - Python prototyping enables rapid feature development

**Strategic Value:**
This **IoT Data Ingestion Platform** solves the fundamental challenge of connecting diverse shopfloor devices to data systems:

1. **Eliminate Configuration Complexity** - Protocol discovery makes any device "plug-and-play"
2. **Universal Connectivity** - Support any device, any protocol, any manufacturer
3. **Clean Data Pipeline** - Validated, calibrated data ready for consumption
4. **Simple Integration** - REST/WebSocket APIs for easy connection to MES, ERP, analytics systems
5. **Extensible Foundation** - Add new device types and protocols without architectural changes

The clear separation between lightweight Python prototypes and the focused C# platform ensures rapid innovation while maintaining production reliability for critical shopfloor data collection.