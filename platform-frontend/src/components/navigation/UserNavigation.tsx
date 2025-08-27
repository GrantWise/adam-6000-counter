import React from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { 
  LayoutDashboard,
  Database,
  BarChart3,
  Calendar,
  Settings,
  Activity,
  Cpu,
  Target
} from 'lucide-react'
import { useAuth } from '@/hooks/useAuth'

interface NavItem {
  id: string
  title: string
  path: string
  icon: React.ComponentType<{ className?: string }>
  permission?: string
  badge?: string
  description: string
}

/**
 * User dashboard navigation with large, touch-friendly module access
 */
export const UserNavigation: React.FC = () => {
  const location = useLocation()
  const { hasPermission } = useAuth()

  const navItems: NavItem[] = [
    {
      id: 'dashboard',
      title: 'Dashboard',
      path: '/dashboard',
      icon: LayoutDashboard,
      description: 'Overview and key metrics'
    },
    {
      id: 'logger',
      title: 'Logger Module',
      path: '/dashboard/logger',
      icon: Cpu,
      permission: 'VIEW_COUNTERS',
      description: 'ADAM device monitoring and counters'
    },
    {
      id: 'oee-dashboard',
      title: 'OEE Module',
      path: '/dashboard/oee-dashboard',
      icon: Target,
      permission: 'VIEW_OEE',
      description: 'Overall Equipment Effectiveness'
    },
    {
      id: 'devices',
      title: 'Device Management',
      path: '/dashboard/logger/devices',
      icon: Database,
      permission: 'CONFIGURE_DEVICES',
      description: 'Device configuration and setup'
    },
    {
      id: 'work-orders',
      title: 'Work Orders',
      path: '/dashboard/oee/work-orders',
      icon: Calendar,
      permission: 'MANAGE_WORK_ORDERS',
      description: 'Production work order management'
    }
  ]

  const isActive = (path: string) => {
    if (path === '/dashboard') {
      return location.pathname === path
    }
    return location.pathname.startsWith(path)
  }

  const canViewItem = (item: NavItem) => {
    return !item.permission || hasPermission(item.permission as any)
  }

  const visibleItems = navItems.filter(canViewItem)

  return (
    <nav className="bg-card border-b border-border">
      <div className="container mx-auto px-4">
        <div className="flex items-center space-x-1 overflow-x-auto py-2">
          {visibleItems.map(item => (
            <Link
              key={item.id}
              to={item.path}
              className={cn(
                "flex flex-col items-center min-w-0 px-4 py-3 rounded-lg transition-all duration-200",
                "hover:bg-accent/50 active:scale-95",
                {
                  "bg-primary text-primary-foreground shadow-sm": isActive(item.path),
                  "text-muted-foreground hover:text-foreground": !isActive(item.path)
                }
              )}
            >
              <div className="flex items-center space-x-2 mb-1">
                <item.icon className={cn("h-5 w-5", {
                  "text-primary-foreground": isActive(item.path),
                  "text-muted-foreground": !isActive(item.path)
                })} />
                {item.badge && (
                  <Badge variant="warning" size="sm">
                    {item.badge}
                  </Badge>
                )}
              </div>
              <span className={cn("text-sm font-medium text-center", {
                "text-primary-foreground": isActive(item.path),
                "text-muted-foreground": !isActive(item.path)
              })}>
                {item.title}
              </span>
              <span className={cn("text-xs text-center mt-1 hidden sm:block", {
                "text-primary-foreground/80": isActive(item.path),
                "text-muted-foreground/80": !isActive(item.path)
              })}>
                {item.description}
              </span>
            </Link>
          ))}
          
          {/* Settings/Profile link */}
          <div className="ml-auto">
            <Button
              variant="ghost"
              size="touch-sm"
              className="flex items-center space-x-2"
            >
              <Settings className="h-5 w-5" />
              <span className="hidden sm:inline">Settings</span>
            </Button>
          </div>
        </div>
      </div>
    </nav>
  )
}