import React from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { 
  Factory,
  Bell, 
  User, 
  LogOut, 
  Home,
  ChevronDown
} from 'lucide-react'
import { useAuth } from '@/hooks/useAuth'
import { usePlatformStore } from '@/store/platformStore'

/**
 * User dashboard header with simplified navigation
 * Optimized for operational users with clear, large controls
 */
export const UserHeader: React.FC = () => {
  const { user, logout, displayName } = useAuth()
  const { notifications, hierarchyContext } = usePlatformStore()

  const unreadNotifications = notifications.filter(n => !n.read).length

  const handleLogout = async () => {
    await logout()
  }

  return (
    <header className="bg-white border-b border-border shadow-sm">
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-between py-3">
          {/* Left Section - Logo and Hierarchy */}
          <div className="flex items-center space-x-4">
            <Link to="/dashboard" className="flex items-center space-x-3">
              <div className="w-10 h-10 bg-primary rounded-lg flex items-center justify-center">
                <Factory className="w-6 h-6 text-primary-foreground" />
              </div>
              <div>
                <h1 className="text-xl font-bold text-foreground">
                  Industrial ADAM
                </h1>
                <div className="flex items-center space-x-1 text-xs text-muted-foreground">
                  {hierarchyContext?.breadcrumbs.map((node, index) => (
                    <React.Fragment key={node.id}>
                      {index > 0 && <span>›</span>}
                      <span>{node.name}</span>
                    </React.Fragment>
                  ))}
                </div>
              </div>
            </Link>
          </div>

          {/* Center Section - Quick Status */}
          <div className="hidden md:flex items-center space-x-6">
            <div className="text-center">
              <p className="text-xs text-muted-foreground">Current Shift</p>
              <p className="text-sm font-semibold">Day Shift</p>
            </div>
            <div className="text-center">
              <p className="text-xs text-muted-foreground">Time</p>
              <p className="text-sm font-semibold">
                {new Date().toLocaleTimeString([], { 
                  hour: '2-digit', 
                  minute: '2-digit' 
                })}
              </p>
            </div>
          </div>

          {/* Right Section - User Controls */}
          <div className="flex items-center space-x-3">
            {/* Notifications */}
            <Button variant="ghost" size="touch-sm" className="relative">
              <Bell className="h-5 w-5" />
              {unreadNotifications > 0 && (
                <Badge 
                  variant="error" 
                  size="sm"
                  className="absolute -top-1 -right-1 h-5 w-5 p-0 text-xs"
                >
                  {unreadNotifications > 9 ? '9+' : unreadNotifications}
                </Badge>
              )}
            </Button>

            {/* User Menu */}
            <div className="flex items-center space-x-2 pl-3 border-l">
              <div className="hidden sm:block text-right">
                <p className="text-sm font-medium">{displayName}</p>
                <p className="text-xs text-muted-foreground">{user?.role}</p>
              </div>
              
              <div className="w-10 h-10 bg-primary/10 rounded-full flex items-center justify-center">
                <User className="w-5 h-5 text-primary" />
              </div>
              
              <Button 
                variant="ghost" 
                size="touch-sm"
                onClick={handleLogout}
                title="Sign Out"
                className="text-muted-foreground hover:text-foreground"
              >
                <LogOut className="h-5 w-5" />
              </Button>
            </div>
          </div>
        </div>
      </div>
    </header>
  )
}