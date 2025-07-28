import { apiClient, retryWithBackoff } from './client';
import { DeviceWithStatus, AdamDeviceConfig, ConnectionTestResult, ChannelConfig } from '@/lib/types/api';

export const deviceApi = {
  // Get all devices with their status
  getAll: () => 
    retryWithBackoff(() => apiClient.get<DeviceWithStatus[]>('/devices')),

  // Get a specific device by ID
  getById: (id: string) => 
    retryWithBackoff(() => apiClient.get<DeviceWithStatus>(`/devices/${id}`)),

  // Create a new device
  create: (device: AdamDeviceConfig) => 
    apiClient.post<DeviceWithStatus>('/devices', device),

  // Update an existing device
  update: (id: string, device: AdamDeviceConfig) => 
    apiClient.put<DeviceWithStatus>(`/devices/${id}`, device),

  // Delete a device
  delete: (id: string) => 
    apiClient.delete(`/devices/${id}`),

  // Test device connection
  test: (id: string) => 
    apiClient.post<ConnectionTestResult>(`/devices/${id}/test`),

  // Enable a device
  enable: (id: string) => 
    apiClient.post(`/devices/${id}/enable`),

  // Disable a device  
  disable: (id: string) => 
    apiClient.post(`/devices/${id}/disable`),

  // Get channels for a device
  getChannels: (id: string) =>
    apiClient.get<ChannelConfig[]>(`/devices/${id}/channels`),

  // Update all channels for a device
  updateChannels: (id: string, channels: ChannelConfig[]) =>
    apiClient.put<DeviceWithStatus>(`/devices/${id}/channels`, channels),

  // Update a single channel
  updateChannel: (deviceId: string, channelId: number, channel: ChannelConfig) =>
    apiClient.put<DeviceWithStatus>(`/devices/${deviceId}/channels/${channelId}`, channel),
};