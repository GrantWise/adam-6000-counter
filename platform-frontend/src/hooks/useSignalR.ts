/**
 * SignalR Hook
 * Provides SignalR connection management and real-time communication
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { authService } from '@/lib/services/authService'

export type ConnectionState = 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting'

interface SignalRContextValue {
  connection: HubConnection | null
  connectionState: ConnectionState
  error: string | null
  isConnected: boolean
  subscribe: (methodName: string, handler: (...args: any[]) => void) => () => void
  invoke: (methodName: string, ...args: any[]) => Promise<any>
}

interface UseSignalROptions {
  hubUrl: string
  automaticReconnect?: boolean
  reconnectDelays?: number[]
  timeoutInMilliseconds?: number
  onConnected?: () => void
  onDisconnected?: (error?: Error) => void
  onReconnecting?: () => void
  onReconnected?: (connectionId?: string) => void
}

export const useSignalR = (options: UseSignalROptions): SignalRContextValue => {
  const {
    hubUrl,
    automaticReconnect = true,
    reconnectDelays = [0, 2000, 10000, 30000],
    timeoutInMilliseconds = 30000,
    onConnected,
    onDisconnected,
    onReconnecting,
    onReconnected
  } = options

  const [connection, setConnection] = useState<HubConnection | null>(null)
  const [connectionState, setConnectionState] = useState<ConnectionState>('Disconnected')
  const [error, setError] = useState<string | null>(null)
  const reconnectTimeoutRef = useRef<NodeJS.Timeout>()
  const subscriptionsRef = useRef<Map<string, (...args: any[]) => void>>(new Map())

  // Create and configure connection
  const createConnection = useCallback(() => {
    const builder = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => authService.getToken() || ''
      })
      .configureLogging(
        process.env.NODE_ENV === 'development' 
          ? LogLevel.Information 
          : LogLevel.Warning
      )

    if (automaticReconnect) {
      builder.withAutomaticReconnect(reconnectDelays)
    }

    if (timeoutInMilliseconds) {
      builder.withServerTimeout(timeoutInMilliseconds)
    }

    return builder.build()
  }, [hubUrl, automaticReconnect, reconnectDelays, timeoutInMilliseconds])

  // Initialize connection
  useEffect(() => {
    const newConnection = createConnection()

    // Connection event handlers
    newConnection.onclose((error) => {
      setConnectionState('Disconnected')
      setError(error?.message || null)
      onDisconnected?.(error || undefined)
      console.warn('SignalR connection closed:', error)
    })

    newConnection.onreconnecting((error) => {
      setConnectionState('Reconnecting')
      setError(error?.message || null)
      onReconnecting?.()
      console.info('SignalR reconnecting:', error)
    })

    newConnection.onreconnected((connectionId) => {
      setConnectionState('Connected')
      setError(null)
      onReconnected?.(connectionId || undefined)
      console.info('SignalR reconnected:', connectionId)
      
      // Re-register existing subscriptions
      subscriptionsRef.current.forEach((handler, methodName) => {
        newConnection.on(methodName, handler)
      })
    })

    setConnection(newConnection)

    return () => {
      newConnection.stop().catch(console.error)
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current)
      }
    }
  }, [createConnection, onConnected, onDisconnected, onReconnecting, onReconnected])

  // Start connection
  useEffect(() => {
    if (connection && connectionState === 'Disconnected') {
      const startConnection = async () => {
        try {
          setConnectionState('Connecting')
          setError(null)
          
          await connection.start()
          
          setConnectionState('Connected')
          onConnected?.()
          console.info('SignalR connection established')
          
          // Register existing subscriptions
          subscriptionsRef.current.forEach((handler, methodName) => {
            connection.on(methodName, handler)
          })
          
        } catch (err) {
          setConnectionState('Disconnected')
          const errorMessage = err instanceof Error ? err.message : 'Connection failed'
          setError(errorMessage)
          console.error('SignalR connection error:', err)
          
          // Retry connection after delay if not using automatic reconnect
          if (!automaticReconnect) {
            reconnectTimeoutRef.current = setTimeout(() => {
              startConnection()
            }, 5000)
          }
        }
      }

      startConnection()
    }
  }, [connection, connectionState, onConnected, automaticReconnect])

  // Subscribe to hub methods
  const subscribe = useCallback((methodName: string, handler: (...args: any[]) => void) => {
    // Store subscription for reconnection
    subscriptionsRef.current.set(methodName, handler)
    
    // Register with active connection
    if (connection && connectionState === 'Connected') {
      connection.on(methodName, handler)
    }

    // Return unsubscribe function
    return () => {
      subscriptionsRef.current.delete(methodName)
      if (connection) {
        connection.off(methodName, handler)
      }
    }
  }, [connection, connectionState])

  // Invoke hub methods
  const invoke = useCallback(async (methodName: string, ...args: any[]) => {
    if (connection && connectionState === 'Connected') {
      try {
        return await connection.invoke(methodName, ...args)
      } catch (error) {
        console.error(`Failed to invoke ${methodName}:`, error)
        throw error
      }
    } else {
      throw new Error(`Cannot invoke ${methodName}: SignalR connection not available`)
    }
  }, [connection, connectionState])

  return {
    connection,
    connectionState,
    error,
    isConnected: connectionState === 'Connected',
    subscribe,
    invoke
  }
}

export default useSignalR