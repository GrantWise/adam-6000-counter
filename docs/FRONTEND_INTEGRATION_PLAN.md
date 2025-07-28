# Frontend Integration Plan for ADAM Counter Service

## Overview
Transform the provided frontend from a mock-data prototype into a fully integrated industrial counter management system that connects to your backend service via REST API and real-time WebSocket connections.

## Phase 1: Create Backend Web API Project

### 1.1 Add new ASP.NET Core Web API project
- Create `Industrial.Adam.Logger.WebApi` project
- Add references to `Industrial.Adam.Logger` library
- Configure Kestrel for HTTP endpoints
- Set up dependency injection for existing services

### 1.2 Implement REST API Controllers

#### DeviceController
```csharp
[ApiController]
[Route("api/devices")]
public class DeviceController : ControllerBase
{
    GET    /api/devices                 // List all devices with status
    GET    /api/devices/{id}           // Get device details with channels
    POST   /api/devices                // Create device
    PUT    /api/devices/{id}           // Update device
    DELETE /api/devices/{id}           // Delete device
    POST   /api/devices/{id}/test     // Test device connection
    POST   /api/devices/{id}/enable   // Enable device
    POST   /api/devices/{id}/disable  // Disable device
}
```

#### ConfigurationController
```csharp
[ApiController]
[Route("api/config")]
public class ConfigurationController : ControllerBase
{
    GET    /api/config                 // Get global configuration
    PUT    /api/config                 // Update global configuration
    GET    /api/config/validate        // Validate configuration
    GET    /api/config/export          // Export all configurations
    POST   /api/config/import          // Import configurations
}
```

#### MonitoringController
```csharp
[ApiController]
[Route("api")]
public class MonitoringController : ControllerBase
{
    GET    /api/devices/health         // Get all devices health
    GET    /api/devices/{id}/health    // Get device health details
    GET    /api/counters/current       // Get current counter values
    GET    /api/system/health          // Get system health
    GET    /api/metrics                // Get performance metrics
    GET    /api/metrics/counters       // Get counter-specific metrics
}
```

#### TestingController
```csharp
[ApiController]
[Route("api/tests")]
public class TestingController : ControllerBase
{
    GET    /api/tests                  // Get available tests
    POST   /api/tests/run             // Run tests by category
    POST   /api/tests/run/{id}        // Run specific test
    GET    /api/tests/results/{id}    // Get test results
    POST   /api/tests/validate        // Validate production readiness
    POST   /api/tests/report          // Generate test report
}
```

#### DiagnosticsController
```csharp
[ApiController]
[Route("api")]
public class DiagnosticsController : ControllerBase
{
    GET    /api/devices/{id}/logs      // Get device logs
    POST   /api/devices/{id}/diagnose  // Run diagnostic test
    GET    /api/errors                 // Get error analytics
    GET    /api/errors/{id}           // Get error details
}
```

### 1.3 Add SignalR Hubs
```csharp
public class CounterDataHub : Hub
{
    // Real-time counter value updates
    public async Task SendCounterUpdate(AdamDataReading reading)
    public async Task SubscribeToDevice(string deviceId)
    public async Task UnsubscribeFromDevice(string deviceId)
}

public class HealthStatusHub : Hub
{
    // Real-time health status updates
    public async Task SendHealthUpdate(AdamDeviceHealth health)
    public async Task SubscribeToHealth()
}
```

## Phase 2: Frontend API Integration Layer

### 2.1 Create API Service Architecture
```
adam-counter-frontend/
└── lib/
    └── api/
        ├── client.ts        # Axios instance with interceptors
        ├── devices.ts       # Device management API
        ├── config.ts        # Configuration API
        ├── monitoring.ts    # Monitoring data API
        ├── testing.ts       # Test execution API
        ├── diagnostics.ts   # Diagnostics API
        └── websocket.ts     # SignalR client setup
```

### 2.2 Axios Client Configuration
```typescript
// lib/api/client.ts
import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request/response interceptors for error handling
apiClient.interceptors.response.use(
  response => response,
  error => {
    // Handle common errors (401, 403, 500, etc.)
    return Promise.reject(error);
  }
);
```

### 2.3 Device API Service
```typescript
// lib/api/devices.ts
import { apiClient } from './client';
import { Device, DeviceConfig } from '@/types';

export const deviceApi = {
  getAll: () => apiClient.get<Device[]>('/devices'),
  getById: (id: string) => apiClient.get<Device>(`/devices/${id}`),
  create: (device: DeviceConfig) => apiClient.post<Device>('/devices', device),
  update: (id: string, device: DeviceConfig) => apiClient.put<Device>(`/devices/${id}`, device),
  delete: (id: string) => apiClient.delete(`/devices/${id}`),
  test: (id: string) => apiClient.post(`/devices/${id}/test`),
  enable: (id: string) => apiClient.post(`/devices/${id}/enable`),
  disable: (id: string) => apiClient.post(`/devices/${id}/disable`),
};
```

### 2.4 React Query Integration
```typescript
// hooks/useDevices.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { deviceApi } from '@/lib/api/devices';

export const useDevices = () => {
  return useQuery({
    queryKey: ['devices'],
    queryFn: () => deviceApi.getAll(),
    refetchInterval: 5000, // Refresh every 5 seconds
  });
};

export const useCreateDevice = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: deviceApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
    },
  });
};
```

### 2.5 SignalR Integration
```typescript
// lib/api/websocket.ts
import * as signalR from '@microsoft/signalr';

export class CounterDataService {
  private connection: signalR.HubConnection;
  
  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/counter-data`)
      .withAutomaticReconnect()
      .build();
  }
  
  async start() {
    await this.connection.start();
  }
  
  onCounterUpdate(callback: (data: AdamDataReading) => void) {
    this.connection.on('CounterUpdate', callback);
  }
  
  async subscribeToDevice(deviceId: string) {
    await this.connection.invoke('SubscribeToDevice', deviceId);
  }
}
```

## Phase 3: State Management Implementation

### 3.1 Zustand Store Structure
```typescript
// stores/deviceStore.ts
import { create } from 'zustand';

interface DeviceStore {
  devices: Device[];
  selectedDevice: Device | null;
  setDevices: (devices: Device[]) => void;
  selectDevice: (device: Device | null) => void;
  updateDevice: (id: string, updates: Partial<Device>) => void;
}

export const useDeviceStore = create<DeviceStore>((set) => ({
  devices: [],
  selectedDevice: null,
  setDevices: (devices) => set({ devices }),
  selectDevice: (device) => set({ selectedDevice: device }),
  updateDevice: (id, updates) => set((state) => ({
    devices: state.devices.map(d => d.id === id ? { ...d, ...updates } : d)
  })),
}));
```

### 3.2 Real-time Data Store
```typescript
// stores/realtimeStore.ts
interface RealtimeStore {
  counterReadings: Map<string, AdamDataReading>;
  deviceHealth: Map<string, AdamDeviceHealth>;
  updateReading: (reading: AdamDataReading) => void;
  updateHealth: (health: AdamDeviceHealth) => void;
}
```

## Phase 4: Missing Feature Implementation

### 4.1 Configuration Import/Export
- Add file upload component with drag-and-drop
- Support JSON and CSV formats
- Validate configuration before import
- Show preview of changes

### 4.2 Audit Logging View
- Create audit log table component
- Add filtering by date, user, action type
- Implement pagination for large datasets
- Export functionality for compliance

### 4.3 Error Analysis Dashboard
- Implement charts using Recharts
- Show error frequency over time
- Display recovery success rates
- Identify chronic issues

### 4.4 Production Readiness Score
- Visual gauge component (0-100)
- Breakdown by category
- List of critical issues
- Actionable recommendations

## Phase 5: Error Handling & Recovery

### 5.1 Network Error Handling
```typescript
// lib/api/retry.ts
export const retryWithBackoff = async (
  fn: () => Promise<any>,
  maxRetries = 3,
  baseDelay = 1000
) => {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await fn();
    } catch (error) {
      if (i === maxRetries - 1) throw error;
      await new Promise(resolve => setTimeout(resolve, baseDelay * Math.pow(2, i)));
    }
  }
};
```

### 5.2 Offline Mode Detection
```typescript
// hooks/useOnlineStatus.ts
export const useOnlineStatus = () => {
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  
  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);
    
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);
  
  return isOnline;
};
```

## Phase 6: Testing & Validation

### 6.1 Unit Tests
```typescript
// __tests__/api/devices.test.ts
import { renderHook, waitFor } from '@testing-library/react';
import { useDevices } from '@/hooks/useDevices';

describe('Device API', () => {
  it('fetches devices successfully', async () => {
    const { result } = renderHook(() => useDevices());
    
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
      expect(result.current.data).toHaveLength(3);
    });
  });
});
```

### 6.2 Integration Tests
```typescript
// e2e/device-management.spec.ts
import { test, expect } from '@playwright/test';

test('create new device', async ({ page }) => {
  await page.goto('/');
  await page.click('text=Device Management');
  await page.click('text=Add Device');
  
  await page.fill('input[name="deviceId"]', 'TEST_DEVICE_001');
  await page.fill('input[name="ipAddress"]', '192.168.1.200');
  
  await page.click('text=Save');
  
  await expect(page.locator('text=TEST_DEVICE_001')).toBeVisible();
});
```

## Implementation Timeline

### Week 1-2: Backend API
- Set up WebAPI project
- Implement all controllers
- Add SignalR hubs
- Write API tests

### Week 3-4: Frontend Integration
- Create API service layer
- Add React Query
- Implement SignalR client
- Replace mock data

### Week 5-6: State Management & Features
- Set up Zustand stores
- Implement missing features
- Add error handling
- Create offline support

### Week 7-8: Testing & Polish
- Write comprehensive tests
- Performance optimization
- Documentation
- Deployment preparation

## Key Dependencies to Add

```json
{
  "dependencies": {
    "axios": "^1.6.0",
    "@tanstack/react-query": "^5.0.0",
    "@microsoft/signalr": "^8.0.0",
    "zustand": "^4.4.0",
    "recharts": "^2.10.0"
  },
  "devDependencies": {
    "msw": "^2.0.0",
    "@playwright/test": "^1.40.0",
    "@testing-library/react": "^14.0.0",
    "@testing-library/jest-dom": "^6.0.0"
  }
}
```

## Success Criteria
1. All mock data replaced with live API calls
2. Real-time updates working via SignalR
3. All PRD features implemented
4. 90%+ test coverage
5. Performance targets met (< 100ms update latency)
6. Production deployment ready

This plan provides a comprehensive roadmap for transforming the mock frontend into a production-ready industrial control system interface.