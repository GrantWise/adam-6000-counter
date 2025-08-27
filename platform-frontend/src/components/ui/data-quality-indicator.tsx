/**
 * Data Quality Indicator Component
 * CFR Part 11 Compliant UI component for displaying data quality warnings
 * Shows users when data is simulated, unavailable, or otherwise non-compliant
 */

import React from 'react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { 
  CheckCircle, 
  AlertTriangle, 
  XCircle, 
  Info, 
  AlertCircle,
  Clock
} from 'lucide-react'
import type { DataQuality, DataWithQuality, DATA_QUALITY_INDICATORS } from '@/types'
import { cn } from '@/lib/utils'

interface DataQualityIndicatorProps {
  quality: DataQuality
  warning?: string
  compact?: boolean
  showBadge?: boolean
  showAlert?: boolean
  className?: string
}

interface DataQualityWrapperProps<T> {
  data: DataWithQuality<T>
  children: React.ReactNode
  showFullWarning?: boolean
  className?: string
}

/**
 * Get the appropriate icon for a data quality level
 */
const getQualityIcon = (quality: DataQuality, className = "h-4 w-4") => {
  const iconClass = cn(className)
  
  switch (quality) {
    case 'good':
      return <CheckCircle className={cn(iconClass, "text-green-500")} />
    case 'uncertain':
      return <AlertTriangle className={cn(iconClass, "text-yellow-500")} />
    case 'bad':
      return <XCircle className={cn(iconClass, "text-red-500")} />
    case 'unavailable':
      return <AlertCircle className={cn(iconClass, "text-red-600")} />
    case 'simulated':
      return <Info className={cn(iconClass, "text-blue-500")} />
    default:
      return <Clock className={cn(iconClass, "text-gray-400")} />
  }
}

/**
 * Get the appropriate badge variant for a data quality level
 */
const getQualityBadgeVariant = (quality: DataQuality): "default" | "secondary" | "destructive" | "outline" => {
  switch (quality) {
    case 'good':
      return 'default'
    case 'uncertain':
      return 'secondary'
    case 'bad':
    case 'unavailable':
      return 'destructive'
    case 'simulated':
      return 'outline'
    default:
      return 'secondary'
  }
}

/**
 * Get the appropriate alert variant for a data quality level
 */
const getQualityAlertVariant = (quality: DataQuality): "default" | "destructive" => {
  switch (quality) {
    case 'bad':
    case 'unavailable':
      return 'destructive'
    default:
      return 'default'
  }
}

/**
 * Get user-friendly labels for data quality levels
 */
const getQualityLabel = (quality: DataQuality): string => {
  switch (quality) {
    case 'good':
      return 'VERIFIED'
    case 'uncertain':
      return 'UNCERTAIN'
    case 'bad':
      return 'BAD DATA'
    case 'unavailable':
      return 'NO DATA'
    case 'simulated':
      return 'SIMULATED'
    default:
      return 'UNKNOWN'
  }
}

/**
 * Get detailed description for data quality levels
 */
const getQualityDescription = (quality: DataQuality): string => {
  switch (quality) {
    case 'good':
      return 'Data verified from source system'
    case 'uncertain':
      return 'Data quality uncertain - verify before use'
    case 'bad':
      return 'Data quality poor - do not use for decisions'
    case 'unavailable':
      return 'Source data unavailable'
    case 'simulated':
      return '⚠️ SYNTHETIC DATA - Not from actual system'
    default:
      return 'Data quality unknown'
  }
}

/**
 * Compact data quality indicator - just icon and badge
 */
export function DataQualityIndicator({ 
  quality, 
  warning, 
  compact = false, 
  showBadge = true, 
  showAlert = false,
  className 
}: DataQualityIndicatorProps) {
  const icon = getQualityIcon(quality)
  const label = getQualityLabel(quality)
  const description = warning || getQualityDescription(quality)
  
  if (compact) {
    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <div className={cn("flex items-center gap-1", className)}>
              {icon}
              {showBadge && (
                <Badge variant={getQualityBadgeVariant(quality)} className="text-xs">
                  {label}
                </Badge>
              )}
            </div>
          </TooltipTrigger>
          <TooltipContent>
            <p className="font-medium">{label}</p>
            <p className="text-sm">{description}</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    )
  }

  return (
    <div className={cn("space-y-2", className)}>
      <div className="flex items-center gap-2">
        {icon}
        {showBadge && (
          <Badge variant={getQualityBadgeVariant(quality)}>
            {label}
          </Badge>
        )}
      </div>
      
      {showAlert && quality !== 'good' && (
        <Alert variant={getQualityAlertVariant(quality)}>
          <AlertDescription>
            {description}
          </AlertDescription>
        </Alert>
      )}
    </div>
  )
}

/**
 * Wrapper component that shows data quality indicators for wrapped content
 * Use this to wrap any data display components to show quality warnings
 */
export function DataQualityWrapper<T>({ 
  data, 
  children, 
  showFullWarning = true,
  className 
}: DataQualityWrapperProps<T>) {
  const shouldShowWarning = data.quality !== 'good' && showFullWarning
  
  return (
    <div className={cn("space-y-2", className)}>
      {/* Data quality indicator */}
      <div className="flex items-center justify-between">
        <div className="flex-1">
          {children}
        </div>
        <DataQualityIndicator 
          quality={data.quality}
          warning={data.warning}
          compact
          className="ml-2"
        />
      </div>
      
      {/* Warning alert */}
      {shouldShowWarning && (
        <Alert variant={getQualityAlertVariant(data.quality)} className="border-l-4">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            <div className="space-y-1">
              <p className="font-medium">Data Quality Warning</p>
              <p>{data.warning || getQualityDescription(data.quality)}</p>
              {data.auditInfo && (
                <div className="text-xs text-muted-foreground space-y-1">
                  <p>Source: {data.auditInfo.sourceSystem}</p>
                  <p>Integrity: {data.auditInfo.dataIntegrity}</p>
                  <p>Generated: {data.timestamp.toLocaleString()}</p>
                </div>
              )}
            </div>
          </AlertDescription>
        </Alert>
      )}
      
      {/* Always show audit info for compliance */}
      {data.auditInfo && !shouldShowWarning && (
        <div className="text-xs text-muted-foreground space-y-1">
          <p>Source: {data.auditInfo.sourceSystem}</p>
          <p>Integrity: {data.auditInfo.dataIntegrity}</p>
        </div>
      )}
    </div>
  )
}

/**
 * Data quality summary component for showing multiple quality indicators
 */
export function DataQualitySummary({ 
  qualities, 
  className 
}: { 
  qualities: Array<{ label: string; quality: DataQuality; warning?: string }>
  className?: string
}) {
  const hasIssues = qualities.some(q => q.quality !== 'good')
  
  return (
    <div className={cn("space-y-2", className)}>
      <div className="flex items-center gap-2">
        <h4 className="text-sm font-medium">Data Quality Status</h4>
        {hasIssues && (
          <Badge variant="secondary" className="text-xs">
            {qualities.filter(q => q.quality !== 'good').length} Issues
          </Badge>
        )}
      </div>
      
      <div className="grid gap-2">
        {qualities.map((item, index) => (
          <div key={index} className="flex items-center justify-between py-1">
            <span className="text-sm">{item.label}</span>
            <DataQualityIndicator 
              quality={item.quality}
              warning={item.warning}
              compact
            />
          </div>
        ))}
      </div>
      
      {hasIssues && (
        <Alert variant="default" className="mt-2">
          <Info className="h-4 w-4" />
          <AlertDescription>
            Some data sources are providing simulated or uncertain data. 
            Review quality indicators before making operational decisions.
          </AlertDescription>
        </Alert>
      )}
    </div>
  )
}

/**
 * Hook to extract data quality from service responses
 */
export function useDataQuality<T>(response?: T & { dataQuality?: DataQuality; complianceWarnings?: string[] }) {
  if (!response) {
    return {
      quality: 'unavailable' as DataQuality,
      warnings: ['Data not available'],
      isCompliant: false
    }
  }
  
  const quality = response.dataQuality || 'good'
  const warnings = response.complianceWarnings || []
  const isCompliant = quality === 'good' && warnings.length === 0
  
  return {
    quality,
    warnings,
    isCompliant
  }
}

// Export utility functions for external use
export {
  getQualityIcon,
  getQualityLabel,
  getQualityDescription,
  getQualityBadgeVariant,
  getQualityAlertVariant
}