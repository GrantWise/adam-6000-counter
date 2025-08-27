/**
 * Simple Tooltip Component
 * Basic tooltip implementation for data quality indicators
 */

import React, { useState } from 'react'
import { cn } from '@/lib/utils'

interface TooltipProps {
  children: React.ReactNode
}

interface TooltipTriggerProps {
  asChild?: boolean
  children: React.ReactNode
}

interface TooltipContentProps {
  children: React.ReactNode
  className?: string
}

interface TooltipContextType {
  isOpen: boolean
  setIsOpen: (open: boolean) => void
}

const TooltipContext = React.createContext<TooltipContextType | null>(null)

export function TooltipProvider({ children }: { children: React.ReactNode }) {
  return <>{children}</>
}

export function Tooltip({ children }: TooltipProps) {
  const [isOpen, setIsOpen] = useState(false)
  
  return (
    <TooltipContext.Provider value={{ isOpen, setIsOpen }}>
      <div className="relative inline-block">
        {children}
      </div>
    </TooltipContext.Provider>
  )
}

export function TooltipTrigger({ asChild = false, children }: TooltipTriggerProps) {
  const context = React.useContext(TooltipContext)
  if (!context) throw new Error('TooltipTrigger must be used within Tooltip')
  
  const { setIsOpen } = context
  
  const handleMouseEnter = () => setIsOpen(true)
  const handleMouseLeave = () => setIsOpen(false)
  
  if (asChild && React.isValidElement(children)) {
    return React.cloneElement(children, {
      onMouseEnter: handleMouseEnter,
      onMouseLeave: handleMouseLeave,
      ...children.props,
    } as any)
  }
  
  return (
    <div
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      className="cursor-help"
    >
      {children}
    </div>
  )
}

export function TooltipContent({ children, className }: TooltipContentProps) {
  const context = React.useContext(TooltipContext)
  if (!context) throw new Error('TooltipContent must be used within Tooltip')
  
  const { isOpen } = context
  
  if (!isOpen) return null
  
  return (
    <div
      className={cn(
        "absolute z-50 bottom-full left-1/2 transform -translate-x-1/2 mb-2",
        "px-3 py-2 text-xs text-white bg-gray-900 rounded-md shadow-lg",
        "whitespace-nowrap pointer-events-none",
        "before:content-[''] before:absolute before:top-full before:left-1/2 before:transform before:-translate-x-1/2",
        "before:border-4 before:border-transparent before:border-t-gray-900",
        className
      )}
    >
      {children}
    </div>
  )
}