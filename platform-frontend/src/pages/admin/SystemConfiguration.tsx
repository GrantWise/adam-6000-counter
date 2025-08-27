import React, { useState, useEffect, useCallback } from 'react'
import {
  Settings,
  Save,
  RefreshCw,
  Download,
  Upload,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Toggle,
  Calendar,
  Clock,
  Shield,
  Server,
  Database,
  Mail,
  Webhook,
  FileText,
  History,
  Search,
  Filter
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Switch } from '@/components/ui/switch'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { DataTable } from '@/components/shared/DataTable'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import { systemConfigurationService } from '@/lib/services/systemConfigurationService'
import type {
  TableColumn,
  ComponentStatus
} from '@/types'

/**
 * System Configuration Management Dashboard
 * Features: System settings, feature flags, maintenance scheduling, notifications
 */
const SystemConfiguration: React.FC = () => {
  const [loading, setLoading] = useState<ComponentStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('settings')

  // Configuration state
  const [settings, setSettings] = useState<any[]>([])
  const [featureFlags, setFeatureFlags] = useState<any[]>([])
  const [maintenanceWindows, setMaintenanceWindows] = useState<any[]>([])
  const [notificationSettings, setNotificationSettings] = useState<any>(null)
  const [systemStatus, setSystemStatus] = useState<any>(null)

  // UI state
  const [showMaintenanceDialog, setShowMaintenanceDialog] = useState(false)
  const [showImportDialog, setShowImportDialog] = useState(false)
  const [selectedCategory, setSelectedCategory] = useState('')
  const [searchTerm, setSearchTerm] = useState('')

  // Load configuration settings
  const loadSettings = useCallback(async () => {
    try {
      const response = await systemConfigurationService.getConfiguration(selectedCategory || undefined)
      if (response.success && response.data) {
        setSettings(response.data.settings)
      }
    } catch (err) {
      console.error('Failed to load settings:', err)
      // Mock data for development
      setSettings([
        {
          key: 'system.name',
          value: 'Industrial ADAM System',
          category: 'System',
          description: 'System name displayed in the UI',
          dataType: 'string',
          isEncrypted: false,
          isRequired: true
        },
        {
          key: 'security.session.timeout',
          value: 3600,
          category: 'Security',
          description: 'Session timeout in seconds',
          dataType: 'number',
          isEncrypted: false,
          isRequired: true
        },
        {
          key: 'database.connection.poolSize',
          value: 20,
          category: 'Database',
          description: 'Database connection pool size',
          dataType: 'number',
          isEncrypted: false,
          isRequired: true
        },
        {
          key: 'api.rate.limit',
          value: 1000,
          category: 'API',
          description: 'API rate limit per minute',
          dataType: 'number',
          isEncrypted: false,
          isRequired: true
        }
      ])
    }
  }, [selectedCategory])

  // Load feature flags
  const loadFeatureFlags = useCallback(async () => {
    try {
      const response = await systemConfigurationService.getFeatureFlags()
      if (response.success && response.data) {
        setFeatureFlags(response.data.flags)
      }
    } catch (err) {
      console.error('Failed to load feature flags:', err)
      // Mock data for development
      setFeatureFlags([
        {
          name: 'advanced_analytics',
          enabled: true,
          description: 'Enable advanced analytics and reporting features',
          conditions: {
            userRoles: ['Admin', 'SystemAdmin']
          }
        },
        {
          name: 'real_time_notifications',
          enabled: true,
          description: 'Enable real-time push notifications',
          conditions: {}
        },
        {
          name: 'beta_dashboard',
          enabled: false,
          description: 'Enable new beta dashboard UI',
          conditions: {
            percentage: 10
          }
        },
        {
          name: 'maintenance_mode',
          enabled: false,
          description: 'Enable maintenance mode',
          conditions: {}
        }
      ])
    }
  }, [])

  // Load maintenance windows
  const loadMaintenanceWindows = useCallback(async () => {
    try {
      const response = await systemConfigurationService.getMaintenanceWindows({ pageSize: 10 })
      if (response.success && response.data) {
        setMaintenanceWindows(response.data.windows)
      }
    } catch (err) {
      console.error('Failed to load maintenance windows:', err)
      // Mock data for development
      setMaintenanceWindows([
        {
          id: '1',
          title: 'Database Maintenance',
          description: 'Scheduled database optimization and backup',
          scheduledStart: new Date(Date.now() + 24 * 60 * 60 * 1000),
          scheduledEnd: new Date(Date.now() + 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000),
          status: 'scheduled',
          affectedServices: ['Database', 'API'],
          createdBy: 'admin'
        },
        {
          id: '2',
          title: 'Security Update',
          description: 'Apply latest security patches',
          scheduledStart: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000),
          scheduledEnd: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 60 * 60 * 1000),
          status: 'scheduled',
          affectedServices: ['All Services'],
          createdBy: 'admin'
        }
      ])
    }
  }, [])

  // Load notification settings
  const loadNotificationSettings = useCallback(async () => {
    try {
      const response = await systemConfigurationService.getNotificationSettings()
      if (response.success && response.data) {
        setNotificationSettings(response.data)
      }
    } catch (err) {
      console.error('Failed to load notification settings:', err)
      // Mock data for development
      setNotificationSettings({
        email: {
          enabled: true,
          smtpServer: 'smtp.factory.com',
          smtpPort: 587,
          useSSL: true,
          username: 'notifications@factory.com',
          fromAddress: 'notifications@factory.com',
          fromName: 'Industrial ADAM System'
        },
        webhook: {
          enabled: true,
          endpoints: [
            {
              name: 'Alerts Webhook',
              url: 'https://alerts.factory.com/webhook',
              events: ['security_alert', 'system_error'],
              headers: {
                'Authorization': 'Bearer ***'
              }
            }
          ]
        },
        inApp: {
          enabled: true,
          retentionDays: 30
        }
      })
    }
  }, [])

  // Load system status
  const loadSystemStatus = useCallback(async () => {
    try {
      const response = await systemConfigurationService.getSystemStatus()
      if (response.success && response.data) {
        setSystemStatus(response.data)
      }
    } catch (err) {
      console.error('Failed to load system status:', err)
      // Mock data for development
      setSystemStatus({
        maintenanceMode: false,
        readOnlyMode: false,
        version: '1.0.0',
        uptime: 86400,
        lastConfigUpdate: new Date(Date.now() - 3600000),
        pendingRestarts: [],
        requiresRestart: false
      })
    }
  }, [])

  // Load all data
  const loadAllData = useCallback(async () => {
    setLoading('loading')
    setError(null)
    
    try {
      await Promise.all([
        loadSettings(),
        loadFeatureFlags(),
        loadMaintenanceWindows(),
        loadNotificationSettings(),
        loadSystemStatus()
      ])
      setLoading('success')
    } catch (err) {
      setError('Failed to load configuration data')
      setLoading('error')
    }
  }, [loadSettings, loadFeatureFlags, loadMaintenanceWindows, loadNotificationSettings, loadSystemStatus])

  useEffect(() => {
    loadAllData()
  }, [loadAllData])

  // Handle setting update
  const handleSettingUpdate = async (key: string, value: any) => {
    try {
      const response = await systemConfigurationService.updateConfigurationSetting(key, value)
      if (response.success) {
        setSuccess('Setting updated successfully')
        await loadSettings()
        await loadSystemStatus()
      } else {
        setError(response.error?.message || 'Failed to update setting')
      }
    } catch (err) {
      setError('An unexpected error occurred')
    }
  }

  // Handle feature flag toggle
  const handleFeatureFlagToggle = async (name: string, enabled: boolean) => {
    try {
      const response = await systemConfigurationService.toggleFeatureFlag(name, enabled)
      if (response.success) {
        setSuccess('Feature flag updated successfully')
        await loadFeatureFlags()
      } else {
        setError(response.error?.message || 'Failed to update feature flag')
      }
    } catch (err) {
      setError('An unexpected error occurred')
    }
  }

  // Handle export configuration
  const handleExportConfiguration = async () => {
    try {
      const response = await systemConfigurationService.exportConfiguration({
        format: 'json',
        includeEncrypted: false
      })
      if (response.success && response.data) {
        const url = window.URL.createObjectURL(response.data)
        const a = document.createElement('a')
        a.style.display = 'none'
        a.href = url
        a.download = `system-config-${new Date().toISOString().split('T')[0]}.json`
        document.body.appendChild(a)
        a.click()
        window.URL.revokeObjectURL(url)
      }
    } catch (err) {
      setError('Failed to export configuration')
    }
  }

  // Handle maintenance mode toggle
  const handleMaintenanceModeToggle = async (enabled: boolean) => {
    try {
      const message = enabled ? 'System is under maintenance' : undefined
      const response = await systemConfigurationService.setMaintenanceMode(enabled, message)
      if (response.success) {
        setSuccess(`Maintenance mode ${enabled ? 'enabled' : 'disabled'}`)
        await loadSystemStatus()
      } else {
        setError(response.error?.message || 'Failed to toggle maintenance mode')
      }
    } catch (err) {
      setError('An unexpected error occurred')
    }
  }

  // Filter settings based on search term and category
  const filteredSettings = settings.filter(setting => {
    const matchesSearch = setting.key.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         setting.description.toLowerCase().includes(searchTerm.toLowerCase())
    const matchesCategory = !selectedCategory || setting.category === selectedCategory
    return matchesSearch && matchesCategory
  })

  const settingsColumns: TableColumn<any>[] = [
    {
      key: 'key',
      title: 'Setting Key',
      sortable: true,
      render: (key, record) => (
        <div>
          <div className="font-medium">{key}</div>
          <div className="text-xs text-muted-foreground">{record.category}</div>
        </div>
      )
    },
    {
      key: 'value',
      title: 'Value',
      render: (value, record) => (
        <div className="max-w-xs">
          {record.dataType === 'boolean' ? (
            <Switch
              checked={Boolean(value)}
              onCheckedChange={(checked) => handleSettingUpdate(record.key, checked)}
            />
          ) : record.isEncrypted ? (
            <span className="text-muted-foreground">••••••••</span>
          ) : (
            <Input
              value={String(value)}
              onChange={(e) => {
                const newValue = record.dataType === 'number' ? Number(e.target.value) : e.target.value
                handleSettingUpdate(record.key, newValue)
              }}
              className="w-full"
            />
          )}
        </div>
      )
    },
    {
      key: 'description',
      title: 'Description',
      render: (description) => (
        <span className="text-sm text-muted-foreground">{description}</span>
      )
    },
    {
      key: 'isRequired',
      title: 'Required',
      render: (isRequired) => (
        <Badge variant={isRequired ? 'default' : 'secondary'}>
          {isRequired ? 'Required' : 'Optional'}
        </Badge>
      )
    }
  ]

  const maintenanceColumns: TableColumn<any>[] = [
    {
      key: 'title',
      title: 'Title',
      render: (title) => <span className="font-medium">{title}</span>
    },
    {
      key: 'scheduledStart',
      title: 'Scheduled Start',
      render: (scheduledStart) => (
        <span className="font-mono text-sm">
          {new Date(scheduledStart).toLocaleString()}
        </span>
      )
    },
    {
      key: 'scheduledEnd',
      title: 'Scheduled End',
      render: (scheduledEnd) => (
        <span className="font-mono text-sm">
          {new Date(scheduledEnd).toLocaleString()}
        </span>
      )
    },
    {
      key: 'status',
      title: 'Status',
      render: (status) => (
        <Badge variant={status === 'active' ? 'destructive' : status === 'scheduled' ? 'default' : 'secondary'}>
          {status}
        </Badge>
      )
    },
    {
      key: 'affectedServices',
      title: 'Affected Services',
      render: (services: string[]) => (
        <div className="flex flex-wrap gap-1">
          {services.map((service, index) => (
            <Badge key={index} variant="outline" className="text-xs">
              {service}
            </Badge>
          ))}
        </div>
      )
    }
  ]

  // Clear messages after 5 seconds
  useEffect(() => {
    if (success || error) {
      const timer = setTimeout(() => {
        setSuccess(null)
        setError(null)
      }, 5000)
      return () => clearTimeout(timer)
    }
  }, [success, error])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">System Configuration</h1>
          <p className="text-muted-foreground mt-2">
            Manage system settings, feature flags, and maintenance
          </p>
        </div>
        <div className="flex items-center gap-2">
          {systemStatus?.requiresRestart && (
            <Badge variant="destructive">Restart Required</Badge>
          )}
          <Button variant="outline" onClick={handleExportConfiguration}>
            <Download className="h-4 w-4 mr-2" />
            Export Config
          </Button>
          <Button variant="outline" onClick={() => setShowImportDialog(true)}>
            <Upload className="h-4 w-4 mr-2" />
            Import Config
          </Button>
          <Button variant="outline" onClick={loadAllData}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
        </div>
      </div>

      {/* Status Alerts */}
      {success && (
        <Alert>
          <CheckCircle className="h-4 w-4" />
          <AlertDescription>{success}</AlertDescription>
        </Alert>
      )}

      {error && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* System Status Card */}
      {systemStatus && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Server className="h-5 w-5" />
              System Status
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="flex items-center gap-2">
                <span>Maintenance Mode:</span>
                <Switch
                  checked={systemStatus.maintenanceMode}
                  onCheckedChange={handleMaintenanceModeToggle}
                />
              </div>
              <div className="flex items-center gap-2">
                <span>Version:</span>
                <Badge variant="outline">{systemStatus.version}</Badge>
              </div>
              <div className="flex items-center gap-2">
                <span>Uptime:</span>
                <span className="font-mono text-sm">
                  {Math.floor(systemStatus.uptime / 3600)}h {Math.floor((systemStatus.uptime % 3600) / 60)}m
                </span>
              </div>
              <div className="flex items-center gap-2">
                <span>Last Update:</span>
                <span className="font-mono text-sm">
                  {new Date(systemStatus.lastConfigUpdate).toLocaleString()}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {loading === 'loading' ? (
        <div className="flex justify-center py-12">
          <LoadingSpinner size="lg" text="Loading configuration..." />
        </div>
      ) : (
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="settings">Settings</TabsTrigger>
            <TabsTrigger value="features">Feature Flags</TabsTrigger>
            <TabsTrigger value="maintenance">Maintenance</TabsTrigger>
            <TabsTrigger value="notifications">Notifications</TabsTrigger>
          </TabsList>

          <TabsContent value="settings" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Settings className="h-5 w-5" />
                  System Settings
                </CardTitle>
                <CardDescription>
                  Configure system-wide settings and parameters
                </CardDescription>
              </CardHeader>
              <CardContent>
                {/* Filters */}
                <div className="flex gap-4 mb-4">
                  <div className="relative flex-1">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Search settings..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-10"
                    />
                  </div>
                  <Select value={selectedCategory} onValueChange={setSelectedCategory}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="All Categories" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">All Categories</SelectItem>
                      <SelectItem value="System">System</SelectItem>
                      <SelectItem value="Security">Security</SelectItem>
                      <SelectItem value="Database">Database</SelectItem>
                      <SelectItem value="API">API</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <DataTable
                  data={filteredSettings}
                  columns={settingsColumns}
                  title="Configuration Settings"
                  description={`${filteredSettings.length} settings found`}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="features" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Toggle className="h-5 w-5" />
                  Feature Flags
                </CardTitle>
                <CardDescription>
                  Enable or disable system features dynamically
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {featureFlags.map((flag) => (
                    <div key={flag.name} className="flex items-center justify-between p-4 border rounded-lg">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <h4 className="font-medium">{flag.name.replace(/_/g, ' ')}</h4>
                          <Badge variant={flag.enabled ? 'default' : 'secondary'}>
                            {flag.enabled ? 'Enabled' : 'Disabled'}
                          </Badge>
                        </div>
                        <p className="text-sm text-muted-foreground">{flag.description}</p>
                        {flag.conditions && Object.keys(flag.conditions).length > 0 && (
                          <div className="mt-2">
                            <span className="text-xs text-muted-foreground">
                              Conditions: {JSON.stringify(flag.conditions)}
                            </span>
                          </div>
                        )}
                      </div>
                      <Switch
                        checked={flag.enabled}
                        onCheckedChange={(checked) => handleFeatureFlagToggle(flag.name, checked)}
                      />
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="maintenance" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Calendar className="h-5 w-5" />
                  Maintenance Windows
                </CardTitle>
                <CardDescription>
                  Schedule and manage system maintenance
                </CardDescription>
                <Button onClick={() => setShowMaintenanceDialog(true)} className="w-fit">
                  <Calendar className="h-4 w-4 mr-2" />
                  Schedule Maintenance
                </Button>
              </CardHeader>
              <CardContent>
                <DataTable
                  data={maintenanceWindows}
                  columns={maintenanceColumns}
                  title="Maintenance Schedule"
                  description={`${maintenanceWindows.length} maintenance windows scheduled`}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="notifications" className="mt-6">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Email Settings */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Mail className="h-5 w-5" />
                    Email Notifications
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {notificationSettings?.email && (
                    <div className="space-y-4">
                      <div className="flex items-center gap-2">
                        <span>Enabled:</span>
                        <Switch checked={notificationSettings.email.enabled} />
                      </div>
                      <div>
                        <label className="text-sm font-medium">SMTP Server</label>
                        <Input value={notificationSettings.email.smtpServer} className="mt-1" />
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <label className="text-sm font-medium">Port</label>
                          <Input type="number" value={notificationSettings.email.smtpPort} className="mt-1" />
                        </div>
                        <div className="flex items-center gap-2 mt-6">
                          <span className="text-sm">Use SSL:</span>
                          <Switch checked={notificationSettings.email.useSSL} />
                        </div>
                      </div>
                      <div>
                        <label className="text-sm font-medium">From Address</label>
                        <Input value={notificationSettings.email.fromAddress} className="mt-1" />
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Webhook Settings */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Webhook className="h-5 w-5" />
                    Webhook Notifications
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {notificationSettings?.webhook && (
                    <div className="space-y-4">
                      <div className="flex items-center gap-2">
                        <span>Enabled:</span>
                        <Switch checked={notificationSettings.webhook.enabled} />
                      </div>
                      <div className="space-y-3">
                        {notificationSettings.webhook.endpoints.map((endpoint: any, index: number) => (
                          <div key={index} className="p-3 border rounded-lg">
                            <div className="font-medium">{endpoint.name}</div>
                            <div className="text-sm text-muted-foreground">{endpoint.url}</div>
                            <div className="flex flex-wrap gap-1 mt-2">
                              {endpoint.events.map((event: string) => (
                                <Badge key={event} variant="outline" className="text-xs">
                                  {event}
                                </Badge>
                              ))}
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            </div>
          </TabsContent>
        </Tabs>
      )}

      {/* Maintenance Dialog */}
      <Dialog open={showMaintenanceDialog} onOpenChange={setShowMaintenanceDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Schedule Maintenance</DialogTitle>
            <DialogDescription>
              Plan a maintenance window for system updates
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <label className="text-sm font-medium">Title</label>
              <Input placeholder="Maintenance window title" className="mt-1" />
            </div>
            <div>
              <label className="text-sm font-medium">Description</label>
              <Input placeholder="Description of maintenance work" className="mt-1" />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium">Start Date/Time</label>
                <Input type="datetime-local" className="mt-1" />
              </div>
              <div>
                <label className="text-sm font-medium">End Date/Time</label>
                <Input type="datetime-local" className="mt-1" />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowMaintenanceDialog(false)}>
              Cancel
            </Button>
            <Button>Schedule Maintenance</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

export default SystemConfiguration