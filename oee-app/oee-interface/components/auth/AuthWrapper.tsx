"use client"

import { useAuth } from '@/lib/auth/AuthContext'
import { LoginForm } from './LoginForm'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'

interface AuthWrapperProps {
  children: React.ReactNode
}

export function AuthWrapper({ children }: AuthWrapperProps) {
  const { user, isLoading, error, login, logout } = useAuth()

  // Show loading spinner while checking authentication
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="text-lg font-medium text-gray-900">Loading...</div>
          <div className="text-sm text-gray-500 mt-2">Checking authentication</div>
        </div>
      </div>
    )
  }

  // Show login form if not authenticated
  if (!user) {
    return (
      <LoginForm
        onLogin={login}
        isLoading={isLoading}
        error={error || undefined}
      />
    )
  }

  // Show main app with user info header
  return (
    <div className="min-h-screen bg-gray-50">
      {/* User info header */}
      <div className="bg-white border-b px-4 py-2 flex justify-between items-center">
        <div className="flex items-center space-x-4">
          <div className="text-sm">
            <span className="font-medium">{user.username}</span>
            <Badge className="ml-2" variant="secondary">
              {user.role.toUpperCase()}
            </Badge>
          </div>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={logout}
          disabled={isLoading}
        >
          {isLoading ? 'Logging out...' : 'Logout'}
        </Button>
      </div>
      
      {/* Main app content */}
      <div className="h-[calc(100vh-60px)]">
        {children}
      </div>
    </div>
  )
}