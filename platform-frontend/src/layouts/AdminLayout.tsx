import React, { Suspense } from 'react'
import { Routes, Route } from 'react-router-dom'
import { AdminSidebar } from '@/components/navigation/AdminSidebar'
import { AdminHeader } from '@/components/navigation/AdminHeader'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'

// Lazy-loaded admin pages
const AdminDashboard = React.lazy(() => import('@/pages/admin/Dashboard'))
const UserManagement = React.lazy(() => import('@/pages/admin/UserManagement'))
const HierarchyManagement = React.lazy(() => import('@/pages/admin/HierarchyManagement'))
const SystemHealth = React.lazy(() => import('@/pages/admin/SystemHealth'))
const SecurityAudit = React.lazy(() => import('@/pages/admin/SecurityAudit'))
const SystemConfiguration = React.lazy(() => import('@/pages/admin/SystemConfiguration'))

/**
 * Admin dashboard layout with full system access
 * Includes comprehensive navigation and system management tools
 */
export const AdminLayout: React.FC = () => {
  return (
    <div className="min-h-screen bg-background">
      {/* Admin Header */}
      <AdminHeader />
      
      <div className="flex">
        {/* Admin Sidebar */}
        <AdminSidebar />
        
        {/* Main Content Area */}
        <main className="flex-1 p-6">
          <Suspense 
            fallback={
              <div className="flex items-center justify-center py-12">
                <LoadingSpinner size="lg" text="Loading..." />
              </div>
            }
          >
            <Routes>
              <Route index element={<AdminDashboard />} />
              <Route path="dashboard" element={<AdminDashboard />} />
              <Route path="users" element={<UserManagement />} />
              <Route path="hierarchy" element={<HierarchyManagement />} />
              <Route path="health" element={<SystemHealth />} />
              <Route path="security" element={<SecurityAudit />} />
              <Route path="configuration" element={<SystemConfiguration />} />
              
              {/* Module routes will be dynamically added here */}
              <Route path="modules/*" element={<ModuleRoutes />} />
            </Routes>
          </Suspense>
        </main>
      </div>
    </div>
  )
}

// Placeholder for dynamic module routes
const ModuleRoutes: React.FC = () => {
  return (
    <div className="text-center py-12">
      <h2 className="text-2xl font-semibold mb-4">Module Routes</h2>
      <p className="text-muted-foreground">
        Dynamic module routes will be rendered here based on registered modules.
      </p>
    </div>
  )
}