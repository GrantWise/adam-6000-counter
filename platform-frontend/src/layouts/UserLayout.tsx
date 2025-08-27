import React, { Suspense } from 'react'
import { Routes, Route } from 'react-router-dom'
import { UserHeader } from '@/components/navigation/UserHeader'
import { UserNavigation } from '@/components/navigation/UserNavigation'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'

// Lazy-loaded user pages
const UserDashboard = React.lazy(() => import('@/pages/user/Dashboard'))
const DeviceMonitoring = React.lazy(() => import('@/pages/user/DeviceMonitoring'))
const OEEOverview = React.lazy(() => import('@/pages/user/OEEOverview'))
const ProductionSchedule = React.lazy(() => import('@/pages/user/ProductionSchedule'))

// Logger Module Components
const LoggerDashboard = React.lazy(() => import('@/modules/logger/components/LoggerDashboard'))
const DeviceManagement = React.lazy(() => import('@/modules/logger/components/DeviceManagement'))
const RealTimeMonitoring = React.lazy(() => import('@/modules/logger/components/RealTimeMonitoring'))

// OEE Module Components  
const OeeDashboard = React.lazy(() => import('@/modules/oee/components/OeeDashboard'))
const MachineDetail = React.lazy(() => import('@/modules/oee/pages/MachineDetail'))
const WorkOrderManagement = React.lazy(() => import('@/modules/oee/components/WorkOrderManagement'))
const StoppageTracking = React.lazy(() => import('@/modules/oee/components/StoppageTracking'))

/**
 * User dashboard layout for operational interface
 * Simplified navigation focused on day-to-day operations
 */
export const UserLayout: React.FC = () => {
  return (
    <div className="min-h-screen bg-background">
      {/* User Header with simplified navigation */}
      <UserHeader />
      
      {/* Quick Navigation */}
      <UserNavigation />
      
      {/* Main Content Area */}
      <main className="container mx-auto px-4 py-6">
        <Suspense 
          fallback={
            <div className="flex items-center justify-center py-12">
              <LoadingSpinner size="lg" text="Loading..." />
            </div>
          }
        >
          <Routes>
            <Route index element={<UserDashboard />} />
            <Route path="devices" element={<DeviceMonitoring />} />
            <Route path="oee" element={<OEEOverview />} />
            <Route path="schedule" element={<ProductionSchedule />} />
            
            {/* Logger Module Routes */}
            <Route path="logger" element={<LoggerDashboard />} />
            <Route path="logger/devices" element={<DeviceManagement />} />
            <Route path="logger/monitoring" element={<RealTimeMonitoring />} />
            
            {/* OEE Module Routes */}
            <Route path="oee-dashboard" element={<OeeDashboard />} />
            <Route path="oee/machine/:machineId" element={<MachineDetail />} />
            <Route path="oee/work-orders" element={<WorkOrderManagement />} />
            <Route path="oee/stoppages" element={<StoppageTracking />} />
            
            {/* Module routes for user-accessible modules */}
            <Route path="modules/*" element={<UserModuleRoutes />} />
          </Routes>
        </Suspense>
      </main>
    </div>
  )
}

// Placeholder for user-accessible module routes
const UserModuleRoutes: React.FC = () => {
  return (
    <div className="text-center py-12">
      <h2 className="text-2xl font-semibold mb-4">Module Access</h2>
      <p className="text-muted-foreground mb-6">
        You have access to the following modules based on your role and permissions.
      </p>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {/* Module cards will be dynamically generated here */}
      </div>
    </div>
  )
}