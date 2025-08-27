import { type ClassValue, clsx } from "clsx"
import { twMerge } from "tailwind-merge"

/**
 * Utility function for merging Tailwind CSS classes
 * Combines clsx for conditional classes with tailwind-merge for proper Tailwind CSS class merging
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/**
 * Format numbers for industrial display with appropriate precision
 */
export function formatNumber(
  value: number,
  options: {
    decimals?: number
    unit?: string
    compact?: boolean
  } = {}
): string {
  const { decimals = 2, unit = "", compact = false } = options

  if (compact && Math.abs(value) >= 1000) {
    const units = ['', 'K', 'M', 'B', 'T']
    const unitIndex = Math.floor(Math.log10(Math.abs(value)) / 3)
    const scaledValue = value / Math.pow(1000, unitIndex)
    return `${scaledValue.toFixed(decimals)}${units[unitIndex]}${unit}`
  }

  return `${value.toFixed(decimals)}${unit}`
}

/**
 * Format time duration for display (milliseconds to human readable)
 */
export function formatDuration(milliseconds: number): string {
  const seconds = Math.floor(milliseconds / 1000)
  const minutes = Math.floor(seconds / 60)
  const hours = Math.floor(minutes / 60)
  const days = Math.floor(hours / 24)

  if (days > 0) return `${days}d ${hours % 24}h`
  if (hours > 0) return `${hours}h ${minutes % 60}m`
  if (minutes > 0) return `${minutes}m ${seconds % 60}s`
  return `${seconds}s`
}

/**
 * Format date for industrial display
 */
export function formatDate(
  date: Date | string,
  options: {
    includeTime?: boolean
    compact?: boolean
  } = {}
): string {
  const { includeTime = false, compact = false } = options
  const dateObj = typeof date === 'string' ? new Date(date) : date

  if (compact) {
    return includeTime 
      ? dateObj.toLocaleString('en-US', { 
          month: 'short', 
          day: 'numeric', 
          hour: '2-digit', 
          minute: '2-digit' 
        })
      : dateObj.toLocaleDateString('en-US', { 
          month: 'short', 
          day: 'numeric' 
        })
  }

  return includeTime 
    ? dateObj.toLocaleString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
      })
    : dateObj.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      })
}

/**
 * Get relative time string (e.g., "2 minutes ago")
 */
export function formatRelativeTime(date: Date | string): string {
  const now = new Date()
  const dateObj = typeof date === 'string' ? new Date(date) : date
  const diffMs = now.getTime() - dateObj.getTime()
  const diffSeconds = Math.floor(diffMs / 1000)
  const diffMinutes = Math.floor(diffSeconds / 60)
  const diffHours = Math.floor(diffMinutes / 60)
  const diffDays = Math.floor(diffHours / 24)

  if (diffDays > 7) return formatDate(dateObj)
  if (diffDays > 0) return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`
  if (diffHours > 0) return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`
  if (diffMinutes > 0) return `${diffMinutes} minute${diffMinutes === 1 ? '' : 's'} ago`
  if (diffSeconds > 30) return `${diffSeconds} seconds ago`
  return 'Just now'
}

/**
 * Debounce function for search inputs and API calls
 */
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout

  return (...args: Parameters<T>) => {
    clearTimeout(timeout)
    timeout = setTimeout(() => func(...args), wait)
  }
}

/**
 * Throttle function for frequently updating data
 */
export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number
): (...args: Parameters<T>) => void {
  let inThrottle: boolean

  return (...args: Parameters<T>) => {
    if (!inThrottle) {
      func(...args)
      inThrottle = true
      setTimeout(() => (inThrottle = false), limit)
    }
  }
}

/**
 * Generate a random ID for components
 */
export function generateId(prefix = 'id'): string {
  return `${prefix}-${Math.random().toString(36).substr(2, 9)}`
}

/**
 * Check if a value is not null or undefined
 */
export function isNotNullish<T>(value: T | null | undefined): value is T {
  return value !== null && value !== undefined
}

/**
 * Safe array access with default value
 */
export function safeArrayAccess<T>(
  array: T[] | null | undefined,
  index: number,
  defaultValue: T
): T {
  return array?.[index] ?? defaultValue
}

/**
 * Industrial color utilities for status indication
 */
export const statusColors = {
  healthy: '#16a34a',
  warning: '#ea580c', 
  error: '#dc2626',
  info: '#2563eb',
  neutral: '#6b7280',
  offline: '#6b7280'
} as const

/**
 * Get status color by status name
 */
export function getStatusColor(
  status: keyof typeof statusColors
): string {
  return statusColors[status]
}

/**
 * Download data as file (for exports)
 */
export function downloadFile(
  data: string | Blob,
  filename: string,
  type = 'text/plain'
): void {
  const blob = typeof data === 'string' ? new Blob([data], { type }) : data
  const url = URL.createObjectURL(blob)
  
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

/**
 * Copy text to clipboard
 */
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text)
    return true
  } catch (error) {
    console.error('Failed to copy to clipboard:', error)
    return false
  }
}