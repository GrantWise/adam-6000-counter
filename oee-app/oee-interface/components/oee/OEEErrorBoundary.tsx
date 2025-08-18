"use client"

import React, { Component, ErrorInfo, ReactNode } from 'react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { AlertTriangle, RotateCcw, Database, Wifi } from 'lucide-react'
import { ErrorHandler } from '@/lib/utils/errorHandler'
import { toast } from '@/hooks/use-toast'

interface Props {
  children: ReactNode
  componentName?: string
  onError?: (error: Error, errorInfo: ErrorInfo) => void
  onRetry?: () => void
}

interface State {
  hasError: boolean
  error?: Error
  errorId: string
  retryCount: number
}

/**
 * OEE-specific Error Boundary Component
 * Handles errors in OEE dashboard components with recovery strategies
 * Provides user-friendly error messages for common OEE scenarios
 */
export class OEEErrorBoundary extends Component<Props, State> {
  private maxRetries = 3

  constructor(props: Props) {
    super(props)
    this.state = { 
      hasError: false,
      errorId: '',
      retryCount: 0
    }
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    const errorId = `OEE-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
    return { 
      hasError: true, 
      error,
      errorId 
    }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error using centralized ErrorHandler with OEE context
    const appError = ErrorHandler.handleCalculationError(error, this.props.componentName || 'unknown')
    
    // Show toast notification
    toast({
      title: "OEE Component Error",
      description: `Error in ${this.props.componentName || 'OEE component'}: ${appError.message}`,
      variant: "destructive",
    })
    
    // Call custom error handler if provided
    if (this.props.onError) {
      this.props.onError(error, errorInfo)
    }

    // Log additional context for OEE components
    console.error('OEE Error Boundary caught an error:', {
      error,
      errorInfo,
      component: this.props.componentName,
      timestamp: new Date().toISOString(),
      errorId: this.state.errorId,
      retryCount: this.state.retryCount
    })
  }

  handleRetry = () => {
    if (this.state.retryCount < this.maxRetries) {
      this.setState(prevState => ({ 
        hasError: false, 
        error: undefined,
        retryCount: prevState.retryCount + 1
      }))
      
      // Call custom retry handler if provided
      if (this.props.onRetry) {
        this.props.onRetry()
      }
      
      toast({
        title: "Retrying...",
        description: `Attempting to recover ${this.props.componentName || 'component'} (${this.state.retryCount + 1}/${this.maxRetries})`,
      })
    }
  }

  handleReset = () => {
    this.setState({ 
      hasError: false, 
      error: undefined, 
      errorId: '',
      retryCount: 0 
    })
  }

  getErrorType = (): 'database' | 'network' | 'calculation' | 'general' => {
    const errorMessage = this.state.error?.message?.toLowerCase() || ''
    
    if (errorMessage.includes('database') || errorMessage.includes('connection')) {
      return 'database'
    }
    if (errorMessage.includes('network') || errorMessage.includes('fetch')) {
      return 'network'
    }
    if (errorMessage.includes('calculation') || errorMessage.includes('oee')) {
      return 'calculation'
    }
    return 'general'
  }

  getErrorIcon = () => {
    const errorType = this.getErrorType()
    switch (errorType) {
      case 'database':
        return <Database className="w-5 h-5" />
      case 'network':
        return <Wifi className="w-5 h-5" />
      default:
        return <AlertTriangle className="w-5 h-5" />
    }
  }

  getErrorTitle = () => {
    const errorType = this.getErrorType()
    const componentName = this.props.componentName || 'OEE Component'
    
    switch (errorType) {
      case 'database':
        return `${componentName} - Database Error`
      case 'network':
        return `${componentName} - Connection Error`
      case 'calculation':
        return `${componentName} - Calculation Error`
      default:
        return `${componentName} - Error`
    }
  }

  getErrorDescription = () => {
    const errorType = this.getErrorType()
    
    switch (errorType) {
      case 'database':
        return 'Unable to connect to the database. Please check your connection and try again.'
      case 'network':
        return 'Network connection issue. Please check your internet connection.'
      case 'calculation':
        return 'Error calculating OEE metrics. This may be due to insufficient data.'
      default:
        return 'An unexpected error occurred while loading this component.'
    }
  }

  render() {
    if (this.state.hasError) {
      const canRetry = this.state.retryCount < this.maxRetries
      
      return (
        <Card className="w-full">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-destructive">
              {this.getErrorIcon()}
              {this.getErrorTitle()}
            </CardTitle>
            <CardDescription>
              {this.getErrorDescription()}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Alert variant="destructive">
              <AlertTriangle className="h-4 w-4" />
              <AlertTitle>Error Details</AlertTitle>
              <AlertDescription className="mt-2">
                <p className="text-sm">{this.state.error?.message}</p>
                <p className="text-xs text-muted-foreground mt-2">
                  Error ID: {this.state.errorId} | Retry attempts: {this.state.retryCount}/{this.maxRetries}
                </p>
              </AlertDescription>
            </Alert>
            
            <div className="flex gap-2">
              {canRetry && (
                <Button onClick={this.handleRetry} variant="default" size="sm">
                  <RotateCcw className="w-4 h-4 mr-2" />
                  Retry ({this.maxRetries - this.state.retryCount} attempts left)
                </Button>
              )}
              <Button onClick={this.handleReset} variant="outline" size="sm">
                Reset Component
              </Button>
            </div>
            
            {!canRetry && (
              <Alert>
                <AlertDescription>
                  Maximum retry attempts reached. Please refresh the page or contact support if the issue persists.
                </AlertDescription>
              </Alert>
            )}
          </CardContent>
        </Card>
      )
    }

    return this.props.children
  }
}