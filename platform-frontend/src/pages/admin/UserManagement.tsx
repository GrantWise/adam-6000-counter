import React, { useState, useEffect, useCallback } from 'react'
import {
  Users,
  UserPlus,
  Search,
  Filter,
  Download,
  Upload,
  MoreHorizontal,
  Edit,
  Trash2,
  Lock,
  Unlock,
  UserX,
  RefreshCw,
  Activity,
  Shield,
  Settings,
  Clock,
  Eye,
  UserCog
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert } from '@/components/ui/alert'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { DataTable } from '@/components/shared/DataTable'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import UserForm from '@/components/admin/UserForm'
import { userService } from '@/lib/services/userService'
import { userManagementService } from '@/lib/services/userManagementService'
import { authService } from '@/lib/services/authService'
import type {
  User,
  UserRole,
  TableColumn,
  SortConfig,
  PaginatedResponse,
  ComponentStatus
} from '@/types'

interface UserStats {
  total: number
  active: number
  inactive: number
  locked: number
  byRole: Record<UserRole, number>
  recentActivity: number
}

/**
 * User Management Page - Complete CRUD interface for user administration
 * Features: User creation, editing, role management, hierarchy assignments, bulk operations
 */
const UserManagement: React.FC = () => {
  const [users, setUsers] = useState<User[]>([])
  const [stats, setStats] = useState<UserStats | null>(null)
  const [loading, setLoading] = useState<ComponentStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  
  // Table state
  const [selectedUsers, setSelectedUsers] = useState<string[]>([])
  const [pagination, setPagination] = useState({ current: 1, pageSize: 25, total: 0, totalPages: 0 })
  const [sorting, setSorting] = useState<SortConfig>({ field: 'fullName', direction: 'asc' })
  const [searchTerm, setSearchTerm] = useState('')
  const [roleFilter, setRoleFilter] = useState<UserRole | ''>('')
  const [statusFilter, setStatusFilter] = useState<'active' | 'inactive' | 'locked' | ''>('')
  
  // Form state
  const [showUserForm, setShowUserForm] = useState(false)
  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create')
  
  // Enhanced Phase 2 state
  const [showUserDetails, setShowUserDetails] = useState(false)
  const [selectedUserForDetails, setSelectedUserForDetails] = useState<User | null>(null)
  const [showBulkActions, setShowBulkActions] = useState(false)
  const [showPermissionMatrix, setShowPermissionMatrix] = useState(false)
  const [bulkActionType, setBulkActionType] = useState<'passwordReset' | 'statusChange' | 'roleChange' | null>(null)

  // Load users with current filters and pagination
  const loadUsers = useCallback(async (showLoading = true) => {
    if (showLoading) setLoading('loading')
    setError(null)

    try {
      const response = await userService.getUsers({
        page: pagination.current,
        pageSize: pagination.pageSize,
        search: searchTerm || undefined,
        role: roleFilter || undefined,
        status: statusFilter || undefined,
        sort: sorting
      })

      if (response.success && response.data) {
        setUsers(response.data.data)
        setPagination(prev => ({
          ...prev,
          total: response.data!.total,
          totalPages: response.data!.totalPages
        }))
        setLoading('success')
      } else {
        throw new Error(response.error?.message || 'Failed to load users')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unexpected error occurred')
      setLoading('error')
      // Set mock data for development
      setUsers([
        {
          id: '1',
          username: 'admin',
          email: 'admin@factory.com',
          fullName: 'System Administrator',
          role: 'SystemAdmin',
          permissions: ['SYSTEM_ADMIN'],
          hierarchyAssignments: [],
          status: 'active',
          lastLogin: new Date(),
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: '2',
          username: 'supervisor1',
          email: 'supervisor@factory.com',
          fullName: 'Production Supervisor',
          role: 'Supervisor',
          permissions: ['VIEW_OEE', 'MANAGE_OEE'],
          hierarchyAssignments: [],
          status: 'active',
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: '3',
          username: 'operator1',
          email: 'operator@factory.com',
          fullName: 'Machine Operator',
          role: 'Operator',
          permissions: ['VIEW_DEVICES'],
          hierarchyAssignments: [],
          status: 'inactive',
          createdAt: new Date(),
          updatedAt: new Date()
        }
      ])
      setPagination(prev => ({ ...prev, total: 3, totalPages: 1 }))
    }
  }, [pagination.current, pagination.pageSize, searchTerm, roleFilter, statusFilter, sorting])

  // Load user statistics
  const loadStats = useCallback(async () => {
    try {
      const response = await userService.getUserStats()
      if (response.success && response.data) {
        setStats(response.data)
      } else {
        // Mock stats for development
        setStats({
          total: 3,
          active: 2,
          inactive: 1,
          locked: 0,
          byRole: {
            SystemAdmin: 1,
            Admin: 0,
            Supervisor: 1,
            Operator: 1
          },
          recentActivity: 2
        })
      }
    } catch (err) {
      console.error('Failed to load user stats:', err)
    }
  }, [])

  // Load data on component mount and when filters change
  useEffect(() => {
    loadUsers()
    loadStats()
  }, [loadUsers, loadStats])

  // Handle user actions
  const handleCreateUser = () => {
    setEditingUser(null)
    setFormMode('create')
    setShowUserForm(true)
  }

  const handleEditUser = (user: User) => {
    setEditingUser(user)
    setFormMode('edit')
    setShowUserForm(true)
  }

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('Are you sure you want to delete this user?')) return

    try {
      const response = await userService.deleteUser(userId)
      if (response.success) {
        await loadUsers(false)
        await loadStats()
      } else {
        setError(response.error?.message || 'Failed to delete user')
      }
    } catch (err) {
      setError('An unexpected error occurred while deleting user')
    }
  }

  const handleToggleUserStatus = async (userId: string, currentStatus: string) => {
    const newStatus = currentStatus === 'active' ? 'inactive' : 'active'
    
    try {
      const response = await userService.setUserStatus(userId, newStatus as any)
      if (response.success) {
        await loadUsers(false)
        await loadStats()
      } else {
        setError(response.error?.message || 'Failed to update user status')
      }
    } catch (err) {
      setError('An unexpected error occurred while updating user status')
    }
  }

  const handleUserSaved = async (user: User) => {
    setShowUserForm(false)
    setEditingUser(null)
    await loadUsers(false)
    await loadStats()
  }

  const handleBulkStatusChange = async (status: 'active' | 'inactive' | 'locked') => {
    if (selectedUsers.length === 0) return
    if (!confirm(`Are you sure you want to ${status} ${selectedUsers.length} selected users?`)) return

    try {
      const response = await userService.bulkUpdateUsers(selectedUsers, { status })
      if (response.success) {
        setSelectedUsers([])
        await loadUsers(false)
        await loadStats()
      } else {
        setError(response.error?.message || 'Failed to update users')
      }
    } catch (err) {
      setError('An unexpected error occurred during bulk update')
    }
  }

  // Enhanced Phase 2 handlers
  const handleViewUserDetails = (user: User) => {
    setSelectedUserForDetails(user)
    setShowUserDetails(true)
  }

  const handleBulkPasswordReset = async () => {
    if (selectedUsers.length === 0) return
    if (!confirm(`Are you sure you want to reset passwords for ${selectedUsers.length} selected users?`)) return

    try {
      const response = await userManagementService.bulkPasswordReset(selectedUsers, {
        sendEmailNotification: true,
        forcePasswordChange: true
      })
      if (response.success) {
        setSelectedUsers([])
        setShowBulkActions(false)
        // Show results in a dialog or alert
        alert(`Password reset completed. ${response.data?.successful.length} successful, ${response.data?.failed.length} failed.`)
      } else {
        setError(response.error?.message || 'Failed to reset passwords')
      }
    } catch (err) {
      setError('An unexpected error occurred during bulk password reset')
    }
  }

  const handleTerminateUserSessions = async (userId: string) => {
    if (!confirm('Are you sure you want to terminate all sessions for this user?')) return

    try {
      const response = await userManagementService.terminateUserSessions(userId)
      if (response.success) {
        alert(`${response.data?.terminatedSessions || 0} sessions terminated.`)
        if (selectedUserForDetails?.id === userId) {
          // Refresh user details
          handleViewUserDetails(selectedUserForDetails)
        }
      } else {
        setError(response.error?.message || 'Failed to terminate sessions')
      }
    } catch (err) {
      setError('An unexpected error occurred while terminating sessions')
    }
  }

  // Table configuration
  const columns: TableColumn<User>[] = [
    {
      key: 'fullName',
      title: 'Name',
      sortable: true,
      render: (_, user) => (
        <div>
          <div className="font-medium">{user.fullName}</div>
          <div className="text-sm text-muted-foreground">{user.username}</div>
        </div>
      )
    },
    {
      key: 'email',
      title: 'Email',
      sortable: true
    },
    {
      key: 'role',
      title: 'Role',
      sortable: true,
      render: (role) => (
        <Badge variant={role === 'SystemAdmin' ? 'default' : 'secondary'}>
          {role}
        </Badge>
      )
    },
    {
      key: 'status',
      title: 'Status',
      sortable: true,
      render: (status) => (
        <Badge variant={
          status === 'active' ? 'default' :
          status === 'locked' ? 'destructive' : 'secondary'
        }>
          {status}
        </Badge>
      )
    },
    {
      key: 'lastLogin',
      title: 'Last Login',
      sortable: true,
      render: (lastLogin) => lastLogin ? (
        <span className="text-sm font-mono">
          {new Date(lastLogin).toLocaleDateString()}
        </span>
      ) : (
        <span className="text-muted-foreground text-sm">Never</span>
      )
    },
    {
      key: 'actions',
      title: 'Actions',
      render: (_, user) => (
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleViewUserDetails(user)}
            className="h-8 w-8 p-0"
            title="View Details"
          >
            <Eye className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleEditUser(user)}
            className="h-8 w-8 p-0"
            title="Edit User"
          >
            <Edit className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleToggleUserStatus(user.id, user.status)}
            className="h-8 w-8 p-0"
            title={user.status === 'active' ? 'Deactivate' : 'Activate'}
          >
            {user.status === 'active' ? (
              <UserX className="h-4 w-4" />
            ) : (
              <Unlock className="h-4 w-4" />
            )}
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleDeleteUser(user.id)}
            className="h-8 w-8 p-0 text-destructive hover:text-destructive"
            title="Delete User"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      )
    }
  ]

  const handleSort = (field: string) => {
    const direction = sorting.field === field && sorting.direction === 'asc' ? 'desc' : 'asc'
    setSorting({ field, direction })
  }

  const handlePageChange = (page: number, pageSize: number) => {
    setPagination(prev => ({ ...prev, current: page, pageSize }))
  }

  const handleSelectionChange = (selectedRowKeys: string[]) => {
    setSelectedUsers(selectedRowKeys)
  }

  if (showUserForm) {
    return (
      <div className="container mx-auto py-6">
        <UserForm
          user={editingUser}
          mode={formMode}
          onSave={handleUserSaved}
          onCancel={() => {
            setShowUserForm(false)
            setEditingUser(null)
          }}
        />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">User Management</h1>
          <p className="text-muted-foreground mt-2">
            Manage user accounts, roles, and permissions
          </p>
        </div>
        <Button size="lg" onClick={handleCreateUser} className="flex items-center gap-2">
          <UserPlus className="h-5 w-5" />
          Add User
        </Button>
      </div>

      {/* Statistics Cards */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Total Users</p>
                  <p className="text-2xl font-bold">{stats.total}</p>
                </div>
                <Users className="h-8 w-8 text-muted-foreground" />
              </div>
            </CardContent>
          </Card>
          
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Active</p>
                  <p className="text-2xl font-bold text-green-600">{stats.active}</p>
                </div>
                <div className="h-8 w-8 bg-green-100 rounded-full flex items-center justify-center">
                  <div className="h-3 w-3 bg-green-600 rounded-full" />
                </div>
              </div>
            </CardContent>
          </Card>
          
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Inactive</p>
                  <p className="text-2xl font-bold text-yellow-600">{stats.inactive}</p>
                </div>
                <div className="h-8 w-8 bg-yellow-100 rounded-full flex items-center justify-center">
                  <div className="h-3 w-3 bg-yellow-600 rounded-full" />
                </div>
              </div>
            </CardContent>
          </Card>
          
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Locked</p>
                  <p className="text-2xl font-bold text-red-600">{stats.locked}</p>
                </div>
                <Lock className="h-8 w-8 text-red-600" />
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Filters and Actions */}
      <Card>
        <CardContent className="p-6">
          <div className="flex flex-col md:flex-row gap-4 items-center justify-between">
            <div className="flex flex-1 gap-4">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search users..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
              
              <select
                value={roleFilter}
                onChange={(e) => setRoleFilter(e.target.value as UserRole | '')}
                className="px-3 py-2 border border-input rounded-md bg-background"
              >
                <option value="">All Roles</option>
                <option value="SystemAdmin">System Admin</option>
                <option value="Admin">Administrator</option>
                <option value="Supervisor">Supervisor</option>
                <option value="Operator">Operator</option>
              </select>
              
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value as any)}
                className="px-3 py-2 border border-input rounded-md bg-background"
              >
                <option value="">All Status</option>
                <option value="active">Active</option>
                <option value="inactive">Inactive</option>
                <option value="locked">Locked</option>
              </select>
            </div>
            
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => loadUsers()}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
              
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowPermissionMatrix(true)}
              >
                <Shield className="h-4 w-4 mr-2" />
                Permissions
              </Button>
              
              {selectedUsers.length > 0 && (
                <>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setShowBulkActions(true)}
                  >
                    <Settings className="h-4 w-4 mr-2" />
                    Bulk Actions ({selectedUsers.length})
                  </Button>
                </>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Error Alert */}
      {error && (
        <Alert variant="destructive">
          <div>{error}</div>
        </Alert>
      )}

      {/* Users Table */}
      <DataTable<User>
        data={users}
        columns={columns}
        loading={loading === 'loading'}
        pagination={{
          current: pagination.current,
          pageSize: pagination.pageSize,
          total: pagination.total,
          onChange: handlePageChange
        }}
        sorting={sorting}
        onSort={handleSort}
        selection={{
          selectedRowKeys: selectedUsers,
          onChange: handleSelectionChange
        }}
        title="System Users"
        description={`${pagination.total} users found`}
      />

      {/* User Details Dialog */}
      {showUserDetails && selectedUserForDetails && (
        <UserDetailsDialog
          user={selectedUserForDetails}
          open={showUserDetails}
          onClose={() => {
            setShowUserDetails(false)
            setSelectedUserForDetails(null)
          }}
          onTerminateSessions={() => handleTerminateUserSessions(selectedUserForDetails.id)}
        />
      )}

      {/* Bulk Actions Dialog */}
      {showBulkActions && (
        <BulkActionsDialog
          selectedUsers={selectedUsers}
          open={showBulkActions}
          onClose={() => setShowBulkActions(false)}
          onPasswordReset={handleBulkPasswordReset}
          onStatusChange={handleBulkStatusChange}
        />
      )}

      {/* Permission Matrix Dialog */}
      {showPermissionMatrix && (
        <PermissionMatrixDialog
          open={showPermissionMatrix}
          onClose={() => setShowPermissionMatrix(false)}
        />
      )}
    </div>
  )
}

// Enhanced Phase 2 Components

/**
 * User Details Dialog with Sessions, Activity Timeline, and Analytics
 */
interface UserDetailsDialogProps {
  user: User
  open: boolean
  onClose: () => void
  onTerminateSessions: () => void
}

const UserDetailsDialog: React.FC<UserDetailsDialogProps> = ({ user, open, onClose, onTerminateSessions }) => {
  const [sessions, setSessions] = useState<any[]>([])
  const [activities, setActivities] = useState<any[]>([])
  const [loadingSessions, setLoadingSessions] = useState(false)
  const [loadingActivities, setLoadingActivities] = useState(false)
  const [activeTab, setActiveTab] = useState('overview')

  useEffect(() => {
    if (open) {
      loadUserSessions()
      loadUserActivities()
    }
  }, [open, user.id])

  const loadUserSessions = async () => {
    setLoadingSessions(true)
    try {
      const response = await userManagementService.getUserSessions(user.id, { pageSize: 10 })
      if (response.success && response.data) {
        setSessions(response.data.data)
      }
    } catch (err) {
      console.error('Failed to load user sessions:', err)
    }
    setLoadingSessions(false)
  }

  const loadUserActivities = async () => {
    setLoadingActivities(true)
    try {
      const response = await userManagementService.getUserActivity(user.id, { pageSize: 20 })
      if (response.success && response.data) {
        setActivities(response.data.data)
      }
    } catch (err) {
      console.error('Failed to load user activities:', err)
    }
    setLoadingActivities(false)
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <UserCog className="h-5 w-5" />
            User Details: {user.fullName}
          </DialogTitle>
          <DialogDescription>
            Comprehensive view of user sessions, activity, and analytics
          </DialogDescription>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="sessions">Sessions</TabsTrigger>
            <TabsTrigger value="activity">Activity</TabsTrigger>
            <TabsTrigger value="analytics">Analytics</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="mt-4">
            <div className="grid grid-cols-2 gap-4">
              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">User Information</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2 text-sm">
                    <div><strong>Username:</strong> {user.username}</div>
                    <div><strong>Email:</strong> {user.email}</div>
                    <div><strong>Role:</strong> <Badge variant="secondary">{user.role}</Badge></div>
                    <div><strong>Status:</strong> 
                      <Badge variant={user.status === 'active' ? 'default' : user.status === 'locked' ? 'destructive' : 'secondary'}>
                        {user.status}
                      </Badge>
                    </div>
                    <div><strong>Last Login:</strong> {user.lastLogin ? new Date(user.lastLogin).toLocaleString() : 'Never'}</div>
                    <div><strong>Created:</strong> {new Date(user.createdAt).toLocaleDateString()}</div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">Quick Actions</CardTitle>
                </CardHeader>
                <CardContent className="space-y-2">
                  <Button size="sm" variant="outline" className="w-full" onClick={onTerminateSessions}>
                    <UserX className="h-4 w-4 mr-2" />
                    Terminate All Sessions
                  </Button>
                  <Button size="sm" variant="outline" className="w-full">
                    <Lock className="h-4 w-4 mr-2" />
                    Reset Password
                  </Button>
                  <Button size="sm" variant="outline" className="w-full">
                    <Shield className="h-4 w-4 mr-2" />
                    View Audit Trail
                  </Button>
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          <TabsContent value="sessions" className="mt-4">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Clock className="h-4 w-4" />
                  Active Sessions
                </CardTitle>
              </CardHeader>
              <CardContent>
                {loadingSessions ? (
                  <LoadingSpinner />
                ) : (
                  <div className="space-y-3">
                    {sessions.length === 0 ? (
                      <p className="text-muted-foreground">No active sessions</p>
                    ) : (
                      sessions.map((session: any) => (
                        <div key={session.id} className="border rounded-lg p-3">
                          <div className="flex justify-between items-start">
                            <div className="space-y-1">
                              <div className="font-medium">{session.ipAddress}</div>
                              <div className="text-sm text-muted-foreground">
                                Started: {new Date(session.startedAt).toLocaleString()}
                              </div>
                              <div className="text-sm text-muted-foreground">
                                Last Activity: {new Date(session.lastActivity).toLocaleString()}
                              </div>
                            </div>
                            <Badge variant={session.isActive ? 'default' : 'secondary'}>
                              {session.isActive ? 'Active' : 'Inactive'}
                            </Badge>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="activity" className="mt-4">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Activity className="h-4 w-4" />
                  Recent Activity
                </CardTitle>
              </CardHeader>
              <CardContent>
                {loadingActivities ? (
                  <LoadingSpinner />
                ) : (
                  <div className="space-y-3 max-h-96 overflow-y-auto">
                    {activities.length === 0 ? (
                      <p className="text-muted-foreground">No recent activity</p>
                    ) : (
                      activities.map((activity: any) => (
                        <div key={activity.id} className="border-l-2 border-blue-200 pl-4 pb-3">
                          <div className="flex justify-between items-start">
                            <div>
                              <div className="font-medium">{activity.action}</div>
                              <div className="text-sm text-muted-foreground">{activity.details}</div>
                              <div className="text-xs text-muted-foreground mt-1">
                                {activity.module} • {new Date(activity.timestamp).toLocaleString()}
                              </div>
                            </div>
                            <Badge variant={activity.success ? 'default' : 'destructive'}>
                              {activity.success ? 'Success' : 'Failed'}
                            </Badge>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="analytics" className="mt-4">
            <div className="grid grid-cols-2 gap-4">
              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">Session Analytics</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="text-center py-8">
                    <div className="text-2xl font-bold">{sessions.length}</div>
                    <div className="text-muted-foreground">Active Sessions</div>
                  </div>
                </CardContent>
              </Card>
              
              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">Activity Analytics</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="text-center py-8">
                    <div className="text-2xl font-bold">{activities.length}</div>
                    <div className="text-muted-foreground">Recent Actions</div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>
        </Tabs>

        <DialogFooter>
          <Button onClick={onClose}>Close</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

/**
 * Bulk Actions Dialog for multiple user operations
 */
interface BulkActionsDialogProps {
  selectedUsers: string[]
  open: boolean
  onClose: () => void
  onPasswordReset: () => void
  onStatusChange: (status: 'active' | 'inactive' | 'locked') => void
}

const BulkActionsDialog: React.FC<BulkActionsDialogProps> = ({
  selectedUsers,
  open,
  onClose,
  onPasswordReset,
  onStatusChange
}) => {
  const [confirmAction, setConfirmAction] = useState<string | null>(null)

  const handleConfirmedAction = () => {
    switch (confirmAction) {
      case 'passwordReset':
        onPasswordReset()
        break
      case 'activate':
        onStatusChange('active')
        break
      case 'deactivate':
        onStatusChange('inactive')
        break
      case 'lock':
        onStatusChange('locked')
        break
    }
    setConfirmAction(null)
    onClose()
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Bulk User Actions</DialogTitle>
          <DialogDescription>
            Perform actions on {selectedUsers.length} selected users
          </DialogDescription>
        </DialogHeader>

        {confirmAction ? (
          <div className="py-4">
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-4">
              <div className="flex">
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-yellow-800">
                    Confirm Action
                  </h3>
                  <div className="mt-2 text-sm text-yellow-700">
                    Are you sure you want to {confirmAction.replace(/([A-Z])/g, ' $1').toLowerCase()} {selectedUsers.length} users?
                    This action cannot be undone.
                  </div>
                </div>
              </div>
            </div>
            <div className="flex justify-end space-x-2">
              <Button variant="outline" onClick={() => setConfirmAction(null)}>
                Cancel
              </Button>
              <Button onClick={handleConfirmedAction}>
                Confirm
              </Button>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-2 gap-3 py-4">
            <Button
              variant="outline"
              className="h-16"
              onClick={() => setConfirmAction('passwordReset')}
            >
              <div className="text-center">
                <Lock className="h-5 w-5 mx-auto mb-1" />
                <div className="text-sm">Reset Passwords</div>
              </div>
            </Button>
            
            <Button
              variant="outline"
              className="h-16"
              onClick={() => setConfirmAction('activate')}
            >
              <div className="text-center">
                <Unlock className="h-5 w-5 mx-auto mb-1" />
                <div className="text-sm">Activate Users</div>
              </div>
            </Button>
            
            <Button
              variant="outline"
              className="h-16"
              onClick={() => setConfirmAction('deactivate')}
            >
              <div className="text-center">
                <UserX className="h-5 w-5 mx-auto mb-1" />
                <div className="text-sm">Deactivate Users</div>
              </div>
            </Button>
            
            <Button
              variant="outline"
              className="h-16"
              onClick={() => setConfirmAction('lock')}
            >
              <div className="text-center">
                <Lock className="h-5 w-5 mx-auto mb-1" />
                <div className="text-sm">Lock Users</div>
              </div>
            </Button>
          </div>
        )}

        {!confirmAction && (
          <DialogFooter>
            <Button variant="outline" onClick={onClose}>Cancel</Button>
          </DialogFooter>
        )}
      </DialogContent>
    </Dialog>
  )
}

/**
 * Permission Matrix Dialog for role-based access control
 */
interface PermissionMatrixDialogProps {
  open: boolean
  onClose: () => void
}

const PermissionMatrixDialog: React.FC<PermissionMatrixDialogProps> = ({ open, onClose }) => {
  const [permissions, setPermissions] = useState<any[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (open) {
      loadPermissions()
    }
  }, [open])

  const loadPermissions = async () => {
    setLoading(true)
    try {
      const response = await userManagementService.getRolePermissions()
      if (response.success && response.data) {
        setPermissions(response.data.roles)
      }
    } catch (err) {
      console.error('Failed to load permissions:', err)
    }
    setLoading(false)
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-6xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            Role Permission Matrix
          </DialogTitle>
          <DialogDescription>
            Manage permissions for different user roles
          </DialogDescription>
        </DialogHeader>

        {loading ? (
          <div className="flex justify-center py-8">
            <LoadingSpinner />
          </div>
        ) : (
          <div className="space-y-6">
            {permissions.map((role: any) => (
              <Card key={role.role}>
                <CardHeader>
                  <CardTitle className="text-lg">{role.displayName}</CardTitle>
                  <CardDescription>{role.description}</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                    {role.permissions.map((permission: any) => (
                      <div key={permission.permission} className="flex items-center space-x-2">
                        <input
                          type="checkbox"
                          checked={permission.granted}
                          className="h-4 w-4 rounded border-gray-300"
                          readOnly
                        />
                        <label className="text-sm">
                          {permission.permission.replace(/_/g, ' ')}
                        </label>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Close</Button>
          <Button disabled={loading}>Save Changes</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

export default UserManagement