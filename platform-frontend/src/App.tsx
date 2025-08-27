import React, { useEffect } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'sonner'

// Core components
import { LoginForm } from '@/components/auth/LoginForm'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import { ErrorBoundary } from '@/components/shared/ErrorBoundary'
import TestCfrDemo from '@/pages/TestCfrDemo'

// Layout components (to be created)
import { AdminLayout } from '@/layouts/AdminLayout'
import { UserLayout } from '@/layouts/UserLayout'

// Services
import { authService } from '@/lib/services/authService'

// Store
import { usePlatformStore } from '@/store/platformStore'

// Hooks
import { useAuth } from '@/hooks/useAuth'

/**
 * Main Application Component
 * Handles authentication, routing, and global error boundaries
 */
function App() {
  const { isAuthenticated, loading, user } = useAuth()
  const { preferences } = usePlatformStore()

  useEffect(() => {
    // Initialize authentication state on app startup
    authService.initializeAuth()
  }, [])

  // Show loading spinner during authentication check
  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  return (
    <ErrorBoundary>
      <div className="min-h-screen bg-background">
        <Routes>
          {/* Public routes */}
          <Route 
            path="/login" 
            element={
              isAuthenticated ? 
                <Navigate to={getDefaultRoute()} replace /> : 
                <LoginForm />
            } 
          />
          
          {/* Test route for CFR compliance demo */}
          <Route path="/test-cfr-demo" element={<TestCfrDemo />} />
          
          {/* Protected routes */}
          {isAuthenticated ? (
            <>
              {/* Admin Dashboard Routes */}
              {(user?.role === 'Admin' || user?.role === 'SystemAdmin') && (
                <Route path="/admin/*" element={<AdminLayout />} />
              )}
              
              {/* User Dashboard Routes */}
              <Route path="/dashboard/*" element={<UserLayout />} />
              
              {/* Default redirect */}
              <Route 
                path="/" 
                element={<Navigate to={getDefaultRoute()} replace />} 
              />
              
              {/* Catch-all for authenticated users */}
              <Route 
                path="*" 
                element={<Navigate to={getDefaultRoute()} replace />} 
              />
            </>
          ) : (
            /* Unauthenticated users redirect to login */
            <Route path="*" element={<Navigate to="/login" replace />} />
          )}
        </Routes>

        {/* Global toast notifications */}
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 5000,
            style: {
              background: 'hsl(var(--background))',
              color: 'hsl(var(--foreground))',
              border: '1px solid hsl(var(--border))',
            },
          }}
        />
      </div>
    </ErrorBoundary>
  )

  /**
   * Get the default route based on user role and preferences
   */
  function getDefaultRoute(): string {
    if (!user) return '/login'
    
    // Use user preference if available
    if (preferences?.defaultView) {
      return preferences.defaultView
    }
    
    // Fallback to role-based default
    switch (user.role) {
      case 'SystemAdmin':
      case 'Admin':
        return '/admin/dashboard'
      case 'Supervisor':
      case 'Operator':
      default:
        return '/dashboard'
    }
  }
}

export default App