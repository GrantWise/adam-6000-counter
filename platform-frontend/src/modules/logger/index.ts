/**
 * Logger Module Exports
 * Industrial ADAM Counter Logger - Device Management and Counter Visualization
 */

// Components
export { default as LoggerDashboard } from './components/LoggerDashboard'
export { default as DeviceManagement } from './components/DeviceManagement'
export { default as DeviceDetail } from './components/DeviceDetail'
export { default as RealTimeMonitoring } from './components/RealTimeMonitoring'
export { default as HistoricalAnalytics } from './components/HistoricalAnalytics'

// Sub-components
export { DeviceStatusCard } from './components/DeviceStatusCard'
export { CounterDisplayWidget } from './components/CounterDisplayWidget'
export { DeviceConfigurationModal } from './components/DeviceConfigurationModal'
export { HistoricalCounterChart } from './components/HistoricalCounterChart'

// Hooks
export { useDeviceData } from './hooks/useDeviceData'
export { useRealTimeCounters } from './hooks/useRealTimeCounters'
export { useDeviceHealth } from './hooks/useDeviceHealth'

// Types
export type {
  DeviceData,
  CounterData,
  DeviceHealth,
  CounterUpdate
} from './types'

// Module configuration
export const loggerModule = {
  id: 'logger',
  name: 'Logger Module',
  description: 'ADAM Device Management and Counter Data Monitoring',
  version: '1.0.0',
  basePath: '/logger',
  permissions: ['VIEW_COUNTERS', 'CONFIGURE_DEVICES', 'VIEW_DEVICE_STATUS'],
  dependencies: ['platform-foundation'],
  routes: [
    {
      path: '/logger/dashboard',
      component: 'LoggerDashboard',
      permissions: ['VIEW_COUNTERS'],
      title: 'Logger Dashboard'
    },
    {
      path: '/logger/devices',
      component: 'DeviceManagement', 
      permissions: ['VIEW_DEVICES'],
      title: 'Device Management'
    },
    {
      path: '/logger/devices/:id',
      component: 'DeviceDetail',
      permissions: ['VIEW_DEVICE_DETAILS'],
      title: 'Device Details'
    },
    {
      path: '/logger/monitoring',
      component: 'RealTimeMonitoring',
      permissions: ['VIEW_COUNTERS'],
      title: 'Real-Time Monitoring'
    },
    {
      path: '/logger/analytics',
      component: 'HistoricalAnalytics',
      permissions: ['VIEW_ANALYTICS'],
      title: 'Historical Analytics'
    }
  ]
} as const