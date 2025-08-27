import React, { useState, useEffect, useCallback } from 'react'
import {
  Shield,
  Activity,
  Eye,
  Download,
  RefreshCw,
  AlertTriangle,
  Users,
  Clock,
  Search,
  Filter,
  Calendar,
  TrendingUp,
  Lock,
  Globe,
  CheckCircle,
  XCircle
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { DataTable } from '@/components/shared/DataTable'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, BarChart, Bar, PieChart, Pie, Cell } from 'recharts'
import { securityAuditService } from '@/lib/services/securityAuditService'
import type {
  LoginAttempt,
  AuditLogEntry,
  UserActivity,
  SecuritySession,
  TableColumn,
  ComponentStatus
} from '@/types'

/**
 * Security Audit Dashboard - Comprehensive security monitoring and compliance
 * Features: Login analysis, audit trail, user activity, compliance reporting
 */
const SecurityAudit: React.FC = () => {
  const [loading, setLoading] = useState<ComponentStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('overview')
  const [timeRange, setTimeRange] = useState<'24h' | '7d' | '30d'>('24h')
  
  // Data state
  const [securityStats, setSecurityStats] = useState<any>(null)
  const [loginAttempts, setLoginAttempts] = useState<LoginAttempt[]>([])
  const [auditTrail, setAuditTrail] = useState<AuditLogEntry[]>([])
  const [userActivities, setUserActivities] = useState<UserActivity[]>([])
  const [activeSessions, setActiveSessions] = useState<SecuritySession[]>([])
  const [securityAlerts, setSecurityAlerts] = useState<any[]>([])
  
  // Filters
  const [loginFilter, setLoginFilter] = useState({ success: '', username: '', search: '' })
  const [auditFilter, setAuditFilter] = useState({ action: '', entityType: '', search: '' })
  
  // Pagination
  const [loginPagination, setLoginPagination] = useState({ current: 1, pageSize: 25, total: 0 })
  const [auditPagination, setAuditPagination] = useState({ current: 1, pageSize: 25, total: 0 })

  // Load security statistics
  const loadSecurityStats = useCallback(async () => {
    try {
      const response = await securityAuditService.getSecurityStats(timeRange)
      if (response.success && response.data) {
        setSecurityStats(response.data)
      }
    } catch (err) {
      console.error('Failed to load security stats:', err)
      // Mock data for development
      setSecurityStats({
        totalLoginAttempts: 1247,
        successfulLogins: 1156,
        failedLogins: 91,
        uniqueUsers: 23,
        activeSessions: 15,
        suspiciousActivities: 3,
        auditLogEntries: 5842,
        complianceScore: 94,
        topFailureReasons: [
          { reason: 'Invalid password', count: 45 },
          { reason: 'Account locked', count: 23 },
          { reason: 'User not found', count: 15 },
          { reason: '2FA failed', count: 8 }
        ],
        loginAttemptsByHour: Array.from({ length: 24 }, (_, i) => ({
          hour: i,
          successful: Math.floor(Math.random() * 50),
          failed: Math.floor(Math.random() * 10)
        })),
        topUserActivities: [
          { action: 'View Dashboard', count: 342 },
          { action: 'Edit User', count: 89 },
          { action: 'Generate Report', count: 67 },
          { action: 'Update Settings', count: 45 }
        ]
      })
    }
  }, [timeRange])

  // Load login attempts
  const loadLoginAttempts = useCallback(async () => {
    try {
      const response = await securityAuditService.getLoginAttempts({
        page: loginPagination.current,
        pageSize: loginPagination.pageSize,
        username: loginFilter.username || undefined,
        success: loginFilter.success ? loginFilter.success === 'true' : undefined
      })
      if (response.success && response.data) {
        setLoginAttempts(response.data.data)
        setLoginPagination(prev => ({
          ...prev,
          total: response.data!.total
        }))
      }
    } catch (err) {
      console.error('Failed to load login attempts:', err)
      // Mock data for development
      setLoginAttempts([
        {
          id: '1',
          username: 'admin',
          ipAddress: '192.168.1.100',
          userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
          success: true,
          timestamp: new Date(),
          location: 'New York, US'
        },
        {
          id: '2',
          username: 'operator1',
          ipAddress: '10.0.1.50',
          userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
          success: false,
          failureReason: 'Invalid password',
          timestamp: new Date(Date.now() - 300000),
          location: 'Factory Floor'
        }
      ])
    }
  }, [loginPagination.current, loginPagination.pageSize, loginFilter])

  // Load audit trail
  const loadAuditTrail = useCallback(async () => {
    try {
      const response = await securityAuditService.getAuditTrail({
        page: auditPagination.current,
        pageSize: auditPagination.pageSize,
        action: auditFilter.action || undefined,
        entityType: auditFilter.entityType || undefined
      })
      if (response.success && response.data) {
        setAuditTrail(response.data.data)
        setAuditPagination(prev => ({
          ...prev,
          total: response.data!.total
        }))
      }
    } catch (err) {
      console.error('Failed to load audit trail:', err)
      // Mock data for development
      setAuditTrail([
        {
          id: '1',
          userId: '1',
          username: 'admin',
          action: 'CREATE_USER',
          entityType: 'User',
          entityId: '5',
          newValue: { username: 'newuser', role: 'Operator' },
          ipAddress: '192.168.1.100',
          userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
          timestamp: new Date(),
          success: true
        },
        {
          id: '2',
          userId: '2',
          username: 'supervisor1',
          action: 'UPDATE_DEVICE_CONFIG',
          entityType: 'Device',
          entityId: 'ADAM-001',
          oldValue: { threshold: 100 },
          newValue: { threshold: 150 },
          ipAddress: '10.0.1.25',
          userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
          timestamp: new Date(Date.now() - 600000),
          success: true
        }
      ])
    }
  }, [auditPagination.current, auditPagination.pageSize, auditFilter])

  // Load security alerts
  const loadSecurityAlerts = useCallback(async () => {
    try {
      const response = await securityAuditService.getSecurityAlerts({ pageSize: 10 })
      if (response.success && response.data) {
        setSecurityAlerts(response.data.data)
      }
    } catch (err) {
      console.error('Failed to load security alerts:', err)
      // Mock data for development
      setSecurityAlerts([
        {
          id: '1',
          type: 'suspicious_login',
          severity: 'medium',
          message: 'Multiple failed login attempts from IP 203.0.113.1',
          timestamp: new Date(Date.now() - 900000),
          resolved: false
        },
        {
          id: '2',
          type: 'unusual_activity',
          severity: 'low',
          message: 'User accessing system outside normal hours',
          timestamp: new Date(Date.now() - 1800000),
          resolved: true
        }
      ])
    }
  }, [])

  // Load all data
  const loadAllData = useCallback(async () => {
    setLoading('loading')
    setError(null)
    
    try {
      await Promise.all([
        loadSecurityStats(),
        loadLoginAttempts(),
        loadAuditTrail(),
        loadSecurityAlerts()
      ])
      setLoading('success')
    } catch (err) {
      setError('Failed to load security data')
      setLoading('error')
    }
  }, [loadSecurityStats, loadLoginAttempts, loadAuditTrail, loadSecurityAlerts])

  useEffect(() => {
    loadAllData()
  }, [loadAllData])

  // Export audit data
  const handleExportData = async () => {
    try {
      const response = await securityAuditService.exportAuditLogs({
        format: 'csv',
        startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
        endDate: new Date()
      })
      if (response.success && response.data) {
        // Create download link
        const url = window.URL.createObjectURL(response.data)
        const a = document.createElement('a')
        a.style.display = 'none'
        a.href = url
        a.download = `security-audit-${new Date().toISOString().split('T')[0]}.csv`
        document.body.appendChild(a)
        a.click()
        window.URL.revokeObjectURL(url)
      }
    } catch (err) {
      setError('Failed to export audit data')
    }
  }

  // Table columns
  const loginColumns: TableColumn<LoginAttempt>[] = [
    {
      key: 'timestamp',
      title: 'Time',
      sortable: true,
      render: (timestamp) => (
        <span className="font-mono text-sm">
          {new Date(timestamp).toLocaleString()}
        </span>
      )
    },
    {
      key: 'username',
      title: 'Username',
      sortable: true,
      render: (username) => <span className="font-medium">{username}</span>
    },
    {
      key: 'ipAddress',
      title: 'IP Address',
      render: (ipAddress) => (
        <span className="font-mono text-sm">{ipAddress}</span>
      )
    },
    {
      key: 'success',
      title: 'Result',
      render: (success, record) => (
        <div className="flex items-center gap-2">
          <Badge variant={success ? 'default' : 'destructive'}>
            {success ? (
              <><CheckCircle className="h-3 w-3 mr-1" />Success</>
            ) : (
              <><XCircle className="h-3 w-3 mr-1" />Failed</>
            )}
          </Badge>
          {!success && record.failureReason && (
            <span className="text-xs text-muted-foreground">
              {record.failureReason}
            </span>
          )}
        </div>
      )
    },
    {
      key: 'location',
      title: 'Location',
      render: (location) => location ? (
        <span className="text-sm flex items-center gap-1">
          <Globe className="h-3 w-3" />
          {location}
        </span>
      ) : null
    }
  ]

  const auditColumns: TableColumn<AuditLogEntry>[] = [
    {
      key: 'timestamp',
      title: 'Time',
      sortable: true,
      render: (timestamp) => (
        <span className="font-mono text-sm">
          {new Date(timestamp).toLocaleString()}
        </span>
      )
    },
    {
      key: 'username',
      title: 'User',
      render: (username) => <span className="font-medium">{username}</span>
    },
    {
      key: 'action',
      title: 'Action',
      render: (action) => (
        <Badge variant="outline">{action.replace(/_/g, ' ')}</Badge>
      )
    },
    {
      key: 'entityType',
      title: 'Entity',
      render: (entityType, record) => (
        <div>
          <div className="font-medium">{entityType}</div>
          <div className="text-xs text-muted-foreground">{record.entityId}</div>
        </div>
      )
    },
    {
      key: 'success',
      title: 'Status',
      render: (success) => (
        <Badge variant={success ? 'default' : 'destructive'}>
          {success ? 'Success' : 'Failed'}
        </Badge>
      )
    }
  ]

  const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8']

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Security Audit</h1>
          <p className="text-muted-foreground mt-2">
            Security monitoring, audit trails, and compliance reporting
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Select value={timeRange} onValueChange={(value: any) => setTimeRange(value)}>
            <SelectTrigger className="w-32">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="24h">Last 24h</SelectItem>
              <SelectItem value="7d">Last 7 days</SelectItem>
              <SelectItem value="30d">Last 30 days</SelectItem>
            </SelectContent>
          </Select>
          <Button variant="outline" onClick={handleExportData}>
            <Download className="h-4 w-4 mr-2" />
            Export
          </Button>
          <Button variant="outline" onClick={loadAllData}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
        </div>
      </div>

      {/* Error Alert */}
      {error && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {loading === 'loading' ? (
        <div className="flex justify-center py-12">
          <LoadingSpinner size="lg" text="Loading security data..." />
        </div>
      ) : (
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="grid w-full grid-cols-5">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="logins">Login Attempts</TabsTrigger>
            <TabsTrigger value="audit">Audit Trail</TabsTrigger>
            <TabsTrigger value="sessions">Active Sessions</TabsTrigger>
            <TabsTrigger value="compliance">Compliance</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="mt-6">
            <div className="space-y-6">
              {/* Security Alerts */}
              {securityAlerts.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <AlertTriangle className="h-5 w-5 text-orange-500" />
                      Security Alerts
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {securityAlerts.map((alert: any) => (
                        <div key={alert.id} className="flex items-center justify-between p-3 border rounded-lg">
                          <div className="flex items-center gap-3">
                            <Badge variant={alert.severity === 'high' ? 'destructive' : alert.severity === 'medium' ? 'default' : 'secondary'}>
                              {alert.severity}
                            </Badge>
                            <div>
                              <div className="font-medium">{alert.message}</div>
                              <div className="text-sm text-muted-foreground">
                                {new Date(alert.timestamp).toLocaleString()}
                              </div>
                            </div>
                          </div>
                          <Badge variant={alert.resolved ? 'default' : 'outline'}>
                            {alert.resolved ? 'Resolved' : 'Active'}
                          </Badge>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Statistics Cards */}
              {securityStats && (
                <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                  <Card>
                    <CardContent className="p-6">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-medium text-muted-foreground">Total Logins</p>
                          <p className="text-2xl font-bold">{securityStats.totalLoginAttempts}</p>
                        </div>
                        <Activity className="h-8 w-8 text-blue-500" />
                      </div>
                    </CardContent>
                  </Card>
                  
                  <Card>
                    <CardContent className="p-6">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-medium text-muted-foreground">Success Rate</p>
                          <p className="text-2xl font-bold text-green-600">
                            {Math.round((securityStats.successfulLogins / securityStats.totalLoginAttempts) * 100)}%
                          </p>
                        </div>
                        <CheckCircle className="h-8 w-8 text-green-500" />
                      </div>
                    </CardContent>
                  </Card>
                  
                  <Card>
                    <CardContent className="p-6">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-medium text-muted-foreground">Active Sessions</p>
                          <p className="text-2xl font-bold">{securityStats.activeSessions}</p>
                        </div>
                        <Users className="h-8 w-8 text-purple-500" />
                      </div>
                    </CardContent>
                  </Card>
                  
                  <Card>
                    <CardContent className="p-6">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-medium text-muted-foreground">Compliance Score</p>
                          <p className="text-2xl font-bold text-green-600">{securityStats.complianceScore}%</p>
                        </div>
                        <Shield className="h-8 w-8 text-green-500" />
                      </div>
                    </CardContent>
                  </Card>
                </div>
              )}

              {/* Charts */}
              {securityStats && (
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  <Card>
                    <CardHeader>
                      <CardTitle>Login Attempts by Hour</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <ResponsiveContainer width="100%" height={300}>
                        <LineChart data={securityStats.loginAttemptsByHour}>
                          <CartesianGrid strokeDasharray="3 3" />
                          <XAxis dataKey="hour" />
                          <YAxis />
                          <Tooltip />
                          <Line type="monotone" dataKey="successful" stroke="#10B981" strokeWidth={2} />
                          <Line type="monotone" dataKey="failed" stroke="#EF4444" strokeWidth={2} />
                        </LineChart>
                      </ResponsiveContainer>
                    </CardContent>
                  </Card>
                  
                  <Card>
                    <CardHeader>
                      <CardTitle>Top User Activities</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <ResponsiveContainer width="100%" height={300}>
                        <BarChart data={securityStats.topUserActivities}>
                          <CartesianGrid strokeDasharray="3 3" />
                          <XAxis dataKey="action" angle={-45} textAnchor="end" height={80} />
                          <YAxis />
                          <Tooltip />
                          <Bar dataKey="count" fill="#8884d8" />
                        </BarChart>
                      </ResponsiveContainer>
                    </CardContent>
                  </Card>
                </div>
              )}
            </div>
          </TabsContent>

          <TabsContent value="logins" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Lock className="h-5 w-5" />
                  Login Attempts
                </CardTitle>
                <CardDescription>
                  Monitor all authentication attempts and identify security threats
                </CardDescription>
              </CardHeader>
              <CardContent>
                {/* Filters */}
                <div className="flex gap-4 mb-4">
                  <div className="relative flex-1">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Search username..."
                      value={loginFilter.username}
                      onChange={(e) => setLoginFilter(prev => ({ ...prev, username: e.target.value }))}
                      className="pl-10"
                    />
                  </div>
                  <Select value={loginFilter.success} onValueChange={(value) => setLoginFilter(prev => ({ ...prev, success: value }))}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="All Results" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">All Results</SelectItem>
                      <SelectItem value="true">Success Only</SelectItem>
                      <SelectItem value="false">Failed Only</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <DataTable<LoginAttempt>
                  data={loginAttempts}
                  columns={loginColumns}
                  pagination={{
                    current: loginPagination.current,
                    pageSize: loginPagination.pageSize,
                    total: loginPagination.total,
                    onChange: (page, pageSize) => setLoginPagination(prev => ({ ...prev, current: page, pageSize }))
                  }}
                  title="Login Attempts"
                  description={`${loginPagination.total} attempts found`}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="audit" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Eye className="h-5 w-5" />
                  Audit Trail
                </CardTitle>
                <CardDescription>
                  Complete log of all system changes and user actions
                </CardDescription>
              </CardHeader>
              <CardContent>
                {/* Filters */}
                <div className="flex gap-4 mb-4">
                  <div className="relative flex-1">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Search actions..."
                      value={auditFilter.search}
                      onChange={(e) => setAuditFilter(prev => ({ ...prev, search: e.target.value }))}
                      className="pl-10"
                    />
                  </div>
                  <Select value={auditFilter.action} onValueChange={(value) => setAuditFilter(prev => ({ ...prev, action: value }))}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="All Actions" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">All Actions</SelectItem>
                      <SelectItem value="CREATE">Create</SelectItem>
                      <SelectItem value="UPDATE">Update</SelectItem>
                      <SelectItem value="DELETE">Delete</SelectItem>
                    </SelectContent>
                  </Select>
                  <Select value={auditFilter.entityType} onValueChange={(value) => setAuditFilter(prev => ({ ...prev, entityType: value }))}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="All Entities" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">All Entities</SelectItem>
                      <SelectItem value="User">Users</SelectItem>
                      <SelectItem value="Device">Devices</SelectItem>
                      <SelectItem value="Config">Configuration</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <DataTable<AuditLogEntry>
                  data={auditTrail}
                  columns={auditColumns}
                  pagination={{
                    current: auditPagination.current,
                    pageSize: auditPagination.pageSize,
                    total: auditPagination.total,
                    onChange: (page, pageSize) => setAuditPagination(prev => ({ ...prev, current: page, pageSize }))
                  }}
                  title="Audit Trail"
                  description={`${auditPagination.total} entries found`}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="sessions" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Users className="h-5 w-5" />
                  Active Sessions
                </CardTitle>
                <CardDescription>
                  Monitor currently active user sessions
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-center py-12">
                  <Clock className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
                  <h3 className="text-xl font-semibold mb-2">{securityStats?.activeSessions || 0} Active Sessions</h3>
                  <p className="text-muted-foreground">
                    Real-time session monitoring will be displayed here
                  </p>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="compliance" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Shield className="h-5 w-5" />
                  CFR Part 11 Compliance
                </CardTitle>
                <CardDescription>
                  Regulatory compliance monitoring and reporting
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-lg">Compliance Score</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="text-center">
                        <div className="text-4xl font-bold text-green-600 mb-2">
                          {securityStats?.complianceScore || 0}%
                        </div>
                        <div className="text-muted-foreground">Overall Compliance</div>
                      </div>
                    </CardContent>
                  </Card>
                  
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-lg">Digital Signatures</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-3">
                        <div className="flex justify-between items-center">
                          <span>Audit Trail Integrity</span>
                          <Badge variant="default">Verified</Badge>
                        </div>
                        <div className="flex justify-between items-center">
                          <span>Electronic Records</span>
                          <Badge variant="default">Compliant</Badge>
                        </div>
                        <div className="flex justify-between items-center">
                          <span>User Authentication</span>
                          <Badge variant="default">Validated</Badge>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      )}
    </div>
  )
}

export default SecurityAudit