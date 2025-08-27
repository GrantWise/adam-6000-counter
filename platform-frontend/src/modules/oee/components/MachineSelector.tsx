/**
 * Machine Selector Component
 * Provides UI for selecting and filtering OEE data by machine/equipment
 * Supports role-based access control and machine status indicators
 */

import React, { useState, useCallback, useMemo } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { useAuth } from '@/hooks/useAuth'
import { 
  Monitor, 
  Settings, 
  TrendingUp, 
  AlertCircle, 
  CheckCircle, 
  Clock,
  Filter,
  Grid,
  List,
  Search
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { OeeData } from '../types'

export interface MachineInfo {
  equipmentId: string
  equipmentName: string
  status: 'online' | 'offline' | 'producing' | 'idle' | 'maintenance'
  oeeData?: OeeData
  assignedOperators?: string[]
  area?: string
  category?: 'production' | 'packaging' | 'quality' | 'utility'
}

interface MachineSelectorProps {
  /** Available machines with their current status */
  machines: MachineInfo[]
  
  /** Currently selected machine ID(s) */
  selectedMachines: string[]
  
  /** Callback when machine selection changes */
  onSelectionChange: (machineIds: string[]) => void
  
  /** Selection mode - single machine or multiple */
  selectionMode?: 'single' | 'multiple' | 'comparison'
  
  /** View layout mode */
  viewMode?: 'grid' | 'list' | 'compact'
  
  /** Show machine status indicators */
  showStatus?: boolean
  
  /** Show OEE values in selector */
  showOeeValues?: boolean
  
  /** Enable quick filtering options */
  enableFiltering?: boolean
  
  /** Maximum number of machines for comparison mode */
  maxComparison?: number
  
  /** Loading state */
  loading?: boolean
  
  /** Additional CSS classes */
  className?: string
}

export const MachineSelector: React.FC<MachineSelectorProps> = ({
  machines = [],
  selectedMachines = [],
  onSelectionChange,
  selectionMode = 'single',
  viewMode = 'grid',
  showStatus = true,
  showOeeValues = true,
  enableFiltering = true,
  maxComparison = 4,
  loading = false,
  className
}) => {
  const { user } = useAuth()
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<'all' | 'online' | 'producing' | 'issues'>('all')
  const [areaFilter, setAreaFilter] = useState<'all' | string>('all')
  const [currentViewMode, setCurrentViewMode] = useState(viewMode)

  // Role-based machine filtering
  const accessibleMachines = useMemo(() => {
    if (!user) return machines

    // Admin and Manager roles see all machines
    if (user.role === 'Admin' || user.role === 'Manager') {
      return machines
    }

    // Supervisor role sees machines in their area
    if (user.role === 'Supervisor') {
      // In a real implementation, this would filter by user's assigned area
      // For now, show all machines but could be extended
      return machines
    }

    // Operator role sees only assigned machines
    if (user.role === 'Operator') {
      // In a real implementation, this would filter by assigned operators
      // For now, show all machines but could be extended
      return machines
    }

    return machines
  }, [machines, user])

  // Apply filters to accessible machines
  const filteredMachines = useMemo(() => {
    let filtered = accessibleMachines

    // Search filter
    if (searchTerm) {
      filtered = filtered.filter(machine => 
        machine.equipmentName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        machine.equipmentId.toLowerCase().includes(searchTerm.toLowerCase())
      )
    }

    // Status filter
    if (statusFilter !== 'all') {
      filtered = filtered.filter(machine => {
        switch (statusFilter) {
          case 'online':
            return machine.status === 'online' || machine.status === 'producing' || machine.status === 'idle'
          case 'producing':
            return machine.status === 'producing'
          case 'issues':
            return machine.status === 'offline' || machine.status === 'maintenance'
          default:
            return true
        }
      })
    }

    // Area filter
    if (areaFilter !== 'all') {
      filtered = filtered.filter(machine => machine.area === areaFilter)
    }

    return filtered
  }, [accessibleMachines, searchTerm, statusFilter, areaFilter])

  // Get unique areas for filtering
  const availableAreas = useMemo(() => {
    const areas = new Set(accessibleMachines.map(m => m.area).filter(Boolean))
    return Array.from(areas)
  }, [accessibleMachines])

  // Machine selection handlers
  const handleMachineClick = useCallback((machineId: string) => {
    if (selectionMode === 'single') {
      onSelectionChange([machineId])
    } else if (selectionMode === 'multiple') {
      const isSelected = selectedMachines.includes(machineId)
      if (isSelected) {
        onSelectionChange(selectedMachines.filter(id => id !== machineId))
      } else {
        onSelectionChange([...selectedMachines, machineId])
      }
    } else if (selectionMode === 'comparison') {
      const isSelected = selectedMachines.includes(machineId)
      if (isSelected) {
        onSelectionChange(selectedMachines.filter(id => id !== machineId))
      } else if (selectedMachines.length < maxComparison) {
        onSelectionChange([...selectedMachines, machineId])
      }
    }
  }, [selectionMode, selectedMachines, onSelectionChange, maxComparison])

  // Select all machines helper
  const handleSelectAll = useCallback(() => {
    if (selectionMode === 'multiple' || selectionMode === 'comparison') {
      const limit = selectionMode === 'comparison' ? maxComparison : filteredMachines.length
      const machineIds = filteredMachines.slice(0, limit).map(m => m.equipmentId)
      onSelectionChange(machineIds)
    }
  }, [selectionMode, filteredMachines, maxComparison, onSelectionChange])

  // Clear selection helper
  const handleClearSelection = useCallback(() => {
    onSelectionChange([])
  }, [onSelectionChange])

  // Get machine status display properties
  const getMachineStatusProps = useCallback((machine: MachineInfo) => {
    const oee = machine.oeeData?.oee

    switch (machine.status) {
      case 'producing':
        return {
          status: 'success' as const,
          icon: <CheckCircle className="w-4 h-4" />,
          label: 'Producing',
          color: 'bg-green-500'
        }
      case 'online':
        return {
          status: 'warning' as const,
          icon: <Clock className="w-4 h-4" />,
          label: 'Online',
          color: 'bg-blue-500'
        }
      case 'idle':
        return {
          status: 'warning' as const,
          icon: <Clock className="w-4 h-4" />,
          label: 'Idle',
          color: 'bg-yellow-500'
        }
      case 'maintenance':
        return {
          status: 'warning' as const,
          icon: <Settings className="w-4 h-4" />,
          label: 'Maintenance',
          color: 'bg-orange-500'
        }
      case 'offline':
      default:
        return {
          status: 'error' as const,
          icon: <AlertCircle className="w-4 h-4" />,
          label: 'Offline',
          color: 'bg-red-500'
        }
    }
  }, [])

  // Render machine card
  const renderMachineCard = useCallback((machine: MachineInfo) => {
    const isSelected = selectedMachines.includes(machine.equipmentId)
    const statusProps = getMachineStatusProps(machine)
    const oee = machine.oeeData?.oee
    const hasRealData = machine.oeeData?.isRealData

    return (
      <Card
        key={machine.equipmentId}
        className={cn(
          'p-4 cursor-pointer transition-all duration-200 hover:shadow-md',
          isSelected && 'ring-2 ring-primary-500 bg-primary-50',
          currentViewMode === 'compact' && 'p-2',
          'border border-neutral-200 hover:border-primary-300'
        )}
        onClick={() => handleMachineClick(machine.equipmentId)}
      >
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center space-x-2">
            <Monitor className="w-5 h-5 text-neutral-600" />
            <div>
              <h4 className="font-medium text-neutral-900 text-sm">
                {machine.equipmentName}
              </h4>
              <p className="text-xs text-neutral-500">ID: {machine.equipmentId}</p>
            </div>
          </div>
          {showStatus && (
            <div className="flex items-center space-x-2">
              <div className={cn('w-2 h-2 rounded-full', statusProps.color)} />
              <StatusIndicator 
                status={statusProps.status} 
                size="sm"
                showLabel={false}
              />
            </div>
          )}
        </div>

        {showOeeValues && (
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-sm text-neutral-600">OEE</span>
              <div className="flex items-center space-x-1">
                {hasRealData ? (
                  <span className={cn(
                    'text-sm font-semibold',
                    oee !== null && oee >= 75 ? 'text-status-success' :
                    oee !== null && oee >= 50 ? 'text-status-warning' : 'text-status-error'
                  )}>
                    {oee !== null ? `${oee.toFixed(1)}%` : 'N/A'}
                  </span>
                ) : (
                  <DataQualityIndicator 
                    quality="unavailable" 
                    size="xs" 
                    showLabel={false}
                  />
                )}
              </div>
            </div>

            {machine.oeeData && (
              <div className="flex justify-between text-xs text-neutral-500">
                <span>A: {machine.oeeData.availability?.toFixed(0) || 'N/A'}%</span>
                <span>P: {machine.oeeData.performance?.toFixed(0) || 'N/A'}%</span>
                <span>Q: {machine.oeeData.quality?.toFixed(0) || 'N/A'}%</span>
              </div>
            )}
          </div>
        )}

        {machine.area && (
          <div className="mt-2">
            <Badge variant="outline" className="text-xs">
              {machine.area}
            </Badge>
          </div>
        )}
      </Card>
    )
  }, [selectedMachines, showStatus, showOeeValues, currentViewMode, handleMachineClick, getMachineStatusProps])

  // Render machine list item
  const renderMachineListItem = useCallback((machine: MachineInfo) => {
    const isSelected = selectedMachines.includes(machine.equipmentId)
    const statusProps = getMachineStatusProps(machine)
    const oee = machine.oeeData?.oee

    return (
      <div
        key={machine.equipmentId}
        className={cn(
          'flex items-center justify-between p-3 border-b border-neutral-200 cursor-pointer',
          'hover:bg-neutral-50 transition-colors duration-150',
          isSelected && 'bg-primary-50 border-primary-200'
        )}
        onClick={() => handleMachineClick(machine.equipmentId)}
      >
        <div className="flex items-center space-x-3">
          <Monitor className="w-4 h-4 text-neutral-600" />
          <div>
            <h4 className="font-medium text-neutral-900 text-sm">
              {machine.equipmentName}
            </h4>
            <p className="text-xs text-neutral-500">ID: {machine.equipmentId}</p>
          </div>
        </div>

        <div className="flex items-center space-x-4">
          {machine.area && (
            <Badge variant="outline" className="text-xs">
              {machine.area}
            </Badge>
          )}

          {showOeeValues && oee !== null && (
            <span className={cn(
              'text-sm font-medium',
              oee >= 75 ? 'text-status-success' :
              oee >= 50 ? 'text-status-warning' : 'text-status-error'
            )}>
              {oee.toFixed(1)}%
            </span>
          )}

          {showStatus && (
            <div className="flex items-center space-x-1">
              <div className={cn('w-2 h-2 rounded-full', statusProps.color)} />
              <span className="text-xs text-neutral-600">{statusProps.label}</span>
            </div>
          )}
        </div>
      </div>
    )
  }, [selectedMachines, showStatus, showOeeValues, handleMachineClick, getMachineStatusProps])

  if (loading) {
    return (
      <Card className={cn('p-6', className)}>
        <div className="animate-pulse space-y-4">
          <div className="flex items-center justify-between">
            <div className="h-6 bg-neutral-200 rounded w-40" />
            <div className="h-8 bg-neutral-200 rounded w-20" />
          </div>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {[1, 2, 3, 4].map(i => (
              <div key={i} className="h-32 bg-neutral-200 rounded" />
            ))}
          </div>
        </div>
      </Card>
    )
  }

  return (
    <Card className={cn('p-6', className)}>
      {/* Header with selection info and view controls */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h3 className="text-lg font-semibold text-neutral-900">
            Machine Selection
          </h3>
          <p className="text-sm text-neutral-600">
            {selectionMode === 'single' && 'Select a machine to view its OEE data'}
            {selectionMode === 'multiple' && 'Select multiple machines for batch operations'}
            {selectionMode === 'comparison' && `Select up to ${maxComparison} machines to compare`}
          </p>
        </div>

        <div className="flex items-center space-x-2">
          {selectedMachines.length > 0 && (
            <Badge variant="default" className="mr-2">
              {selectedMachines.length} Selected
            </Badge>
          )}

          <div className="flex items-center border border-neutral-200 rounded">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setCurrentViewMode('grid')}
              className={cn(
                'px-2 py-1',
                currentViewMode === 'grid' && 'bg-primary-100 text-primary-700'
              )}
            >
              <Grid className="w-4 h-4" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setCurrentViewMode('list')}
              className={cn(
                'px-2 py-1',
                currentViewMode === 'list' && 'bg-primary-100 text-primary-700'
              )}
            >
              <List className="w-4 h-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Filtering and Search */}
      {enableFiltering && (
        <div className="space-y-4 mb-6">
          <div className="flex flex-wrap items-center gap-4">
            {/* Search */}
            <div className="flex-1 min-w-48 relative">
              <Search className="w-4 h-4 absolute left-3 top-2.5 text-neutral-400" />
              <input
                type="text"
                placeholder="Search machines..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-9 pr-4 py-2 border border-neutral-200 rounded-md text-sm focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              />
            </div>

            {/* Status Filter */}
            <Tabs value={statusFilter} onValueChange={(v) => setStatusFilter(v as any)} className="w-auto">
              <TabsList className="grid w-full grid-cols-4">
                <TabsTrigger value="all">All</TabsTrigger>
                <TabsTrigger value="online">Online</TabsTrigger>
                <TabsTrigger value="producing">Producing</TabsTrigger>
                <TabsTrigger value="issues">Issues</TabsTrigger>
              </TabsList>
            </Tabs>

            {/* Area Filter */}
            {availableAreas.length > 0 && (
              <select
                value={areaFilter}
                onChange={(e) => setAreaFilter(e.target.value)}
                className="px-3 py-2 border border-neutral-200 rounded-md text-sm focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              >
                <option value="all">All Areas</option>
                {availableAreas.map(area => (
                  <option key={area} value={area}>{area}</option>
                ))}
              </select>
            )}
          </div>

          {/* Bulk Selection Actions */}
          {(selectionMode === 'multiple' || selectionMode === 'comparison') && (
            <div className="flex items-center gap-2">
              <Button 
                variant="outline" 
                size="sm" 
                onClick={handleSelectAll}
                disabled={filteredMachines.length === 0}
              >
                Select All
              </Button>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={handleClearSelection}
                disabled={selectedMachines.length === 0}
              >
                Clear Selection
              </Button>
              
              {selectionMode === 'comparison' && selectedMachines.length === maxComparison && (
                <Badge variant="secondary" className="ml-2">
                  Maximum machines selected for comparison
                </Badge>
              )}
            </div>
          )}
        </div>
      )}

      {/* Machine Display */}
      {filteredMachines.length === 0 ? (
        <div className="text-center py-12">
          <Monitor className="w-12 h-12 text-neutral-400 mx-auto mb-4" />
          <h4 className="text-lg font-medium text-neutral-600 mb-2">
            No machines found
          </h4>
          <p className="text-neutral-500">
            {searchTerm || statusFilter !== 'all' || areaFilter !== 'all' 
              ? 'Try adjusting your filters to see more machines.'
              : 'No machines are available for your role.'
            }
          </p>
        </div>
      ) : (
        <div className={cn(
          currentViewMode === 'grid' && 'grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4',
          currentViewMode === 'list' && 'space-y-0',
          currentViewMode === 'compact' && 'grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-2'
        )}>
          {currentViewMode === 'list' 
            ? filteredMachines.map(renderMachineListItem)
            : filteredMachines.map(renderMachineCard)
          }
        </div>
      )}

      {/* Selection Summary */}
      {selectedMachines.length > 0 && (
        <div className="mt-6 p-4 bg-primary-50 rounded-lg border border-primary-200">
          <div className="flex items-center justify-between">
            <div>
              <h4 className="font-medium text-primary-900 text-sm">
                Selected Machines ({selectedMachines.length})
              </h4>
              <p className="text-sm text-primary-700">
                {selectedMachines.map(id => {
                  const machine = machines.find(m => m.equipmentId === id)
                  return machine?.equipmentName || id
                }).join(', ')}
              </p>
            </div>
            
            {selectionMode === 'comparison' && selectedMachines.length > 1 && (
              <Badge variant="default">
                <TrendingUp className="w-3 h-3 mr-1" />
                Ready to Compare
              </Badge>
            )}
          </div>
        </div>
      )}
    </Card>
  )
}

export default MachineSelector