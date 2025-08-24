import * as signalR from '@microsoft/signalr'
import type { 
  HealthUpdate, 
  ServiceStatus, 
  SystemMetrics, 
  SystemAlert, 
  DatabaseHealth,
  ConnectionStatus 
} from '@/types'

/**
 * SignalR Health Service for real-time system health monitoring
 * Handles SignalR connections for real-time updates from backend
 * Provides automatic reconnection and connection state management
 */
class WebSocketHealthService {
  private connection: signalR.HubConnection | null = null
  private connectionStatus: ConnectionStatus = {
    connected: false,
    reconnecting: false
  }
  
  // Configuration
  private readonly config = {
    url: this.getSignalRUrl(),
    reconnectInterval: 5000,
    maxReconnectAttempts: 10,
    automaticReconnect: true,
  }
  
  // Event listeners
  private eventListeners: Map<string, Array<(data: any) => void>> = new Map()
  private statusListeners: Array<(status: ConnectionStatus) => void> = []
  
  private reconnectAttempts = 0

  /**
   * Initialize and connect to the health SignalR hub
   */
  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return // Already connected
    }

    try {
      this.setConnectionStatus({ connected: false, reconnecting: true })
      
      // Create new SignalR connection
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(this.config.url, {
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000)
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build()
      
      // Set up event handlers
      this.setupEventHandlers()
      
      // Start the connection
      await this.connection.start()
      
      console.log('SignalR health connection established')
      this.setConnectionStatus({ 
        connected: true, 
        reconnecting: false,
        lastConnected: new Date()
      })
      
      // Join health monitoring group
      await this.connection.invoke('JoinGroup', 'HealthMonitoring').catch(console.error)
      
    } catch (error) {
      console.error('Failed to initialize SignalR connection:', error)
      this.setConnectionStatus({ 
        connected: false, 
        reconnecting: false, 
        error: error instanceof Error ? error.message : 'Failed to initialize connection'
      })
      throw error
    }
  }

  /**
   * Disconnect from SignalR hub
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop()
      } catch (error) {
        console.error('Error stopping SignalR connection:', error)
      }
      this.connection = null
    }
    
    this.setConnectionStatus({ connected: false, reconnecting: false })
  }

  /**
   * Subscribe to service status updates
   */
  onServiceStatusUpdate(callback: (status: ServiceStatus) => void): () => void {
    return this.addEventListener('service_status', callback)
  }

  /**
   * Subscribe to system metrics updates
   */
  onMetricsUpdate(callback: (metrics: SystemMetrics) => void): () => void {
    return this.addEventListener('metrics', callback)
  }

  /**
   * Subscribe to new alerts
   */
  onAlertUpdate(callback: (alert: SystemAlert) => void): () => void {
    return this.addEventListener('alert', callback)
  }

  /**
   * Subscribe to database health updates
   */
  onDatabaseHealthUpdate(callback: (health: DatabaseHealth) => void): () => void {
    return this.addEventListener('database_status', callback)
  }

  /**
   * Subscribe to connection status changes
   */
  onConnectionStatusChange(callback: (status: ConnectionStatus) => void): () => void {
    this.statusListeners.push(callback)
    
    // Return unsubscribe function
    return () => {
      const index = this.statusListeners.indexOf(callback)
      if (index >= 0) {
        this.statusListeners.splice(index, 1)
      }
    }
  }

  /**
   * Get current connection status
   */
  getConnectionStatus(): ConnectionStatus {
    return { ...this.connectionStatus }
  }

  /**
   * Request specific data from server
   */
  async requestHealthUpdate(): Promise<void> {
    if (this.isConnected() && this.connection) {
      try {
        await this.connection.invoke('RequestHealthUpdate')
      } catch (error) {
        console.error('Failed to request health update:', error)
      }
    }
  }

  /**
   * Subscribe to specific service updates
   */
  async subscribeToService(serviceName: string): Promise<void> {
    if (this.isConnected() && this.connection) {
      try {
        await this.connection.invoke('SubscribeToService', serviceName)
      } catch (error) {
        console.error('Failed to subscribe to service:', error)
      }
    }
  }

  /**
   * Unsubscribe from specific service updates
   */
  async unsubscribeFromService(serviceName: string): Promise<void> {
    if (this.isConnected() && this.connection) {
      try {
        await this.connection.invoke('UnsubscribeFromService', serviceName)
      } catch (error) {
        console.error('Failed to unsubscribe from service:', error)
      }
    }
  }

  // Private methods

  private getSignalRUrl(): string {
    // In production, this would be configurable
    const protocol = window.location.protocol === 'https:' ? 'https:' : 'http:'
    const host = window.location.hostname
    const port = '5137' // AdminDashboard API port (when available)
    return `${protocol}//${host}:${port}/healthHub`
  }

  private setupEventHandlers(): void {
    if (!this.connection) return
    
    // Connection state events
    this.connection.onreconnecting(() => {
      console.log('SignalR health connection reconnecting...')
      this.setConnectionStatus({ connected: false, reconnecting: true })
    })
    
    this.connection.onreconnected(() => {
      console.log('SignalR health connection reconnected')
      this.setConnectionStatus({ 
        connected: true, 
        reconnecting: false,
        lastConnected: new Date()
      })
    })
    
    this.connection.onclose((error) => {
      console.log('SignalR health connection closed:', error)
      this.setConnectionStatus({ 
        connected: false, 
        reconnecting: false,
        error: error?.message 
      })
    })
    
    // Data update events
    this.connection.on('SystemHealthUpdate', (data) => {
      this.emit('service_status', data)
    })
    
    this.connection.on('SystemMetricsUpdate', (data) => {
      this.emit('metrics', data)
    })
    
    this.connection.on('SystemAlertsUpdate', (data) => {
      this.emit('alert', data)
    })
    
    this.connection.on('DatabaseHealthUpdate', (data) => {
      this.emit('database_status', data)
    })
    
    // Connection will be started in connect() method
  }







  private isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected
  }


  private addEventListener(event: string, callback: (data: any) => void): () => void {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, [])
    }
    
    this.eventListeners.get(event)!.push(callback)
    
    // Return unsubscribe function
    return () => {
      const listeners = this.eventListeners.get(event)
      if (listeners) {
        const index = listeners.indexOf(callback)
        if (index >= 0) {
          listeners.splice(index, 1)
        }
      }
    }
  }

  private emit(event: string, data: any): void {
    const listeners = this.eventListeners.get(event)
    if (listeners) {
      listeners.forEach(listener => {
        try {
          listener(data)
        } catch (error) {
          console.error('Error in WebSocket health event listener:', error)
        }
      })
    }
  }

  private setConnectionStatus(status: Partial<ConnectionStatus>): void {
    this.connectionStatus = { ...this.connectionStatus, ...status }
    
    // Notify status listeners
    this.statusListeners.forEach(listener => {
      try {
        listener(this.getConnectionStatus())
      } catch (error) {
        console.error('Error in connection status listener:', error)
      }
    })
  }
}

// Export singleton instance
export const webSocketHealthService = new WebSocketHealthService()

// Export the class for testing
export { WebSocketHealthService }