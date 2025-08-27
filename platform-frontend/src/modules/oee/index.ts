/**
 * OEE Module Exports
 * Industrial ADAM Overall Equipment Effectiveness - Core Business Module
 */

// Components
export { default as OeeDashboard } from './components/OeeDashboard'
export { default as EquipmentOeeDetail } from './components/EquipmentOeeDetail'
export { default as WorkOrderManagement } from './components/WorkOrderManagement'
export { default as WorkOrderDetail } from './components/WorkOrderDetail'
export { default as StoppageTracking } from './components/StoppageTracking'
export { default as OeeAnalytics } from './components/OeeAnalytics'

// Sub-components
export { OeeMetricCard } from './components/OeeMetricCard'
export { StoppageTrackingWidget } from './components/StoppageTrackingWidget'
export { WorkOrderProgressCard } from './components/WorkOrderProgressCard'
export { OeeTrendChart } from './components/OeeTrendChart'
export { OeeReportExporter } from './components/OeeReportExporter'

// Hooks
export { useOeeData } from './hooks/useOeeData'
export { useOeeRealTime } from './hooks/useOeeRealTime'

// Types
export type {
  OeeData,
  WorkOrderData,
  StoppageData,
  OeeLossAnalysis,
  OeeLevel
} from './types'

// Module configuration
export const oeeModule = {
  id: 'oee',
  name: 'OEE Module',
  description: 'Overall Equipment Effectiveness Monitoring and Analysis',
  version: '1.0.0',
  basePath: '/oee',
  permissions: ['VIEW_OEE', 'MANAGE_WORK_ORDERS', 'VIEW_STOPPAGES'],
  dependencies: ['platform-foundation', 'logger'],
  routes: [
    {
      path: '/oee/dashboard',
      component: 'OeeDashboard',
      permissions: ['VIEW_OEE'],
      title: 'OEE Dashboard'
    },
    {
      path: '/oee/equipment/:id',
      component: 'EquipmentOeeDetail',
      permissions: ['VIEW_OEE_DETAILS'],
      title: 'Equipment OEE Analysis'
    },
    {
      path: '/oee/work-orders',
      component: 'WorkOrderManagement',
      permissions: ['VIEW_WORK_ORDERS'],
      title: 'Work Order Management'
    },
    {
      path: '/oee/work-orders/:id',
      component: 'WorkOrderDetail',
      permissions: ['VIEW_WORK_ORDER_DETAILS'],
      title: 'Work Order Details'
    },
    {
      path: '/oee/stoppages',
      component: 'StoppageTracking',
      permissions: ['VIEW_STOPPAGES'],
      title: 'Stoppage Tracking'
    },
    {
      path: '/oee/analytics',
      component: 'OeeAnalytics',
      permissions: ['VIEW_OEE_ANALYTICS'],
      title: 'OEE Analytics & Trends'
    }
  ]
} as const