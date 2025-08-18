/**
 * Resilient WebSocket connection with automatic reconnection
 * Designed for industrial environments where network connectivity can be unstable
 * Provides circuit breaker pattern and exponential backoff for reconnections
 */

import { CircuitBreaker, withRetry, RetryOptions } from './errorHandler';

export interface ResilientWebSocketOptions {
  url: string;
  protocols?: string | string[];
  maxReconnectAttempts?: number;
  reconnectInterval?: number;
  maxReconnectInterval?: number;
  reconnectBackoff?: number;
  heartbeatInterval?: number;
  connectionTimeout?: number;
  circuitBreakerFailureThreshold?: number;
  circuitBreakerResetTimeout?: number;
}

export interface WebSocketMessage {
  type: string;
  data: any;
  timestamp: Date;
}

export type WebSocketEventHandler = (event: any) => void;
export type WebSocketMessageHandler = (message: WebSocketMessage) => void;
export type WebSocketConnectionHandler = (connected: boolean) => void;

const DEFAULT_OPTIONS: Required<Omit<ResilientWebSocketOptions, 'url' | 'protocols'>> = {
  maxReconnectAttempts: 10,
  reconnectInterval: 1000,
  maxReconnectInterval: 30000,
  reconnectBackoff: 1.5,
  heartbeatInterval: 30000,
  connectionTimeout: 5000,
  circuitBreakerFailureThreshold: 5,
  circuitBreakerResetTimeout: 60000,
};

export class ResilientWebSocket {
  private options: Required<ResilientWebSocketOptions>;
  private ws: WebSocket | null = null;
  private reconnectAttempts = 0;
  private reconnectTimer: NodeJS.Timeout | null = null;
  private heartbeatTimer: NodeJS.Timeout | null = null;
  private connectionTimer: NodeJS.Timeout | null = null;
  private circuitBreaker: CircuitBreaker;
  private isConnecting = false;
  private isClosing = false;
  private lastPongTime = 0;
  
  // Event handlers
  private messageHandlers = new Map<string, WebSocketMessageHandler[]>();
  private connectionHandlers: WebSocketConnectionHandler[] = [];
  private errorHandlers: WebSocketEventHandler[] = [];

  constructor(options: ResilientWebSocketOptions) {
    this.options = { ...DEFAULT_OPTIONS, ...options };
    this.circuitBreaker = new CircuitBreaker(
      this.options.circuitBreakerFailureThreshold,
      this.options.circuitBreakerResetTimeout
    );
  }

  /**
   * Connect to WebSocket with resilience patterns
   */
  async connect(): Promise<void> {
    if (this.isConnecting || this.isConnected()) {
      return;
    }

    return this.circuitBreaker.execute(async () => {
      await this.performConnection();
    });
  }

  /**
   * Disconnect from WebSocket
   */
  disconnect(): void {
    this.isClosing = true;
    this.stopReconnectTimer();
    this.stopHeartbeat();
    this.stopConnectionTimer();
    
    if (this.ws) {
      this.ws.close(1000, 'Client disconnecting');
      this.ws = null;
    }
    
    this.reconnectAttempts = 0;
    this.notifyConnectionHandlers(false);
  }

  /**
   * Send message through WebSocket with error handling
   */
  async send(type: string, data: any): Promise<void> {
    if (!this.isConnected()) {
      throw new Error('WebSocket is not connected');
    }

    const message: WebSocketMessage = {
      type,
      data,
      timestamp: new Date()
    };

    try {
      this.ws!.send(JSON.stringify(message));
    } catch (error) {
      console.error('Failed to send WebSocket message:', error);
      throw error;
    }
  }

  /**
   * Add message handler for specific message type
   */
  onMessage(type: string, handler: WebSocketMessageHandler): void {
    if (!this.messageHandlers.has(type)) {
      this.messageHandlers.set(type, []);
    }
    this.messageHandlers.get(type)!.push(handler);
  }

  /**
   * Remove message handler
   */
  offMessage(type: string, handler: WebSocketMessageHandler): void {
    const handlers = this.messageHandlers.get(type);
    if (handlers) {
      const index = handlers.indexOf(handler);
      if (index !== -1) {
        handlers.splice(index, 1);
      }
    }
  }

  /**
   * Add connection status handler
   */
  onConnection(handler: WebSocketConnectionHandler): void {
    this.connectionHandlers.push(handler);
  }

  /**
   * Remove connection status handler
   */
  offConnection(handler: WebSocketConnectionHandler): void {
    const index = this.connectionHandlers.indexOf(handler);
    if (index !== -1) {
      this.connectionHandlers.splice(index, 1);
    }
  }

  /**
   * Add error handler
   */
  onError(handler: WebSocketEventHandler): void {
    this.errorHandlers.push(handler);
  }

  /**
   * Remove error handler
   */
  offError(handler: WebSocketEventHandler): void {
    const index = this.errorHandlers.indexOf(handler);
    if (index !== -1) {
      this.errorHandlers.splice(index, 1);
    }
  }

  /**
   * Check if WebSocket is connected
   */
  isConnected(): boolean {
    return this.ws?.readyState === WebSocket.OPEN;
  }

  /**
   * Get connection status information
   */
  getStatus(): {
    connected: boolean;
    connecting: boolean;
    reconnectAttempts: number;
    circuitBreakerState: string;
    lastPongTime: Date | null;
  } {
    return {
      connected: this.isConnected(),
      connecting: this.isConnecting,
      reconnectAttempts: this.reconnectAttempts,
      circuitBreakerState: this.circuitBreaker.getState(),
      lastPongTime: this.lastPongTime > 0 ? new Date(this.lastPongTime) : null
    };
  }

  /**
   * Perform actual WebSocket connection
   */
  private async performConnection(): Promise<void> {
    if (this.isClosing) {
      throw new Error('WebSocket is closing');
    }

    this.isConnecting = true;

    try {
      const connectionPromise = new Promise<void>((resolve, reject) => {
        try {
          this.ws = new WebSocket(this.options.url, this.options.protocols);
          
          // Set up connection timeout
          this.connectionTimer = setTimeout(() => {
            reject(new Error('WebSocket connection timeout'));
          }, this.options.connectionTimeout);

          this.ws.onopen = () => {
            this.stopConnectionTimer();
            this.isConnecting = false;
            this.reconnectAttempts = 0;
            this.startHeartbeat();
            this.notifyConnectionHandlers(true);
            console.log('WebSocket connected successfully');
            resolve();
          };

          this.ws.onclose = (event) => {
            this.stopConnectionTimer();
            this.stopHeartbeat();
            this.isConnecting = false;
            this.notifyConnectionHandlers(false);
            
            if (!this.isClosing) {
              console.warn('WebSocket connection closed:', event.code, event.reason);
              this.scheduleReconnect();
            }
          };

          this.ws.onerror = (error) => {
            this.stopConnectionTimer();
            this.isConnecting = false;
            console.error('WebSocket error:', error);
            this.notifyErrorHandlers(error);
            reject(new Error('WebSocket connection failed'));
          };

          this.ws.onmessage = (event) => {
            this.handleMessage(event);
          };

        } catch (error) {
          this.isConnecting = false;
          reject(error);
        }
      });

      await connectionPromise;

    } catch (error) {
      this.isConnecting = false;
      throw error;
    }
  }

  /**
   * Handle incoming WebSocket messages
   */
  private handleMessage(event: MessageEvent): void {
    try {
      const message: WebSocketMessage = JSON.parse(event.data);
      
      // Handle special system messages
      if (message.type === 'ping') {
        this.send('pong', { timestamp: Date.now() });
        return;
      }
      
      if (message.type === 'pong') {
        this.lastPongTime = Date.now();
        return;
      }

      // Dispatch to registered handlers
      const handlers = this.messageHandlers.get(message.type);
      if (handlers) {
        handlers.forEach(handler => {
          try {
            handler(message);
          } catch (error) {
            console.error('Error in message handler:', error);
          }
        });
      }

    } catch (error) {
      console.error('Failed to parse WebSocket message:', error);
    }
  }

  /**
   * Start heartbeat mechanism
   */
  private startHeartbeat(): void {
    this.stopHeartbeat();
    
    this.heartbeatTimer = setInterval(() => {
      if (this.isConnected()) {
        try {
          this.send('ping', { timestamp: Date.now() });
        } catch (error) {
          console.error('Failed to send heartbeat:', error);
        }
      }
    }, this.options.heartbeatInterval);
  }

  /**
   * Stop heartbeat mechanism
   */
  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  /**
   * Schedule reconnection attempt
   */
  private scheduleReconnect(): void {
    if (this.isClosing || this.reconnectAttempts >= this.options.maxReconnectAttempts) {
      console.error('Max reconnection attempts reached or WebSocket is closing');
      return;
    }

    this.stopReconnectTimer();
    
    // Calculate reconnect delay with exponential backoff
    const delay = Math.min(
      this.options.reconnectInterval * Math.pow(this.options.reconnectBackoff, this.reconnectAttempts),
      this.options.maxReconnectInterval
    );

    console.log(`Scheduling WebSocket reconnect attempt ${this.reconnectAttempts + 1} in ${delay}ms`);

    this.reconnectTimer = setTimeout(async () => {
      this.reconnectAttempts++;
      
      try {
        await this.connect();
      } catch (error) {
        console.error('Reconnect attempt failed:', error);
        // scheduleReconnect will be called by onclose handler
      }
    }, delay);
  }

  /**
   * Stop reconnect timer
   */
  private stopReconnectTimer(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
  }

  /**
   * Stop connection timer
   */
  private stopConnectionTimer(): void {
    if (this.connectionTimer) {
      clearTimeout(this.connectionTimer);
      this.connectionTimer = null;
    }
  }

  /**
   * Notify connection handlers
   */
  private notifyConnectionHandlers(connected: boolean): void {
    this.connectionHandlers.forEach(handler => {
      try {
        handler(connected);
      } catch (error) {
        console.error('Error in connection handler:', error);
      }
    });
  }

  /**
   * Notify error handlers
   */
  private notifyErrorHandlers(error: any): void {
    this.errorHandlers.forEach(handler => {
      try {
        handler(error);
      } catch (error) {
        console.error('Error in error handler:', error);
      }
    });
  }
}

/**
 * Factory function to create resilient WebSocket connections for OEE data
 */
export function createOeeWebSocket(deviceId: string, options: Partial<ResilientWebSocketOptions> = {}): ResilientWebSocket {
  const wsUrl = process.env.NEXT_PUBLIC_WS_URL || 
    (typeof window !== 'undefined' 
      ? `${window.location.protocol === 'https:' ? 'wss:' : 'ws:'}//${window.location.host}/ws`
      : 'ws://localhost:3000/ws'
    );

  return new ResilientWebSocket({
    url: `${wsUrl}/oee/${deviceId}`,
    maxReconnectAttempts: 15, // More attempts for industrial environments
    reconnectInterval: 2000,   // Start with 2 second intervals
    maxReconnectInterval: 60000, // Max 1 minute between attempts
    heartbeatInterval: 15000,  // 15 second heartbeat for quick detection
    circuitBreakerFailureThreshold: 3, // More sensitive for real-time data
    ...options
  });
}

/**
 * Hook for using resilient WebSocket in React components
 */
export function useResilientWebSocket(
  url: string, 
  options: Partial<ResilientWebSocketOptions> = {}
) {
  // This would be implemented as a React hook if needed
  // For now, just export the class and factory function
  return {
    createConnection: () => new ResilientWebSocket({ url, ...options })
  };
}