import React, { useState } from 'react'
import { ChevronUp, ChevronDown, MoreHorizontal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { LoadingSpinner } from './LoadingSpinner'
import type { TableProps, TableColumn, SortConfig } from '@/types'
import { cn } from '@/lib/utils'

/**
 * Industrial DataTable Component
 * Provides consistent table presentation with sorting, pagination, and selection
 * Designed for industrial environments with high contrast and accessibility
 */
export function DataTable<T = any>({
  data,
  columns,
  loading = false,
  error,
  pagination,
  sorting,
  onSort,
  selection,
  rowKey = 'id',
  title,
  description,
  actions,
  className
}: TableProps<T> & {
  title?: string
  description?: string
  actions?: React.ReactNode
  className?: string
}) {
  const [hoveredRow, setHoveredRow] = useState<string | null>(null)

  const getRowKey = (record: T, index: number): string => {
    if (typeof rowKey === 'function') {
      return rowKey(record)
    }
    return String((record as any)[rowKey] || index)
  }

  const handleSort = (field: string) => {
    if (!onSort) return
    onSort(field)
  }

  const handleSelectAll = (checked: boolean) => {
    if (!selection) return
    if (checked) {
      const allKeys = data.map((record, index) => getRowKey(record, index))
      selection.onChange(allKeys, data)
    } else {
      selection.onChange([], [])
    }
  }

  const handleSelectRow = (record: T, index: number, checked: boolean) => {
    if (!selection) return
    const key = getRowKey(record, index)
    let newSelectedKeys = [...selection.selectedRowKeys]
    
    if (checked) {
      newSelectedKeys.push(key)
    } else {
      newSelectedKeys = newSelectedKeys.filter(k => k !== key)
    }
    
    const selectedRecords = data.filter((r, i) => 
      newSelectedKeys.includes(getRowKey(r, i))
    )
    
    selection.onChange(newSelectedKeys, selectedRecords)
  }

  const renderCell = (column: TableColumn<T>, record: T, index: number) => {
    const value = (record as any)[column.key]
    
    if (column.render) {
      return column.render(value, record, index)
    }
    
    // Default rendering based on value type
    if (value === null || value === undefined) {
      return <span className="text-muted-foreground">-</span>
    }
    
    if (typeof value === 'boolean') {
      return (
        <Badge variant={value ? 'default' : 'secondary'}>
          {value ? 'Yes' : 'No'}
        </Badge>
      )
    }
    
    if (value instanceof Date) {
      return (
        <span className="font-mono text-sm">
          {value.toLocaleDateString()} {value.toLocaleTimeString()}
        </span>
      )
    }
    
    return String(value)
  }

  const getSortIcon = (field: string) => {
    if (!sorting || sorting.field !== field) {
      return null
    }
    return sorting.direction === 'asc' ? (
      <ChevronUp className="h-4 w-4 ml-1" />
    ) : (
      <ChevronDown className="h-4 w-4 ml-1" />
    )
  }

  if (error) {
    return (
      <Card className={cn('border-destructive', className)}>
        <CardContent className="p-6">
          <div className="text-center">
            <div className="text-destructive font-medium mb-2">Error Loading Data</div>
            <div className="text-muted-foreground text-sm">{error}</div>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={className}>
      {(title || description || actions) && (
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              {title && <CardTitle>{title}</CardTitle>}
              {description && <CardDescription>{description}</CardDescription>}
            </div>
            {actions && <div className="flex items-center gap-2">{actions}</div>}
          </div>
        </CardHeader>
      )}
      
      <CardContent className="p-0">
        {loading ? (
          <div className="p-8 text-center">
            <LoadingSpinner size="lg" />
            <div className="mt-4 text-muted-foreground">Loading data...</div>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-muted/50 border-b">
                  <tr>
                    {selection && (
                      <th className="p-3 text-left w-12">
                        <input
                          type="checkbox"
                          className="h-4 w-4 rounded border-border"
                          checked={
                            data.length > 0 && 
                            selection.selectedRowKeys.length === data.length
                          }
                          onChange={(e) => handleSelectAll(e.target.checked)}
                          aria-label="Select all rows"
                        />
                      </th>
                    )}
                    
                    {columns.map((column) => (
                      <th
                        key={String(column.key)}
                        className={cn(
                          'p-3 text-left font-medium text-muted-foreground',
                          column.sortable && 'cursor-pointer hover:text-foreground',
                          column.align === 'center' && 'text-center',
                          column.align === 'right' && 'text-right'
                        )}
                        style={{ width: column.width }}
                        onClick={() => column.sortable && handleSort(String(column.key))}
                      >
                        <div className="flex items-center">
                          {column.title}
                          {column.sortable && getSortIcon(String(column.key))}
                        </div>
                      </th>
                    ))}
                    
                    <th className="p-3 w-12">
                      {/* Actions column placeholder */}
                    </th>
                  </tr>
                </thead>
                
                <tbody>
                  {data.length === 0 ? (
                    <tr>
                      <td 
                        colSpan={columns.length + (selection ? 1 : 0) + 1} 
                        className="p-8 text-center text-muted-foreground"
                      >
                        No data available
                      </td>
                    </tr>
                  ) : (
                    data.map((record, index) => {
                      const key = getRowKey(record, index)
                      const isSelected = selection?.selectedRowKeys.includes(key) || false
                      const isHovered = hoveredRow === key
                      
                      return (
                        <tr
                          key={key}
                          className={cn(
                            'border-b transition-colors',
                            'hover:bg-muted/50',
                            isSelected && 'bg-muted/30',
                            isHovered && 'bg-muted/70'
                          )}
                          onMouseEnter={() => setHoveredRow(key)}
                          onMouseLeave={() => setHoveredRow(null)}
                        >
                          {selection && (
                            <td className="p-3">
                              <input
                                type="checkbox"
                                className="h-4 w-4 rounded border-border"
                                checked={isSelected}
                                onChange={(e) => handleSelectRow(record, index, e.target.checked)}
                                aria-label={`Select row ${index + 1}`}
                              />
                            </td>
                          )}
                          
                          {columns.map((column) => (
                            <td
                              key={String(column.key)}
                              className={cn(
                                'p-3',
                                column.align === 'center' && 'text-center',
                                column.align === 'right' && 'text-right'
                              )}
                            >
                              {renderCell(column, record, index)}
                            </td>
                          ))}
                          
                          <td className="p-3">
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-8 w-8 p-0"
                              aria-label="More actions"
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </td>
                        </tr>
                      )
                    })
                  )}
                </tbody>
              </table>
            </div>
            
            {pagination && (
              <div className="border-t p-4 flex items-center justify-between">
                <div className="text-sm text-muted-foreground">
                  Showing {((pagination.current - 1) * pagination.pageSize) + 1} to{' '}
                  {Math.min(pagination.current * pagination.pageSize, pagination.total)} of{' '}
                  {pagination.total} results
                </div>
                
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={pagination.current <= 1}
                    onClick={() => pagination.onChange(pagination.current - 1, pagination.pageSize)}
                  >
                    Previous
                  </Button>
                  
                  <div className="flex items-center gap-1">
                    {Array.from(
                      { length: Math.min(5, pagination.totalPages) },
                      (_, i) => {
                        const page = i + Math.max(1, pagination.current - 2)
                        return (
                          <Button
                            key={page}
                            variant={page === pagination.current ? 'default' : 'outline'}
                            size="sm"
                            className="w-8 h-8 p-0"
                            onClick={() => pagination.onChange(page, pagination.pageSize)}
                          >
                            {page}
                          </Button>
                        )
                      }
                    )}
                  </div>
                  
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={pagination.current >= pagination.totalPages}
                    onClick={() => pagination.onChange(pagination.current + 1, pagination.pageSize)}
                  >
                    Next
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

export default DataTable