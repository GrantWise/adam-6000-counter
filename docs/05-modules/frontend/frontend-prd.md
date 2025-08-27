# Product Requirements Document (PRD)
## ADAM Counter Logger Frontend

### 1. Overview

The ADAM Counter Logger Frontend is a web-based configuration and monitoring interface for industrial technicians to manage ADAM-6000 series counter devices. These devices are industrial-grade hardware modules that count electrical pulses from production equipment, quality gates, conveyor systems, and other manufacturing machinery.

**What are Industrial Counters?**
Industrial counters track production metrics by counting electrical pulses. For example:
- A bottle filling line sends a pulse each time a bottle passes a sensor
- A quality inspection station sends pulses for "passed" and "failed" items
- A packaging machine counts completed packages

The ADAM-6000 devices connect to these pulse sources and communicate the count values over Ethernet using the Modbus TCP protocol, an industry-standard for industrial automation.

**Design Philosophy**
This frontend prioritizes direct access to all features without wizards or progressive disclosure, designed for experienced technicians who need full control and visibility. Every setting and feature is immediately accessible - no hunting through menus or multi-step processes.

### 2. Goals & Non-Goals

#### Goals
- Provide comprehensive device configuration interface for ADAM units
- Enable real-time monitoring of device health and connection status
- Display current counter values and rates without complex graphing
- Offer direct access to all configuration options (no wizards)
- Support multiple simultaneous device configurations
- Provide clear error messages and troubleshooting information
- Enable production testing and validation capabilities
- Expose all available system functionality through the UI

#### Non-Goals
- Complex data visualization (handled by Grafana)
- Historical data analysis (handled by InfluxDB/Grafana)
- User management or authentication (initial version)
- Mobile-first design (desktop priority for control room use)
- Report generation beyond test reports

### 3. User Persona

**Primary User**: Industrial Automation Technician
- 5+ years experience with Modbus devices
- Familiar with register addresses and industrial protocols
- Needs quick access to all settings without navigation barriers
- Values diagnostic information and connection details
- Works in control room environment with standard monitors
- Expects full system visibility and control

### 4. Technology Stack

- **Frontend Framework**: React with TypeScript
- **UI Components**: shadcn/ui
- **Styling**: Tailwind CSS
- **State Management**: Zustand or React Context
- **API Client**: Axios with React Query
- **Real-time Updates**: SignalR or WebSockets
- **Build Tool**: Vite
- **Testing**: Vitest + React Testing Library

### 5. Core Features

#### 5.1 Device Management

**Device List View**

The main screen displays all configured ADAM counter devices in a table format. Each row represents one physical ADAM device on the factory network.

**Table Columns:**
- **Device ID**: Unique identifier (e.g., "PROD_LINE_1_COUNTER")
- **Device Name**: Human-friendly name (e.g., "Main Production Line Counter")
- **IP:Port**: Network address (e.g., "192.168.1.100:502")
- **Unit ID**: Modbus address (typically 1-247)
- **Status**: Real-time connection state
  - 🟢 Connected: Device responding normally
  - 🟡 Warning: Intermittent issues or degraded performance
  - 🔴 Disconnected: No communication
  - ⚠️ Error: Active fault requiring attention
- **Last Contact**: Time since last successful communication
- **Actions**: Quick operation buttons

**Quick Actions (per device):**
- **Edit**: Modify device configuration
- **Delete**: Remove device (with confirmation)
- **Test Connection**: Run diagnostic connection test
- **Enable/Disable**: Temporarily stop polling without deleting configuration

**Global Actions:**
- **Add Device**: Configure a new ADAM device
- **Export Configuration**: Download all device configs as JSON file
- **Import Configuration**: Load previously saved configurations

**Device Configuration Panel**
- All settings visible on single screen (no tabs/accordions)
- Fields grouped by logical sections with clear borders
- Real-time validation with immediate error display
- Test Connection button with detailed results
- Save/Cancel with confirmation for changes

**Configuration Fields**:

**Device Information Section:**
- **Device ID**: Unique identifier for this device
  - Auto-generated (e.g., "ADAM_001") but can be customized
  - Used in logs and data storage for tracking
  - Example: "BOTTLING_LINE_3_COUNTER"
  
- **Device Name**: Human-readable name
  - Displayed in monitoring screens
  - Example: "Bottling Line 3 Main Counter"
  
- **Description**: Free text for additional context
  - Example: "Counts bottles after capping station, before case packer"

**Connection Settings Section:**
- **IP Address**: Network address of the ADAM device
  - Must be accessible from the server running this software
  - Example: "192.168.1.100"
  
- **Port**: TCP port for Modbus communication
  - Default: 502 (standard Modbus TCP port)
  - Some installations may use custom ports
  
- **Unit ID**: Modbus slave address
  - Range: 1-247
  - Each device on same network must have unique ID
  - Default: 1
  
- **Timeout (ms)**: How long to wait for device response
  - Default: 5000ms (5 seconds)
  - Increase for devices on slow networks
  - Decrease for local high-speed networks
  
- **Max Retries**: Number of retry attempts on failure
  - Default: 3
  - Set higher for unreliable networks
  - Set to 0 to fail immediately (not recommended)
  
- **Retry Delay (ms)**: Wait time between retry attempts
  - Default: 1000ms (1 second)
  - Prevents flooding device with requests

**Polling Configuration Section:**
- **Poll Interval (ms)**: How often to read counter values
  - Default: 2000ms (2 seconds)
  - Minimum: 100ms (10 readings per second)
  - Maximum: 300000ms (5 minutes)
  - Consider network load and data resolution needs
  
- **Enable/Disable Device**: Master on/off switch
  - Disabled devices remain configured but are not polled
  - Useful for maintenance or temporary shutdown
  
- **Priority Level**: Polling priority when multiple devices configured
  - High: Poll first in each cycle
  - Normal: Standard priority
  - Low: Poll last, may skip if cycle running long

#### 5.2 Channel Configuration

**Per-Device Channel Grid**
- All 16 channels displayed in expandable rows
- Direct editing in grid cells
- Bulk operations (Enable/Disable selected)
- Copy configuration between channels
- Channel validation based on data type

**Channel Configuration Fields**:

Each ADAM device has up to 16 channels (0-15), where each channel represents one counter input. For example, Channel 0 might count "good products" while Channel 1 counts "rejects".

- **Channel Number**: 0-15 (read-only)
  - Physical input number on the ADAM device
  - Cannot be changed, only enabled/disabled

- **Channel Name**: Descriptive name for this counter
  - Example: "Good_Products", "Rejects", "Total_Count"
  - Used in data storage and displays

- **Description**: Detailed explanation of what this counts
  - Example: "Counts bottles that passed vision inspection"

- **Enable/Disable**: Whether to read this channel
  - Disabled channels are skipped to improve performance
  - Disable unused channels

- **Start Register**: Modbus register address where counter value begins
  - ADAM devices store counter values in numbered registers
  - Example: Register 0 for Channel 0, Register 2 for Channel 1
  - Consult ADAM device manual for register mapping

- **Register Count**: How many 16-bit registers this counter uses
  - 1 register: 16-bit counter (0-65,535)
  - 2 registers: 32-bit counter (0-4,294,967,295)
  - 4 registers: 64-bit counter (0-18,446,744,073,709,551,615)
  - Larger counters needed for high-speed or long-running operations

- **Data Type**: How to interpret the register data
  - UInt16: Unsigned 16-bit integer
  - UInt32: Unsigned 32-bit integer
  - UInt64: Unsigned 64-bit integer
  - Must match the register count

- **Scale Factor**: Multiply raw value by this number
  - Default: 1.0 (no scaling)
  - Example: 0.001 to convert milliseconds to seconds
  - Example: 12 to convert dozens to individual items

- **Offset**: Add this value after scaling
  - Default: 0 (no offset)
  - Used for calibration adjustments

- **Min/Max Values**: Expected range for validation
  - System alerts if values fall outside this range
  - Example: Min=0, Max=1000 for hourly production count
  - Helps detect sensor failures or data errors

- **Counter Type**:
  - **Incremental**: Counter always increases (normal counting)
  - **Absolute**: Counter can increase or decrease
  - Most production counters are incremental

- **Overflow Behavior**: What happens when counter reaches maximum
  - **Wrap**: Reset to 0 and continue (e.g., 65535 → 0)
  - **Saturate**: Stop at maximum value
  - **Error**: Generate alert and stop processing
  - Most counters use "Wrap" with software handling the rollover

- **Unit of Measurement**: What the count represents
  - Examples: "bottles", "cases", "cycles", "pieces"
  - Used for display and reporting

#### 5.3 Real-Time Monitoring

**Device Health Dashboard**

A grid of cards, one per device, providing at-a-glance health status. Think of this as your "factory floor overview" - you should be able to spot problems immediately.

**Per-Device Status Card Contents:**
- **Connection State**: Large color-coded indicator
  - 🟢 Green: Connected and responding
  - 🟡 Yellow: Intermittent issues (some failures)
  - 🔴 Red: Disconnected or critical errors
  - 🔵 Blue: Disabled (intentionally offline)

- **Device Name**: "Bottling Line Counter"
- **Last Read**: "2 seconds ago" (updates live)
- **Current Status**: "Reading channel data..."
- **Error Display**: If any, shows latest error message

- **Communication Statistics**:
  - **Success Rate**: "99.8%" (last 1000 attempts)
  - **Avg Response**: "23ms" (lower is better)
  - **Failed Reads**: "2 today" (resets at midnight)
  - **Uptime**: "6 days, 4 hours"

- **Quick Actions** (icon buttons):
  - 🔄 **Reconnect**: Force immediate connection attempt
  - 📄 **View Logs**: Open device-specific log viewer
  - ⚡ **Quick Test**: Run connection diagnostic
  - ⏸️ **Pause**: Temporarily stop polling

- **Resource Impact**:
  - Shows this device's CPU/Memory usage
  - Helps identify problem devices affecting system

**Counter Values Display**

Real-time table showing all counter readings across all devices. This is where operators monitor production counts.

**Table Columns Explained:**
- **Device**: Which ADAM device (e.g., "Line_1_Counter")
- **Channel**: Channel name (e.g., "Good_Products")
- **Current Value**: Latest counter reading
  - Updates based on poll interval
  - Bold = recently changed
  - Gray = no recent changes
- **Rate/Min**: Calculated rate per minute
  - Based on last 60 seconds of data
  - Shows production speed
- **Rate/Hour**: Projected hourly rate
  - Useful for meeting targets
- **Last Update**: Time since last reading
  - "Now" = Just updated
  - "5s ago" = Normal
  - "1m ago" = Possible issue
  - Red text if > poll interval
- **Data Quality**: Reading reliability
  - 🟢 **Good**: Normal operation
  - 🟡 **Uncertain**: Possible issues
  - 🔴 **Bad**: Invalid data
  - 🟠 **Config Error**: Setup problem

**Display Features:**
- **Auto-Refresh**: Updates every 1-60 seconds (configurable)
- **Change Highlighting**: Flashes yellow when value changes
- **Sorting**: Click column headers to sort
- **Filtering**: Search box to find specific channels
- **Export Button**: Download current snapshot as CSV file
- **Pause Button**: Freeze display for analysis

**System Health Overview**

A dedicated panel showing the health of the entire counting system:

- **Overall System Status**: Single indicator showing system health
  - 🟢 Healthy: All systems operational
  - 🟡 Warning: Non-critical issues present
  - 🔴 Critical: Immediate attention required

- **Active Alerts and Warnings**: List of current issues
  - Device connection failures
  - Data quality problems
  - Performance degradation
  - Configuration errors
  - Each alert shows: Severity, Time, Device/Component, Description

- **Resource Usage Gauges**:
  - **CPU Usage**: Percentage of processor used by counting system
    - Normal: < 50%
    - Warning: 50-80%
    - Critical: > 80%
  - **Memory Usage**: RAM consumption
    - Shows used/available in GB
    - Alerts if approaching limits
  - **Network Bandwidth**: Data transfer rate
    - Mbps used for device communication
    - Important for high-frequency polling

- **InfluxDB Connection Status**: Time-series database health
  - 🟢 Connected: Data being stored successfully
  - 🔴 Disconnected: Data may be buffered locally
  - Shows: Last write time, write rate, queue size

- **Performance Counters Status**: System metrics collection
  - Indicates if performance monitoring is active
  - Shows collection interval and last update

#### 5.4 Global Configuration

**Service Configuration Panel**

Global settings that affect the entire counting system. These are advanced settings that control how the software operates.

**Performance Settings Section:**

- **Max Concurrent Devices (1-50)**:
  - How many devices to poll simultaneously
  - Default: 10
  - Higher = Faster overall polling but more network/CPU load
  - Lower = Slower but more stable on limited resources
  - Example: Set to 5 if running on older hardware

- **Data Buffer Size (100-100,000)**:
  - Number of readings to hold in memory before processing
  - Default: 10,000
  - Larger buffer = Better handling of processing delays
  - Smaller buffer = Less memory usage
  - Increase if seeing "buffer full" warnings

- **Batch Size (1-1,000)**:
  - Number of readings to process together
  - Default: 100
  - Larger = More efficient but higher latency
  - Smaller = Lower latency but more processing overhead
  - Balance based on your real-time requirements

- **Batch Timeout (ms)**:
  - Maximum time to wait before processing partial batch
  - Default: 5000ms (5 seconds)
  - Ensures data flows even with slow polling
  - Lower for more real-time display updates

- **Health Check Interval (ms)**:
  - How often to verify device connectivity
  - Default: 30000ms (30 seconds)
  - More frequent = Faster problem detection
  - Less frequent = Lower network overhead

**Error Handling Section:**

- **Enable Automatic Recovery**:
  - ✓ Checked: System automatically attempts to reconnect failed devices
  - ☐ Unchecked: Failed devices stay offline until manually restarted
  - Recommended: Always enabled for production

- **Max Consecutive Failures**:
  - Failed reads before marking device offline
  - Default: 10
  - Prevents endless retry loops
  - Higher for unreliable networks
  - Lower for quick failure detection

- **Device Timeout Minutes**:
  - Time before considering device unresponsive
  - Default: 5 minutes
  - After timeout, device marked as "Needs Attention"
  - Triggers alerts if configured

**Monitoring Section:**

- **Enable Performance Counters**:
  - ✓ Checked: Collect detailed performance metrics
  - ☐ Unchecked: Minimal metrics (lower overhead)
  - Enable for troubleshooting or optimization

- **Enable Detailed Logging**:
  - ✓ Checked: Verbose logs for debugging
  - ☐ Unchecked: Standard logging only
  - Warning: Can impact performance and disk usage

- **Enable Demo Mode**:
  - ✓ Checked: Generate fake counter data for testing
  - ☐ Unchecked: Connect to real devices
  - Useful for training or development

**InfluxDB Settings Section:**

Configuration for time-series database where counter data is stored.

- **URL**: InfluxDB server address
  - Example: "http://localhost:8086"
  - Include protocol (http/https) and port

- **Token**: Authentication token
  - Generated by InfluxDB admin
  - Keep secure - allows data write access

- **Organization**: InfluxDB organization name
  - Logical grouping in InfluxDB
  - Example: "manufacturing_corp"

- **Bucket**: Database bucket name
  - Where counter data is stored
  - Example: "production_counters"

- **Measurement**: Table name for counter data
  - Default: "counter_data"
  - All readings stored under this name

- **Write Batch Size**: Records to send at once
  - Default: 50
  - Larger = More efficient writes
  - Smaller = Lower memory usage

- **Flush Interval (ms)**: Force write incomplete batches
  - Default: 5000ms
  - Ensures data written even with slow polling

#### 5.5 Testing & Validation

**Production Testing Interface**

Comprehensive testing system to validate the counter system is ready for production use. This is equivalent to running the backend with the `--test` flag but through the web interface.

**Test Execution Options:**
- **Run All Tests**: Execute complete test suite (approximately 5 minutes)
- **Run by Category**: Select specific test groups

**Test Categories Explained:**

1. **Configuration Tests**:
   - Validates all device and channel configurations
   - Checks for conflicts or invalid settings
   - Verifies register mappings make sense
   - Example: Alerts if two channels use same registers

2. **Connection Tests**:
   - Attempts to connect to each configured device
   - Measures connection time and stability
   - Tests Modbus communication protocol
   - Identifies network issues or wrong IP addresses

3. **Data Quality Tests**:
   - Reads sample data from each channel
   - Verifies data is within expected ranges
   - Checks for stuck values (counter not changing)
   - Detects configuration mismatches

4. **Performance Benchmarks**:
   - Measures system capacity
   - Tests maximum polling rate achievable
   - Identifies bottlenecks
   - Provides optimization recommendations

5. **Health Check Tests**:
   - Verifies all monitoring systems work
   - Tests alert generation
   - Validates resource usage tracking
   - Ensures fail-safes operate correctly

6. **Device Discovery**:
   - Scans network for ADAM devices
   - Identifies unconfigured devices
   - Helps find devices with changed IP addresses
   - Not all devices support discovery

**Test Results Display:**
- **Pass** ✅: Test completed successfully
- **Warning** ⚠️: Test passed but found issues
- **Fail** ❌: Test failed - requires attention
- **Skip** ⏭️: Test skipped (dependencies not met)

Each test shows:
- Test name and description
- Execution time
- Detailed results or error messages
- Specific recommendations if failed

**Production Readiness Score:**
- Overall percentage score (0-100%)
- Based on test results weighted by importance
- 90%+ = Ready for production
- 70-89% = Functional but needs attention
- <70% = Not ready, critical issues present

**Critical Issues Section:**
- Lists problems that MUST be fixed before production
- Examples:
  - "No devices configured"
  - "InfluxDB connection failed"
  - "Invalid channel configurations detected"

**Recommendations Section:**
- Suggestions for optimal performance
- Examples:
  - "Consider reducing poll interval for 50+ channels"
  - "Enable performance counters for better monitoring"
  - "Increase timeout for devices on remote network"

**Export Options:**
- **Console**: Text format for command-line viewing
- **JSON**: Machine-readable for automation
- **Markdown**: Formatted for documentation
- **HTML**: Full report with styling for management

**Individual Test Execution**

For running specific tests rather than the full suite:

**Test Selection Interface:**
- **Dropdown Menu**: Lists all available tests by category
  - Grouped by: Configuration, Connection, Performance, etc.
  - Search box for finding specific tests
  - Recently run tests at top

- **Test Information Display** (shown when test selected):
  - **Description**: What this test validates
  - **Estimated Duration**: "~30 seconds"
  - **Requirements**: What must be configured
    - Example: "Requires at least one device configured"
    - Example: "Requires InfluxDB connection"
  - **Dependencies**: Other tests that must pass first
    - Shown as checklist with status
    - Can't run if dependencies failed

**During Test Execution:**
- **Progress Bar**: Visual percentage complete
- **Current Step**: "Testing device PROD_LINE_1..."
- **Elapsed Time**: Running timer
- **Cancel Button**: Stop test early
- **Live Log**: Scrolling test output
  - Info messages in black
  - Warnings in orange
  - Errors in red

**Test Results Panel:**
- **Overall Status**: Large PASS/FAIL/WARNING indicator
- **Score**: If applicable (e.g., "Performance: 87/100")
- **Duration**: Actual time taken
- **Detailed Findings**:
  ```
  ✅ Device connection successful
  ✅ All channels readable
  ⚠️ Response time higher than recommended (avg 145ms)
  ❌ Register 40 returned error code 02
  ```
- **Recommendations**: Specific fixes for any issues
- **Rerun Button**: Run same test again
- **Export Results**: Save as text/JSON file

#### 5.6 Diagnostics & Troubleshooting

**Connection Test Panel**

When you click "Test Connection" on a device, this panel provides comprehensive diagnostics. It's like having a network engineer look at the connection.

**Test Stages** (shown as progress steps):

1. **TCP Connection Test**:
   - Attempts basic network connection to IP:Port
   - Shows: "Connecting to 192.168.1.100:502..."
   - Result: ✅ Success (12ms) or ❌ Failed (timeout)
   - Failure reasons:
     - "Host unreachable" = Wrong IP or device offline
     - "Connection refused" = Port closed or wrong port
     - "Timeout" = Network too slow or firewall blocking

2. **Modbus Protocol Test**:
   - Sends Modbus identification request
   - Verifies device speaks Modbus TCP
   - Shows device info if available
   - Common failures:
     - "Invalid response" = Not a Modbus device
     - "Wrong unit ID" = Device has different ID

3. **Register Read Test**:
   - Attempts to read first configured register
   - Verifies register addressing is correct
   - Shows actual values read
   - Failures indicate:
     - "Illegal address" = Wrong register number
     - "Device failure" = Device error condition

4. **Performance Analysis**:
   - Runs 10 rapid requests
   - Measures timing statistics:
     - Min response time: Best case
     - Max response time: Worst case
     - Average: Typical performance
     - Standard deviation: Consistency
   - Recommendations based on results

5. **Raw Data Display** (Advanced - expandable section):
   - Shows actual network packets
   - Hexadecimal and ASCII view
   - For vendor support or deep debugging
   - Example:
     ```
     Request:  00 01 00 00 00 06 01 03 00 00 00 02
     Response: 00 01 00 00 00 07 01 03 04 01 A3 02 B5
     ```

**Test Summary Box**:
- Overall result: PASS/FAIL
- Total test time
- Recommended actions if failed
- "Copy to Clipboard" for support tickets

**Device Logs View**
- Filterable log entries per device
- Log levels: Error, Warning, Info, Debug
- Search by message content
- Time range filter
- Export logs functionality
- Structured error details with:
  - Error codes and messages
  - Troubleshooting steps
  - Context data
  - Recovery suggestions

**Error Analysis Dashboard**

Aggregated view of all system errors to identify patterns and chronic issues. This helps maintenance teams focus on the most impactful problems.

**Common Errors by Device** (Bar Chart):
- Shows top 10 error types per device
- Hover for details: Error message, count, last occurrence
- Click to filter logs to specific error type
- Examples of common errors:
  - "Connection timeout" (network issues)
  - "Invalid register address" (configuration problem)
  - "CRC error" (electrical interference)
  - "Device not responding" (device powered off)

**Error Frequency Trends** (Line Graph):
- Timeline showing error rates over time
- Selectable time ranges: Hour, Day, Week, Month
- Multiple lines for different error types
- Helps identify:
  - Increasing problems (trending up)
  - Time-based patterns (errors at shift change)
  - Improvement after fixes (trending down)

**Recovery Success Rates** (Pie Charts):
- **Automatic Recovery**: What percentage self-healed
  - 85% = Recovered automatically
  - 10% = Required manual intervention
  - 5% = Still failing
- **Recovery Methods**:
  - Connection retry: 60%
  - Device reset: 25%
  - Configuration reload: 10%
  - Manual fix required: 5%

**Mean Time to Recovery (MTTR)** (Statistics):
- Average time from error to resolution
- Breakdown by error type:
  - Network errors: ~30 seconds (auto-retry)
  - Device errors: ~5 minutes (reset required)
  - Configuration errors: ~20 minutes (human fix)
- Trends over time (improving or degrading)
- Target vs Actual MTTR comparison

#### 5.7 Performance Monitoring

**Metrics Dashboard**
- Real-time performance metrics
- Counter processing rates
- Batch processing performance
- Memory usage trends
- Network latency metrics
- Data quality percentages

**Counter-Specific Metrics**

Specialized metrics focused on industrial counter operations:

**Overflow Event Tracking:**
- **What is overflow?** When a counter reaches its maximum value and resets
  - 16-bit counter: Resets after 65,535
  - 32-bit counter: Resets after 4,294,967,295
- **Metrics displayed:**
  - Total overflow events per channel
  - Time since last overflow
  - Overflow frequency (events per day)
  - Automatic handling success rate
- **Why it matters:** Ensures no counts are lost during reset

**Rate Calculation Performance:**
- **Production Rate**: Items counted per time unit
  - Counts per minute
  - Counts per hour
  - Counts per shift
- **Calculation Metrics:**
  - Average calculation time (should be <10ms)
  - Number of data points used
  - Rate stability (variation percentage)
  - Anomaly detection (sudden rate changes)

**Channel-Specific Statistics:**
Per-channel dashboard showing:
- **Current Count**: Live value from device
- **Today's Total**: Counts since midnight
- **Average Rate**: Based on last 100 readings
- **Peak Rate**: Maximum rate observed
- **Data Quality**: Percentage of good readings
- **Last Update**: Freshness of data
- **Status Indicators**:
  - 🟢 Active: Counter incrementing normally
  - 🟡 Idle: No changes detected
  - 🔴 Error: Read failures or invalid data

**Device Response Time Distribution:**
- **Histogram** showing response times:
  - <10ms: Excellent (local network)
  - 10-50ms: Good (typical)
  - 50-100ms: Acceptable
  - 100-500ms: Slow (may need optimization)
  - >500ms: Poor (investigate network)
- **Statistics:**
  - Average response time
  - 95th percentile (worst 5%)
  - Standard deviation (consistency)
- **Per-device breakdown** to identify slow devices

### 6. API Requirements

The frontend communicates with the backend through a RESTful API and WebSocket connections. All endpoints return JSON and use standard HTTP status codes.

#### Base URL Structure
- Development: `http://localhost:5000/api`
- Production: `https://your-server/api`

#### Authentication (Future)
- Bearer token in Authorization header
- Token refresh endpoint
- Role-based permissions

#### Endpoints

```typescript
// Device Management
GET    /api/devices                 // List all devices with status
GET    /api/devices/{id}           // Get device details with channels
POST   /api/devices                // Create device
PUT    /api/devices/{id}           // Update device
DELETE /api/devices/{id}           // Delete device
POST   /api/devices/{id}/test     // Test device connection
POST   /api/devices/{id}/enable   // Enable device
POST   /api/devices/{id}/disable  // Disable device

// Channel Management  
GET    /api/devices/{id}/channels  // Get device channels
PUT    /api/devices/{id}/channels  // Update all channels
PUT    /api/devices/{id}/channels/{ch} // Update single channel
POST   /api/devices/{id}/channels/bulk // Bulk channel operations

// Global Configuration
GET    /api/config                 // Get global configuration
PUT    /api/config                 // Update global configuration
GET    /api/config/validate        // Validate configuration

// Monitoring
GET    /api/devices/health         // Get all devices health
GET    /api/devices/{id}/health    // Get device health details
GET    /api/counters/current       // Get current counter values
GET    /api/system/health          // Get system health
GET    /api/metrics                // Get performance metrics
GET    /api/metrics/counters       // Get counter-specific metrics
WS     /ws/counters               // WebSocket for real-time updates
WS     /ws/health                 // WebSocket for health updates

// Testing
GET    /api/tests                  // Get available tests
POST   /api/tests/run             // Run tests by category
POST   /api/tests/run/{id}        // Run specific test
GET    /api/tests/results/{id}    // Get test results
POST   /api/tests/validate        // Validate production readiness
POST   /api/tests/report          // Generate test report

// Diagnostics
GET    /api/devices/{id}/logs      // Get device logs
POST   /api/devices/{id}/diagnose  // Run diagnostic test
GET    /api/errors                 // Get error analytics
GET    /api/errors/{id}           // Get error details

// Data Export/Import
GET    /api/config/export          // Export all configurations
POST   /api/config/import          // Import configurations
GET    /api/data/export            // Export current readings
```

### 7. UI/UX Specifications

**Important Note**: This is an industrial control system interface, not a consumer application. The design must prioritize function over form, with every element serving a specific operational purpose.

#### Design Principles
- **Information Density**: Maximum information visible without scrolling
- **Direct Manipulation**: Edit values in-place where possible
- **Clear Feedback**: Immediate validation and error messages
- **Keyboard Navigation**: Full keyboard support for efficiency
- **Consistent Layout**: Similar operations in similar locations
- **Industrial Context**: Design for 24/7 control room environment

#### Color Scheme
- Background: Neutral grays (Tailwind gray-50/gray-900 for light/dark)
- Success: Green-500
- Warning: Yellow-500  
- Error: Red-500
- Info: Blue-500
- Borders: Gray-200/Gray-700
- Data Quality Indicators:
  - Good: Green
  - Uncertain: Yellow
  - Bad: Red
  - Configuration Error: Orange

#### Component Specifications
- **Tables**: shadcn/ui DataTable with sorting, filtering, pagination
- **Forms**: shadcn/ui Form with react-hook-form
- **Buttons**: Primary/Secondary/Destructive variants
- **Status Indicators**: Badge components with semantic colors
- **Input Validation**: Real-time with debouncing
- **Toasts**: For success/error notifications
- **Dialogs**: For confirmations and detailed views
- **Command Palette**: For quick navigation (Cmd+K)

### 8. Performance Requirements

- Page load time: < 2 seconds
- API response time: < 500ms for reads, < 1s for writes
- Real-time update latency: < 100ms
- Support 50+ simultaneous device configurations
- Handle 1000+ counter updates per second
- Smooth UI with 60fps interactions

### 9. Error Handling

Comprehensive error handling designed for industrial environments where downtime is costly.

**Network Errors:**
- **Automatic Retry**: System attempts reconnection automatically
  - First retry: After 1 second
  - Second retry: After 2 seconds
  - Third retry: After 4 seconds (exponential backoff)
  - Prevents network flooding while maintaining responsiveness
- **User Notification**: Toast showing "Connection lost, retrying..."
- **Manual Override**: "Retry Now" button for immediate attempt

**Validation Errors:**
- **Real-time Validation**: Errors appear as you type
- **Field-level Messages**: Error appears directly under problem field
- **Examples**:
  - "IP address must be in format XXX.XXX.XXX.XXX"
  - "Port must be between 1 and 65535"
  - "Register count must match data type (2 for UInt32)"
- **Prevention**: Invalid inputs highlighted in red
- **Guidance**: Tooltip with correct format/range

**System Errors:**
- **Toast Notifications**: Pop-up messages in corner of screen
- **Severity Levels**:
  - ℹ️ Info: "Configuration saved successfully"
  - ⚠️ Warning: "High memory usage detected"
  - ❌ Error: "Failed to write to database"
- **Actionable Messages**: Each error includes what to do
  - Bad: "Error 500"
  - Good: "Database connection failed. Check InfluxDB is running."

**Connection Failures:**
- **Visual Status**: Device row highlighted in red
- **Status Icon**: 🔴 with hover showing last error
- **Quick Actions**:
  - "Test Connection": Run diagnostic
  - "View Logs": See detailed error history
  - "Edit Settings": Adjust timeout/retries
- **Automatic Recovery**: If enabled, shows retry countdown

**Data Conflicts:**
- **Scenario**: Two users edit same device simultaneously
- **Detection**: System tracks configuration versions
- **Resolution Options**:
  - "Use Mine": Override with your changes
  - "Use Theirs": Accept other user's changes
  - "Merge": Show differences and manually resolve
- **Prevention**: Warning when opening already-being-edited device

**Structured Error Display Format:**

Each error shows four sections (expandable):

1. **Summary** (Always visible):
   - Plain English description
   - Example: "Cannot connect to Production Line Counter"

2. **Error Classification**:
   - **Type**: Connection/Configuration/Data/System
   - **Severity**: Critical/Warning/Info
   - **Code**: For support reference (e.g., "CONN_TIMEOUT_001")
   - **Component**: What part failed (e.g., "ModbusClient")

3. **Troubleshooting Steps**:
   - Numbered checklist of things to try
   - Example for connection error:
     1. ✓ Verify device is powered on
     2. ✓ Check network cable is connected
     3. ✓ Ping device IP address (click to test)
     4. ✓ Verify firewall allows port 502
     5. ✓ Check device configuration matches

4. **Technical Details** (Collapsed by default):
   - Stack trace for developers
   - Raw error message
   - Timestamp and context
   - Device configuration at time of error
   - Network diagnostics

**Recovery Actions:**
Each error includes relevant action buttons:
- "Retry Operation"
- "Test Connection"
- "View Documentation"
- "Contact Support" (pre-fills error details)
- "Ignore and Continue" (where safe)

### 10. Data Management

**Configuration Versioning with Rollback**

Every configuration change is automatically versioned, like "track changes" in a document:

- **Version History List**: Shows all past configurations
  - Version number, date/time, who changed, what changed
  - Example: "v23 - 2024-01-15 14:30 - John - Added Line 3 Counter"
- **Compare Versions**: See differences between any two versions
  - Red = Removed, Green = Added, Yellow = Modified
- **Rollback Function**: One-click restore to any previous version
  - Confirmation dialog shows what will change
  - Creates new version (doesn't delete history)
- **Comments**: Optional note explaining why change was made

**Bulk Import/Export of Device Configurations**

- **Export Formats**:
  - **JSON**: Complete configuration with all settings
  - **CSV**: Simplified format for Excel editing
  - **ZIP**: Includes configs + documentation

- **Export Options**:
  - All devices or selected devices only
  - Include channel configurations
  - Include global settings
  - Include test results

- **Import Features**:
  - Drag-and-drop file upload
  - Preview changes before applying
  - Validation with error highlighting
  - Merge options:
    - Replace all (overwrite everything)
    - Merge (add new, update existing)
    - Add only (skip existing devices)

**Automatic Configuration Backup**

- **Scheduled Backups**: Daily/Weekly/Monthly options
- **Backup Triggers**:
  - Before major changes
  - After successful test suite
  - On demand via "Backup Now" button
- **Backup Storage**:
  - Local filesystem with retention policy
  - Network share option
  - Cloud storage integration (future)
- **Restore Interface**:
  - Browse available backups
  - Preview backup contents
  - Selective restore options

**Configuration Templates**

Pre-built configurations for common scenarios:

- **Template Library**:
  - "Single Production Line": 1 device, 3 channels
  - "Multi-Line Factory": 10 devices, standard layout
  - "Quality Station": Pass/fail counter setup
  - "High-Speed Bottling": Optimized for speed
  - "Remote Facility": Long timeouts, retry settings

- **Template Features**:
  - Preview template settings
  - Customize after applying
  - Save custom templates
  - Share templates between installations

**Change Tracking with Audit Log**

- **Audit Log Table**:
  - Timestamp of change
  - User/System that made change
  - Component changed (Device/Channel/Global)
  - Before/After values
  - Reason (if provided)

- **Log Features**:
  - Filter by date range
  - Filter by component
  - Search by value
  - Export for compliance
  - Retention settings

- **Change Notifications**:
  - Real-time alerts for critical changes
  - Email summaries (if configured)
  - Dashboard widget showing recent changes

### 11. Future Considerations

- Authentication & role-based access
- Multi-tenant support
- Device grouping and hierarchies
- Alarm configuration interface
- Integration with existing SCADA systems
- Mobile responsive design
- Offline mode with sync
- Configuration change notifications
- Advanced search and filtering
- Custom dashboard layouts

### 12. Success Metrics

These metrics determine if the frontend successfully meets industrial operational needs:

- Time to configure new device: < 2 minutes
- Mean time to identify connection issue: < 30 seconds
- User error rate in configuration: < 5%
- System uptime: 99.9%
- Page load performance: 95th percentile < 3 seconds
- Test execution time: < 5 minutes for full suite
- Configuration validation accuracy: 100%

### 13. Accessibility Requirements

- WCAG 2.1 AA compliance
- Keyboard navigation for all features
- Screen reader support
- High contrast mode
- Configurable font sizes
- Color-blind friendly palettes

### 14. Browser Support

- Chrome/Edge (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Minimum resolution: 1920x1080
- No IE11 support

---

This PRD provides comprehensive coverage of all backend functionality exposed through a professional, technician-focused interface that prioritizes direct control and complete system visibility.

## Appendix A: Glossary of Terms

- **ADAM-6000**: Series of industrial I/O modules by Advantech
- **Channel**: Individual counter input on a device (0-15)
- **Counter**: Device that counts electrical pulses
- **InfluxDB**: Time-series database for storing counter data
- **Modbus TCP**: Industrial protocol for device communication
- **Polling**: Reading data from devices at regular intervals
- **Register**: Memory location in device holding counter value
- **SCADA**: Supervisory Control and Data Acquisition system
- **Unit ID**: Unique address for Modbus devices (1-247)

## Appendix B: Typical Use Cases

1. **Production Line Monitoring**:
   - Configure device for each production line
   - Channel 0: Good products
   - Channel 1: Rejects
   - Channel 2: Total items
   - Monitor rates to ensure meeting targets

2. **Quality Gate Tracking**:
   - One channel for passed items
   - One channel for failed items
   - Calculate quality percentage
   - Alert if quality drops below threshold

3. **Multi-Site Deployment**:
   - Configure devices across network
   - Different polling rates for local vs remote
   - Aggregate data in InfluxDB
   - Company-wide dashboards in Grafana

4. **Maintenance Validation**:
   - Run test suite before shift
   - Verify all counters responding
   - Document results for compliance
   - Quick diagnostics for issues