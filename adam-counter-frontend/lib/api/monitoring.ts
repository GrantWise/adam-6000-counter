import { apiClient } from './client';
import { SystemHealth, CounterMetrics, AdamDeviceHealth } from '@/lib/types/api';

export const monitoringApi = {
  // Get all devices health status
  getDevicesHealth: () => 
    apiClient.get<AdamDeviceHealth[]>('/devices/health'),

  // Get specific device health details
  getDeviceHealth: (id: string) => 
    apiClient.get<AdamDeviceHealth>(`/devices/${id}/health`),

  // Get current counter values for all devices
  getCurrentCounters: () => 
    apiClient.get<CounterMetrics[]>('/counters/current'),

  // Get system health
  getSystemHealth: () => 
    apiClient.get<SystemHealth>('/system/health'),

  // Get performance metrics
  getMetrics: () => 
    apiClient.get('/metrics'),

  // Get counter-specific metrics
  getCounterMetrics: () => 
    apiClient.get('/metrics/counters'),
};