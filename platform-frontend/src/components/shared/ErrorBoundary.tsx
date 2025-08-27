import React, { Component, ErrorInfo, ReactNode } from 'react'
import { AlertTriangle, RefreshCw, Home } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'

interface Props {
  children: ReactNode
  fallback?: ReactNode
  onError?: (error: Error, errorInfo: ErrorInfo) => void
}

interface State {
  hasError: boolean
  error?: Error
  errorInfo?: ErrorInfo
}

/**
 * Error boundary component for graceful error handling
 * Prevents module failures from crashing the entire platform
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Error boundary caught an error:', error, errorInfo)
    
    this.setState({ errorInfo })
    
    // Call error reporting callback if provided
    this.props.onError?.(error, errorInfo)
    
    // In production, send error to monitoring service
    if (import.meta.env.PROD) {
      this.reportError(error, errorInfo)
    }
  }

  private reportError(error: Error, errorInfo: ErrorInfo) {
    // TODO: Implement error reporting service integration
    console.error('Production error:', {
      message: error.message,
      stack: error.stack,
      componentStack: errorInfo.componentStack,
      timestamp: new Date().toISOString()
    })
  }

  private handleRetry = () => {
    this.setState({ hasError: false, error: undefined, errorInfo: undefined })
  }

  private handleGoHome = () => {
    window.location.href = '/'
  }

  render() {
    if (this.state.hasError) {
      // Custom fallback UI
      if (this.props.fallback) {
        return this.props.fallback
      }

      // Default error UI
      return (
        <div className="min-h-screen flex items-center justify-center bg-background p-4">
          <Card className="w-full max-w-2xl">
            <CardHeader>
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-full bg-destructive/10">
                  <AlertTriangle className="w-6 h-6 text-destructive" />
                </div>
                <div>
                  <CardTitle className="text-destructive">
                    Application Error
                  </CardTitle>
                  <CardDescription>
                    Something went wrong with the Industrial ADAM Platform
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            
            <CardContent className="space-y-6">
              <Alert variant="destructive">
                <AlertTriangle className="h-4 w-4" />
                <AlertTitle>Error Details</AlertTitle>
                <AlertDescription>
                  {this.state.error?.message || 'An unexpected error occurred'}
                </AlertDescription>
              </Alert>

              {import.meta.env.DEV && this.state.error && (
                <div className="space-y-3">
                  <h4 className="font-medium text-sm">Stack Trace (Development)</h4>
                  <pre className="text-xs bg-muted p-3 rounded-md overflow-auto max-h-40 font-mono">
                    {this.state.error.stack}
                  </pre>
                  {this.state.errorInfo && (
                    <>
                      <h4 className="font-medium text-sm">Component Stack</h4>
                      <pre className="text-xs bg-muted p-3 rounded-md overflow-auto max-h-40 font-mono">
                        {this.state.errorInfo.componentStack}
                      </pre>
                    </>
                  )}
                </div>
              )}

              <div className="flex gap-3 flex-col sm:flex-row">
                <Button
                  onClick={this.handleRetry}
                  className="flex items-center gap-2"
                >
                  <RefreshCw className="w-4 h-4" />
                  Try Again
                </Button>
                
                <Button
                  variant="outline"
                  onClick={this.handleGoHome}
                  className="flex items-center gap-2"
                >
                  <Home className="w-4 h-4" />
                  Go to Dashboard
                </Button>
              </div>

              <div className="text-sm text-muted-foreground space-y-2">
                <p>
                  If this error persists, please contact your system administrator with the following information:
                </p>
                <ul className="list-disc list-inside space-y-1 text-xs">
                  <li>Time: {new Date().toLocaleString()}</li>
                  <li>Error: {this.state.error?.message}</li>
                  <li>User Agent: {navigator.userAgent}</li>
                  <li>URL: {window.location.href}</li>
                </ul>
              </div>
            </CardContent>
          </Card>
        </div>
      )
    }

    return this.props.children
  }
}

// Functional wrapper for easier use in components
export const withErrorBoundary = <P extends object>(
  Component: React.ComponentType<P>,
  fallback?: ReactNode
) => {
  const WrappedComponent = (props: P) => (
    <ErrorBoundary fallback={fallback}>
      <Component {...props} />
    </ErrorBoundary>
  )
  
  WrappedComponent.displayName = `withErrorBoundary(${Component.displayName || Component.name})`
  
  return WrappedComponent
}