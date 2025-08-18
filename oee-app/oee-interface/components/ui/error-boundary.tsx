"use client"

import React, { Component, ErrorInfo, ReactNode } from 'react'
import { Alert, AlertDescription, AlertTitle } from './alert'
import { Button } from './button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './card'
import { AlertTriangle, RefreshCw } from 'lucide-react'
import { ErrorHandler } from '@/lib/utils/errorHandler'

interface Props {
  children: ReactNode
  fallback?: ReactNode
  onError?: (error: Error, errorInfo: ErrorInfo) => void
}

interface State {
  hasError: boolean
  error?: Error
  errorId: string
}

/**
 * Error Boundary Component
 * Catches JavaScript errors in component tree and displays fallback UI
 * Integrates with ErrorHandler for consistent error logging
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { 
      hasError: false,
      errorId: ''
    }
  }

  static getDerivedStateFromError(error: Error): State {
    // Update state so the next render will show the fallback UI
    const errorId = `ERR-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
    return { 
      hasError: true, 
      error,
      errorId 
    }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error using centralized ErrorHandler
    const appError = ErrorHandler.handleApiError(error, 'component-render')
    
    // Call custom error handler if provided
    if (this.props.onError) {
      this.props.onError(error, errorInfo)
    }

    // Log additional error info
    console.error('Error Boundary caught an error:', {
      error,
      errorInfo,
      componentStack: errorInfo.componentStack,
      errorId: this.state.errorId
    })
  }

  handleReset = () => {
    this.setState({ hasError: false, error: undefined, errorId: '' })
  }

  handleReload = () => {
    window.location.reload()
  }

  render() {
    if (this.state.hasError) {
      // You can render any custom fallback UI
      if (this.props.fallback) {
        return this.props.fallback
      }

      return (
        <Card className="max-w-lg mx-auto mt-8">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-destructive">
              <AlertTriangle className="w-5 h-5" />
              Something went wrong
            </CardTitle>
            <CardDescription>
              An unexpected error occurred. This error has been logged for investigation.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Alert variant="destructive">
              <AlertTriangle className="h-4 w-4" />
              <AlertTitle>Error Details</AlertTitle>
              <AlertDescription className="mt-2">
                <details className="text-sm">
                  <summary className="cursor-pointer hover:underline">
                    Technical Information (Error ID: {this.state.errorId})
                  </summary>
                  <pre className="mt-2 p-2 bg-muted rounded text-xs overflow-auto">
                    {this.state.error?.message}
                    {this.state.error?.stack && (
                      <>
                        {'\n\nStack Trace:\n'}
                        {this.state.error.stack}
                      </>
                    )}
                  </pre>
                </details>
              </AlertDescription>
            </Alert>
            
            <div className="flex gap-2">
              <Button onClick={this.handleReset} variant="outline" size="sm">
                Try Again
              </Button>
              <Button onClick={this.handleReload} variant="default" size="sm">
                <RefreshCw className="w-4 h-4 mr-2" />
                Reload Page
              </Button>
            </div>
            
            <p className="text-xs text-muted-foreground">
              If this error persists, please contact support with Error ID: {this.state.errorId}
            </p>
          </CardContent>
        </Card>
      )
    }

    return this.props.children
  }
}

/**
 * Hook-based error boundary for functional components
 * Usage: const throwError = useErrorHandler()
 * Then: throwError(new Error('Something went wrong'))
 */
export const useErrorHandler = () => {
  return (error: Error) => {
    // Log error using centralized ErrorHandler
    ErrorHandler.handleApiError(error, 'react-hook')
    
    // Re-throw to trigger error boundary
    throw error
  }
}