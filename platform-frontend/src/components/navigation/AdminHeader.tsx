import React from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { 
  Settings, 
  Bell, 
  User, 
  LogOut, 
  Shield, 
  Menu,
  Activity
} from 'lucide-react'
import { useAuth } from '@/hooks/useAuth'
import { usePlatformStore } from '@/store/platformStore'

/**
 * Admin dashboard header with system status and user controls
 */
export const AdminHeader: React.FC = () => {
  const { user, logout, displayName } = useAuth()
  const { 
    notifications, 
    connectionStatus,
    setSidebarCollapsed,
    app: { sidebarCollapsed }
  } = usePlatformStore()

  const unreadNotifications = (notifications || []).filter(n => !n.read).length
  const systemHealthy = Object.values(connectionStatus || {}).every(
    status => status.connected
  )

  const handleLogout = async () => {
    await logout()
  }

  const toggleSidebar = () => {
    setSidebarCollapsed(!sidebarCollapsed)
  }

  return (
    <header className="bg-white border-b border-border shadow-sm">
      <div className="flex items-center justify-between px-6 py-3">
        {/* Left Section */}
        <div className="flex items-center space-x-4">
          {/* Sidebar Toggle */}
          <Button
            variant="ghost"
            size="icon"
            onClick={toggleSidebar}
            className="lg:hidden"
          >
            <Menu className="h-5 w-5" />
          </Button>

          {/* Logo and Title */}
          <Link to="/admin" className="flex items-center space-x-3">
            <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center">
              <Shield className="w-5 h-5 text-primary-foreground" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-foreground">
                Industrial ADAM
              </h1>
              <p className="text-xs text-muted-foreground">
                Administration Console
              </p>
            </div>
          </Link>
        </div>

        {/* Center Section - System Status */}
        <div className="hidden md:flex items-center space-x-6">
          <StatusIndicator
            status={systemHealthy ? 'healthy' : 'error'}
            label="System Status"
            description={systemHealthy ? 'All systems operational' : 'Issues detected'}
          />
          
          <div className="flex items-center space-x-2">
            <Activity className="w-4 h-4 text-muted-foreground" />
            <span className="text-sm text-muted-foreground">
              Last updated: {new Date().toLocaleTimeString()}
            </span>
          </div>
        </div>

        {/* Right Section */}
        <div className="flex items-center space-x-3">
          {/* Notifications */}
          <Button variant="ghost" size="icon" className="relative">
            <Bell className="h-5 w-5" />
            {unreadNotifications > 0 && (
              <Badge 
                variant="error" 
                className="absolute -top-1 -right-1 h-5 w-5 p-0 text-xs"
              >
                {unreadNotifications > 9 ? '9+' : unreadNotifications}
              </Badge>
            )}
          </Button>

          {/* Settings */}
          <Button variant="ghost" size="icon">
            <Settings className="h-5 w-5" />
          </Button>

          {/* User Menu */}
          <div className="flex items-center space-x-3 pl-3 border-l">
            <div className="hidden sm:block text-right">
              <p className="text-sm font-medium">{displayName}</p>
              <p className="text-xs text-muted-foreground">{user?.role}</p>
            </div>
            
            <div className="w-8 h-8 bg-primary/10 rounded-full flex items-center justify-center">
              <User className="w-4 h-4 text-primary" />
            </div>
            
            <Button 
              variant="ghost" 
              size="icon"
              onClick={handleLogout}
              title="Sign Out"
            >
              <LogOut className="h-5 w-5" />
            </Button>
          </div>
        </div>
      </div>
    </header>
  )
}