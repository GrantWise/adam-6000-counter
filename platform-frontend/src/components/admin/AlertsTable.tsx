import React, { useState, useMemo } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tooltip } from '@/components/ui/tooltip'
import { 
  AlertTriangle, 
  CheckCircle, 
  Clock, 
  User,
  Filter,
  X,
  ChevronLeft,
  ChevronRight
} from 'lucide-react'
import type { AlertsTableProps, SystemAlert } from '@/types'

/**
 * AlertsTable - Sortable, filterable alerts list with acknowledgment
 * Advanced DataTable with filtering, sorting, and batch operations
 */
const AlertsTable: React.FC<AlertsTableProps> = ({ 
  alerts, 
  loading = false,
  onAcknowledge,
  onResolve,
  pagination 
}) => {
  const [selectedRows, setSelectedRows] = useState<string[]>([])
  const [sortField, setSortField] = useState<keyof SystemAlert>('timestamp')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc')
  const [filters, setFilters] = useState({
    type: '',
    severity: '',
    acknowledged: 'all',
    resolved: 'all'
  })

  // Filter and sort alerts
  const filteredAndSortedAlerts = useMemo(() => {
    let filtered = alerts.filter(alert => {
      if (filters.type && alert.type !== filters.type) return false
      if (filters.severity && alert.severity !== filters.severity) return false
      if (filters.acknowledged !== 'all') {
        const isAcknowledged = alert.acknowledged
        if (filters.acknowledged === 'yes' && !isAcknowledged) return false
        if (filters.acknowledged === 'no' && isAcknowledged) return false
      }
      if (filters.resolved !== 'all') {
        const isResolved = alert.resolved
        if (filters.resolved === 'yes' && !isResolved) return false
        if (filters.resolved === 'no' && isResolved) return false
      }
      return true
    })

    // Sort alerts
    filtered.sort((a, b) => {
      let aValue = a[sortField]
      let bValue = b[sortField]
      
      if (aValue instanceof Date) aValue = aValue.getTime()
      if (bValue instanceof Date) bValue = bValue.getTime()
      
      if (sortDirection === 'asc') {
        return aValue < bValue ? -1 : aValue > bValue ? 1 : 0
      } else {
        return aValue > bValue ? -1 : aValue < bValue ? 1 : 0
      }
    })

    return filtered
  }, [alerts, sortField, sortDirection, filters])

  // Severity configuration
  const severityConfig = {
    critical: {
      color: 'bg-red-100 text-red-800 border-red-200',
      icon: AlertTriangle,
      iconColor: 'text-red-600'
    },
    high: {
      color: 'bg-orange-100 text-orange-800 border-orange-200',
      icon: AlertTriangle,
      iconColor: 'text-orange-600'
    },
    medium: {
      color: 'bg-yellow-100 text-yellow-800 border-yellow-200',
      icon: AlertTriangle,
      iconColor: 'text-yellow-600'
    },
    low: {
      color: 'bg-blue-100 text-blue-800 border-blue-200',
      icon: AlertTriangle,
      iconColor: 'text-blue-600'
    }
  }

  // Type configuration
  const typeConfig = {
    error: { color: 'bg-red-100 text-red-800', label: 'Error' },
    warning: { color: 'bg-yellow-100 text-yellow-800', label: 'Warning' },
    info: { color: 'bg-blue-100 text-blue-800', label: 'Info' }
  }

  // Handle sorting
  const handleSort = (field: keyof SystemAlert) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDirection('desc')
    }
  }

  // Handle row selection
  const handleRowSelect = (alertId: string, checked: boolean) => {
    if (checked) {
      setSelectedRows([...selectedRows, alertId])
    } else {
      setSelectedRows(selectedRows.filter(id => id !== alertId))
    }
  }

  // Handle select all
  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      setSelectedRows(filteredAndSortedAlerts.map(alert => alert.id))
    } else {
      setSelectedRows([])
    }
  }

  // Handle batch acknowledge
  const handleBatchAcknowledge = () => {
    selectedRows.forEach(alertId => onAcknowledge(alertId))
    setSelectedRows([])
  }

  // Handle batch resolve
  const handleBatchResolve = () => {
    if (onResolve) {
      selectedRows.forEach(alertId => onResolve(alertId))
      setSelectedRows([])
    }
  }

  // Clear filters
  const clearFilters = () => {
    setFilters({
      type: '',
      severity: '',
      acknowledged: 'all',
      resolved: 'all'
    })
  }

  // Format timestamp
  const formatTimestamp = (date: Date) => {
    return new Date(date).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const hasActiveFilters = Object.values(filters).some(value => value && value !== 'all')

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <AlertTriangle className="w-5 h-5" />
            System Alerts
            <Badge variant="outline">
              {filteredAndSortedAlerts.length} of {alerts.length}
            </Badge>
          </CardTitle>
          
          {/* Batch Actions */}
          {selectedRows.length > 0 && (
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">
                {selectedRows.length} selected
              </span>
              <Button size="sm" onClick={handleBatchAcknowledge}>
                Acknowledge
              </Button>
              {onResolve && (
                <Button size="sm" variant="outline" onClick={handleBatchResolve}>
                  Resolve
                </Button>
              )}
            </div>
          )}
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-4 pt-4">
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-muted-foreground" />
            <span className="text-sm text-muted-foreground">Filters:</span>
          </div>

          <select
            value={filters.severity}
            onChange={(e) => setFilters({ ...filters, severity: e.target.value })}
            className="px-3 py-1 border border-gray-300 rounded text-sm"
          >
            <option value="">All Severities</option>
            <option value="critical">Critical</option>
            <option value="high">High</option>
            <option value="medium">Medium</option>
            <option value="low">Low</option>
          </select>

          <select
            value={filters.type}
            onChange={(e) => setFilters({ ...filters, type: e.target.value })}
            className="px-3 py-1 border border-gray-300 rounded text-sm"
          >
            <option value="">All Types</option>
            <option value="error">Error</option>
            <option value="warning">Warning</option>
            <option value="info">Info</option>
          </select>

          <select
            value={filters.acknowledged}
            onChange={(e) => setFilters({ ...filters, acknowledged: e.target.value })}
            className="px-3 py-1 border border-gray-300 rounded text-sm"
          >
            <option value="all">All Status</option>
            <option value="yes">Acknowledged</option>
            <option value="no">Unacknowledged</option>
          </select>

          {hasActiveFilters && (
            <Button size="sm" variant="ghost" onClick={clearFilters}>
              <X className="w-4 h-4 mr-1" />
              Clear
            </Button>
          )}
        </div>
      </CardHeader>

      <CardContent>
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b">
                    <th className="text-left p-2">
                      <input
                        type="checkbox"
                        checked={selectedRows.length === filteredAndSortedAlerts.length && filteredAndSortedAlerts.length > 0}
                        onChange={(e) => handleSelectAll(e.target.checked)}
                        className="rounded border-gray-300"
                      />
                    </th>
                    <th 
                      className="text-left p-2 cursor-pointer hover:bg-gray-50"
                      onClick={() => handleSort('severity')}
                    >
                      Severity
                      {sortField === 'severity' && (
                        <span className="ml-1">{sortDirection === 'asc' ? '↑' : '↓'}</span>
                      )}
                    </th>
                    <th 
                      className="text-left p-2 cursor-pointer hover:bg-gray-50"
                      onClick={() => handleSort('type')}
                    >
                      Type
                      {sortField === 'type' && (
                        <span className="ml-1">{sortDirection === 'asc' ? '↑' : '↓'}</span>
                      )}
                    </th>
                    <th 
                      className="text-left p-2 cursor-pointer hover:bg-gray-50"
                      onClick={() => handleSort('title')}
                    >
                      Alert
                      {sortField === 'title' && (
                        <span className="ml-1">{sortDirection === 'asc' ? '↑' : '↓'}</span>
                      )}
                    </th>
                    <th 
                      className="text-left p-2 cursor-pointer hover:bg-gray-50"
                      onClick={() => handleSort('source')}
                    >
                      Source
                      {sortField === 'source' && (
                        <span className="ml-1">{sortDirection === 'asc' ? '↑' : '↓'}</span>
                      )}
                    </th>
                    <th 
                      className="text-left p-2 cursor-pointer hover:bg-gray-50"
                      onClick={() => handleSort('timestamp')}
                    >
                      Time
                      {sortField === 'timestamp' && (
                        <span className="ml-1">{sortDirection === 'asc' ? '↑' : '↓'}</span>
                      )}
                    </th>
                    <th className="text-left p-2">Status</th>
                    <th className="text-left p-2">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredAndSortedAlerts.map((alert) => {
                    const severityConf = severityConfig[alert.severity]
                    const typeConf = typeConfig[alert.type]
                    const SeverityIcon = severityConf.icon

                    return (
                      <tr key={alert.id} className="border-b hover:bg-gray-50">
                        <td className="p-2">
                          <input
                            type="checkbox"
                            checked={selectedRows.includes(alert.id)}
                            onChange={(e) => handleRowSelect(alert.id, e.target.checked)}
                            className="rounded border-gray-300"
                          />
                        </td>
                        <td className="p-2">
                          <div className="flex items-center gap-2">
                            <SeverityIcon className={`w-4 h-4 ${severityConf.iconColor}`} />
                            <Badge variant="outline" className={severityConf.color}>
                              {alert.severity.toUpperCase()}
                            </Badge>
                          </div>
                        </td>
                        <td className="p-2">
                          <Badge variant="outline" className={typeConf.color}>
                            {typeConf.label}
                          </Badge>
                        </td>
                        <td className="p-2">
                          <div>
                            <div className="font-medium text-sm">{alert.title}</div>
                            <div className="text-xs text-muted-foreground truncate max-w-xs">
                              {alert.message}
                            </div>
                          </div>
                        </td>
                        <td className="p-2">
                          <span className="font-mono text-xs bg-gray-100 px-2 py-1 rounded">
                            {alert.source}
                          </span>
                        </td>
                        <td className="p-2">
                          <div className="flex items-center gap-1 text-sm">
                            <Clock className="w-3 h-3 text-muted-foreground" />
                            {formatTimestamp(alert.timestamp)}
                          </div>
                        </td>
                        <td className="p-2">
                          <div className="flex flex-col gap-1">
                            {alert.acknowledged && (
                              <div className="flex items-center gap-1 text-xs text-green-600">
                                <CheckCircle className="w-3 h-3" />
                                <span>Ack</span>
                              </div>
                            )}
                            {alert.resolved && (
                              <div className="flex items-center gap-1 text-xs text-blue-600">
                                <CheckCircle className="w-3 h-3" />
                                <span>Resolved</span>
                              </div>
                            )}
                            {!alert.acknowledged && !alert.resolved && (
                              <span className="text-xs text-red-600">Active</span>
                            )}
                          </div>
                        </td>
                        <td className="p-2">
                          <div className="flex items-center gap-1">
                            {!alert.acknowledged && (
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => onAcknowledge(alert.id)}
                              >
                                Ack
                              </Button>
                            )}
                            {onResolve && !alert.resolved && (
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => onResolve(alert.id)}
                              >
                                Resolve
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>

              {filteredAndSortedAlerts.length === 0 && (
                <div className="text-center py-8 text-muted-foreground">
                  No alerts found
                </div>
              )}
            </div>

            {/* Pagination */}
            {pagination && (
              <div className="flex items-center justify-between mt-4 pt-4 border-t">
                <div className="text-sm text-muted-foreground">
                  Showing {((pagination.current - 1) * pagination.pageSize) + 1} to {Math.min(pagination.current * pagination.pageSize, pagination.total)} of {pagination.total} alerts
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => pagination.onChange(pagination.current - 1, pagination.pageSize)}
                    disabled={pagination.current === 1}
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </Button>
                  <span className="text-sm">
                    Page {pagination.current} of {Math.ceil(pagination.total / pagination.pageSize)}
                  </span>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => pagination.onChange(pagination.current + 1, pagination.pageSize)}
                    disabled={pagination.current >= Math.ceil(pagination.total / pagination.pageSize)}
                  >
                    <ChevronRight className="w-4 h-4" />
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  )
}

export default AlertsTable