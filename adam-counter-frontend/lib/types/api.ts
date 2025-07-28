// API Types matching the backend models exactly

export interface AdamDeviceConfig {
  deviceId: string;
  ipAddress: string;
  port: number;
  unitId: number;
  description?: string;
  enabled: boolean;
  pollInterval: number;
  timeout: number;
  retryCount: number;
  channels: ChannelConfig[];
}

export interface ChannelConfig {
  channelNumber: number;
  name: string;
  enabled: boolean;
  scalingFactor: number;
  offset: number;
  unit?: string;
  alarmLowThreshold?: number;
  alarmHighThreshold?: number;
  rateCalculationEnabled: boolean;
  rateWindowSeconds: number;
}

export interface AdamDeviceHealth {
  deviceId: string;
  timestamp: string;
  status: DeviceStatus;
  isConnected: boolean;
  lastSuccessfulRead?: string;
  consecutiveFailures: number;
  communicationLatency?: number;
  lastError?: string;
  totalReads: number;
  successfulReads: number;
  successRate: number;
}

export enum DeviceStatus {
  Online = 0,
  Warning = 1,
  Error = 2,
  Offline = 3,
  Unknown = 4
}

export interface DeviceWithStatus {
  config: AdamDeviceConfig;
  health?: AdamDeviceHealth;
  status: string;
}

export interface ConnectionTestResult {
  success: boolean;
  message: string;
  responseTime?: number;
  details?: string;
}

export interface AdamDataReading {
  deviceId: string;
  channel: number;
  rawValue: number;
  timestamp: string;
  processedValue?: number;
  rate?: number;
  quality: DataQuality;
  unit?: string;
  acquisitionTime: string;
  tags: Record<string, any>;
  errorMessage?: string;
}

export enum DataQuality {
  Good = 0,
  Uncertain = 1,
  Bad = 2,
  Timeout = 3,
  DeviceFailure = 4,
  ConfigurationError = 5,
  Overflow = 6,
  Unknown = 7
}

export interface SystemHealth {
  status: string;
  uptime: string;
  connectedDevices: number;
  totalDevices: number;
  influxDbConnected: boolean;
  memoryUsage: number;
  cpuUsage: number;
}

export interface CounterMetrics {
  deviceId: string;
  channelNumber: number;
  currentValue: number;
  rate: number;
  unit?: string;
  timestamp: string;
  quality: DataQuality;
}