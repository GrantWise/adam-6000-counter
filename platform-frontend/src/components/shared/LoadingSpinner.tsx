import React from 'react'
import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg' | 'xl'
  className?: string
  text?: string
}

/**
 * Industrial-styled loading spinner component
 * Used throughout the platform for loading states
 */
export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'md',
  className,
  text
}) => {
  const sizeClasses = {
    sm: 'w-4 h-4',
    md: 'w-6 h-6',
    lg: 'w-8 h-8', 
    xl: 'w-12 h-12'
  }

  const textSizeClasses = {
    sm: 'text-sm',
    md: 'text-base',
    lg: 'text-lg',
    xl: 'text-xl'
  }

  return (
    <div className={cn('flex flex-col items-center gap-3', className)}>
      <Loader2 
        className={cn(
          'animate-spin text-primary',
          sizeClasses[size]
        )}
      />
      {text && (
        <p className={cn(
          'text-muted-foreground font-medium',
          textSizeClasses[size]
        )}>
          {text}
        </p>
      )}
    </div>
  )
}

// Skeleton loading component for content placeholders
export const LoadingSkeleton: React.FC<{
  className?: string
  count?: number
}> = ({ className, count = 1 }) => {
  return (
    <div className="space-y-3">
      {Array.from({ length: count }, (_, i) => (
        <div
          key={i}
          className={cn(
            'animate-pulse bg-muted rounded-md h-4',
            className
          )}
        />
      ))}
    </div>
  )
}

// Full page loading component
export const PageLoader: React.FC<{ message?: string }> = ({ 
  message = 'Loading...' 
}) => {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <LoadingSpinner size="lg" text={message} />
    </div>
  )
}