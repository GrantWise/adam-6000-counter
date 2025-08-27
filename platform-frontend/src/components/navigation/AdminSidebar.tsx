import React from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { 
  LayoutDashboard,
  Users,
  TreePine,
  Shield,
  Activity,
  Settings,
  Database,
  BarChart3,
  Calendar,
  AlertTriangle,
  ChevronDown,
  ChevronRight
} from 'lucide-react'
import { useAuth } from '@/hooks/useAuth'
import { usePlatformStore } from '@/store/platformStore'

interface SidebarItem {
  id: string
  title: string
  icon: React.ComponentType<{ className?: string }>
  path: string
  permission?: string
  badge?: string
  children?: SidebarItem[]
}

/**
 * Admin dashboard sidebar with full system navigation
 */
export const AdminSidebar: React.FC = () => {
  const location = useLocation()
  const { hasPermission } = useAuth()
  const { 
    app: { sidebarCollapsed },
    moduleRegistry
  } = usePlatformStore()

  const [expandedSections, setExpandedSections] = React.useState<Set<string>>(
    new Set(['system', 'modules'])
  )

  const toggleSection = (sectionId: string) => {
    const newExpanded = new Set(expandedSections)
    if (newExpanded.has(sectionId)) {
      newExpanded.delete(sectionId)
    } else {
      newExpanded.add(sectionId)
    }
    setExpandedSections(newExpanded)
  }

  const sidebarSections: Array<{
    id: string
    title: string
    items: SidebarItem[]
  }> = [
    {
      id: 'main',
      title: 'Main',
      items: [
        {
          id: 'dashboard',
          title: 'Dashboard',
          icon: LayoutDashboard,
          path: '/admin/dashboard'
        }
      ]
    },
    {
      id: 'system',
      title: 'System Management',
      items: [
        {
          id: 'users',
          title: 'Users & Roles',
          icon: Users,
          path: '/admin/users',
          permission: 'MANAGE_USERS'
        },
        {
          id: 'hierarchy',
          title: 'Hierarchy',
          icon: TreePine,
          path: '/admin/hierarchy',
          permission: 'MANAGE_HIERARCHY'
        },
        {
          id: 'security',
          title: 'Security',
          icon: Shield,
          path: '/admin/security',
          permission: 'VIEW_SECURITY'
        }
      ]
    },
    {
      id: 'monitoring',
      title: 'System Monitoring',
      items: [
        {
          id: 'health',
          title: 'System Health',
          icon: Activity,
          path: '/admin/health'
        },
        {
          id: 'logs',
          title: 'System Logs',
          icon: Database,
          path: '/admin/logs',
          badge: '!'
        }
      ]
    },
    {
      id: 'modules',
      title: 'Business Modules',
      items: [
        // Static module entries
        {
          id: 'logger',
          title: 'Device Logger',
          icon: Database,
          path: '/admin/modules/logger',
          permission: 'MANAGE_DEVICES'
        },
        {
          id: 'oee',
          title: 'OEE Monitoring',
          icon: BarChart3,
          path: '/admin/modules/oee',
          permission: 'MANAGE_OEE'
        },
        {
          id: 'scheduling',
          title: 'Equipment Scheduling',
          icon: Calendar,
          path: '/admin/modules/scheduling',
          permission: 'MANAGE_SCHEDULING'
        }
        // Dynamic modules from registry will be added here
      ]
    }
  ]

  const isItemActive = (path: string) => {
    return location.pathname === path || 
           (path !== '/admin/dashboard' && location.pathname.startsWith(path))
  }

  const canViewItem = (item: SidebarItem) => {
    return !item.permission || hasPermission(item.permission as any)
  }

  const renderSidebarItem = (item: SidebarItem, level = 0) => {
    if (!canViewItem(item)) return null

    const isActive = isItemActive(item.path)
    const hasChildren = item.children && item.children.length > 0
    const isExpanded = expandedSections.has(item.id)

    return (
      <div key={item.id}>
        <Link
          to={item.path}
          className={cn(
            "flex items-center justify-between w-full px-3 py-2 text-sm rounded-md transition-colors",
            {
              "bg-primary text-primary-foreground": isActive,
              "hover:bg-accent hover:text-accent-foreground": !isActive,
              "pl-6": level > 0,
              "text-muted-foreground": !isActive
            }
          )}
        >
          <div className="flex items-center space-x-3">
            <item.icon className={cn("h-4 w-4", {
              "text-primary-foreground": isActive,
              "text-muted-foreground": !isActive
            })} />
            {!sidebarCollapsed && (
              <>
                <span className="font-medium">{item.title}</span>
                {item.badge && (
                  <Badge variant="error" size="sm">
                    {item.badge}
                  </Badge>
                )}
              </>
            )}
          </div>
          
          {!sidebarCollapsed && hasChildren && (
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6"
              onClick={(e) => {
                e.preventDefault()
                toggleSection(item.id)
              }}
            >
              {isExpanded ? (
                <ChevronDown className="h-3 w-3" />
              ) : (
                <ChevronRight className="h-3 w-3" />
              )}
            </Button>
          )}
        </Link>

        {/* Render children if expanded */}
        {!sidebarCollapsed && hasChildren && isExpanded && (
          <div className="ml-4 mt-1 space-y-1">
            {item.children!.map(child => renderSidebarItem(child, level + 1))}
          </div>
        )}
      </div>
    )
  }

  return (
    <aside className={cn(
      "bg-card border-r border-border transition-all duration-300 flex flex-col",
      {
        "w-64": !sidebarCollapsed,
        "w-16": sidebarCollapsed
      }
    )}>
      <div className="flex-1 overflow-y-auto p-4">
        <div className="space-y-6">
          {sidebarSections.map(section => {
            const visibleItems = section.items.filter(canViewItem)
            if (visibleItems.length === 0) return null

            return (
              <div key={section.id}>
                {!sidebarCollapsed && (
                  <h3 className="mb-2 px-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                    {section.title}
                  </h3>
                )}
                <div className="space-y-1">
                  {visibleItems.map(item => renderSidebarItem(item))}
                </div>
              </div>
            )
          })}
        </div>
      </div>

      {/* Sidebar Footer */}
      <div className="p-4 border-t border-border">
        <Button
          variant="ghost"
          size="sm"
          className="w-full justify-start"
        >
          <Settings className="h-4 w-4" />
          {!sidebarCollapsed && <span className="ml-3">Settings</span>}
        </Button>
      </div>
    </aside>
  )
}