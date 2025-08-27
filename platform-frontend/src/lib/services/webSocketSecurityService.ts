import * as signalR from '@microsoft/signalr'
import { authService } from './authService'

/**
 * WebSocket Security Events Service
 * Handles real-time security events and notifications using SignalR
 */
class WebSocketSecurityService {
  private connection: signalR.HubConnection | null = null
  private reconnectAttempts = 0
  private maxReconnectAttempts = 10
  private reconnectInterval = 5000
  private isConnecting = false
  private eventHandlers = new Map<string, Set<Function>>()

  /**
   * Connect to the security events SignalR hub
   */
  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected || this.isConnecting) {
      return
    }

    this.isConnecting = true

    try {
      const token = authService.getStoredToken()
      if (!token) {
        throw new Error('No authentication token available')
      }

      // Create SignalR connection with security hub endpoint
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(this.getSecurityHubUrl(), {
          accessTokenFactory: () => token,
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            return Math.min(this.reconnectInterval * Math.pow(2, retryContext.previousRetryCount), 30000)
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build()

      // Set up connection event handlers
      this.setupConnectionEventHandlers()
      
      // Set up security event handlers
      this.setupSecurityEventHandlers()

      // Start the connection
      await this.connection.start()
      
      console.log('Connected to security SignalR hub')
      this.reconnectAttempts = 0
      this.isConnecting = false
      this.emitToHandlers('connection_status', { connected: true })
      
      // Join security monitoring group
      await this.connection.invoke('JoinSecurityGroup').catch(console.error)
      
    } catch (error) {
      console.error('Failed to connect to security SignalR hub:', error)
      this.isConnecting = false
      throw error
    }
  }

  /**
   * Disconnect from the SignalR hub
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
    this.reconnectAttempts = 0
    this.isConnecting = false
    this.eventHandlers.clear()
  }

  /**
   * Check if the SignalR connection is connected
   */
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected
  }

  /**
   * Get connection status
   */
  getConnectionStatus(): {
    connected: boolean
    connecting: boolean
    reconnectAttempts: number
    maxReconnectAttempts: number
  } {
    return {
      connected: this.isConnected(),
      connecting: this.isConnecting,
      reconnectAttempts: this.reconnectAttempts,
      maxReconnectAttempts: this.maxReconnectAttempts
    }
  }

  /**
   * Subscribe to security events
   */
  on<T = any>(event: string, handler: (data: T) => void): void {
    if (!this.eventHandlers.has(event)) {
      this.eventHandlers.set(event, new Set())
    }
    this.eventHandlers.get(event)!.add(handler)
  }

  /**
   * Unsubscribe from security events
   */
  off<T = any>(event: string, handler: (data: T) => void): void {
    const handlers = this.eventHandlers.get(event)
    if (handlers) {
      handlers.delete(handler)
      if (handlers.size === 0) {
        this.eventHandlers.delete(event)
      }
    }
  }

  /**
   * Invoke a method on the server
   */
  async invoke(methodName: string, ...args: any[]): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke(methodName, ...args)
      } catch (error) {
        console.error(`Failed to invoke ${methodName}:`, error)
        throw error
      }
    } else {
      console.warn('SignalR not connected, cannot invoke method:', methodName)
      throw new Error('SignalR connection not established')
    }
  }

  // Private helper methods

  private getSecurityHubUrl(): string {
    // In production, this should be configurable via environment variables
    const protocol = window.location.protocol === 'https:' ? 'https:' : 'http:'
    const host = process.env.VITE_SECURITY_API_HOST || window.location.hostname
    const port = process.env.VITE_SECURITY_API_PORT || '5139' // Security API port
    return `${protocol}//${host}:${port}/securityHub`
  }

  private setupConnectionEventHandlers(): void {
    if (!this.connection) return

    this.connection.onreconnecting(() => {
      console.log('Security SignalR connection reconnecting...')
      this.isConnecting = true
      this.emitToHandlers('connection_status', { connected: false, reconnecting: true })
    })

    this.connection.onreconnected(() => {
      console.log('Security SignalR connection reconnected')
      this.reconnectAttempts = 0
      this.isConnecting = false
      this.emitToHandlers('connection_status', { connected: true, reconnected: true })
    })

    this.connection.onclose((error) => {
      console.log('Security SignalR connection closed:', error)
      this.emitToHandlers('connection_status', { 
        connected: false, 
        error: error?.message 
      })
    })
  }

  private setupSecurityEventHandlers(): void {
    if (!this.connection) return

    // Security events from SignalR hub
    this.connection.on('SecurityAlert', this.handleSecurityAlert.bind(this))
    this.connection.on('LoginAttempt', this.handleLoginAttempt.bind(this))
    this.connection.on('UserActivity', this.handleUserActivity.bind(this))
    this.connection.on('AuditLogEntry', this.handleAuditLogEntry.bind(this))
    this.connection.on('SessionTerminated', this.handleSessionTerminated.bind(this))
    this.connection.on('SystemEvent', this.handleSystemEvent.bind(this))
  }

  private handleSecurityAlert(data: {
    id: string
    type: 'suspicious_login' | 'brute_force' | 'unusual_activity' | 'data_breach_attempt'
    severity: 'low' | 'medium' | 'high' | 'critical'
    message: string
    details: Record<string, any>
    userId?: string
    ipAddress?: string
    timestamp: string
  }): void {
    console.log('Security alert received:', data)
    this.emitToHandlers('security_alert', data)
  }

  private handleLoginAttempt(data: {
    id: string
    username: string
    ipAddress: string
    userAgent: string
    success: boolean
    failureReason?: string
    timestamp: string
    location?: string
    deviceFingerprint?: string
  }): void {
    console.log('Login attempt received:', data)
    this.emitToHandlers('login_attempt', data)
  }

  private handleUserActivity(data: {
    id: string
    userId: string
    username: string
    action: string
    module: string
    details: string
    ipAddress: string
    timestamp: string
    duration?: number
    success: boolean
  }): void {
    console.log('User activity received:', data)
    this.emitToHandlers('user_activity', data)
  }

  private handleAuditLogEntry(data: {
    id: string
    userId: string
    username: string
    action: string
    entityType: string
    entityId: string
    oldValue?: any
    newValue?: any
    ipAddress: string
    userAgent: string
    timestamp: string
    success: boolean
  }): void {
    console.log('Audit log entry received:', data)
    this.emitToHandlers('audit_log_entry', data)
  }

  private handleSessionTerminated(data: {
    sessionId: string
    userId: string
    username: string
    reason: 'logout' | 'timeout' | 'admin_action' | 'security_violation'
    timestamp: string
  }): void {
    console.log('Session terminated:', data)
    this.emitToHandlers('session_terminated', data)
  }

  private handleSystemEvent(data: {
    id: string
    type: 'maintenance_start' | 'maintenance_end' | 'config_change' | 'service_restart'
    message: string
    details: Record<string, any>
    timestamp: string
  }): void {
    console.log('System event received:', data)
    this.emitToHandlers('system_event', data)
  }

  private emitToHandlers(event: string, data: any): void {
    const handlers = this.eventHandlers.get(event)
    if (handlers) {
      handlers.forEach(handler => {
        try {
          handler(data)
        } catch (error) {
          console.error(`Error in event handler for ${event}:`, error)
        }
      })
    }
  }

  /**
   * Subscribe to specific security alert types
   */
  onSecurityAlert(handler: (alert: {
    id: string
    type: string
    severity: string
    message: string
    details: Record<string, any>
    timestamp: string
  }) => void): void {
    this.on('security_alert', handler)
  }

  /**
   * Subscribe to login attempts
   */
  onLoginAttempt(handler: (attempt: {
    id: string
    username: string
    ipAddress: string
    success: boolean
    failureReason?: string
    timestamp: string
  }) => void): void {
    this.on('login_attempt', handler)
  }

  /**
   * Subscribe to user activity events
   */
  onUserActivity(handler: (activity: {
    id: string
    userId: string
    username: string
    action: string
    module: string
    details: string
    timestamp: string
    success: boolean
  }) => void): void {
    this.on('user_activity', handler)
  }

  /**
   * Subscribe to audit log entries
   */
  onAuditLogEntry(handler: (entry: {
    id: string
    userId: string
    username: string
    action: string
    entityType: string
    entityId: string
    timestamp: string
    success: boolean
  }) => void): void {
    this.on('audit_log_entry', handler)
  }

  /**
   * Subscribe to connection status changes
   */
  onConnectionStatus(handler: (status: {
    connected: boolean
    reason?: string
    reconnected?: boolean
  }) => void): void {
    this.on('connection_status', handler)
  }

  /**
   * Subscribe to system events
   */
  onSystemEvent(handler: (event: {
    id: string
    type: string
    message: string
    details: Record<string, any>
    timestamp: string
  }) => void): void {
    this.on('system_event', handler)
  }

  /**
   * Request real-time security dashboard data
   */
  async requestDashboardData(): Promise<void> {
    await this.invoke('RequestDashboardData')
  }

  /**
   * Request user session data
   */
  async requestUserSessions(userId?: string): Promise<void> {
    await this.invoke('RequestUserSessions', userId)
  }

  /**
   * Request audit trail updates
   */
  async requestAuditTrailUpdates(filters?: {
    userId?: string
    action?: string
    entityType?: string
    startDate?: Date
    endDate?: Date
  }): Promise<void> {
    await this.invoke('RequestAuditTrailUpdates', filters)
  }
}

// Export singleton instance
export const webSocketSecurityService = new WebSocketSecurityService()

// Export the class for testing
export { WebSocketSecurityService }