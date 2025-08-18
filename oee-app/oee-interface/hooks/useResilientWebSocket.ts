"use client"

import { useEffect, useRef, useState, useCallback } from 'react'
import { ResilientWebSocket, ResilientWebSocketOptions, WebSocketMessage, WebSocketMessageHandler, WebSocketConnectionHandler } from '@/lib/utils/resilientWebSocket'

export interface UseResilientWebSocketOptions extends Partial<ResilientWebSocketOptions> {
  autoConnect?: boolean;
  onMessage?: WebSocketMessageHandler;
  onConnection?: WebSocketConnectionHandler;
  onError?: (error: any) => void;
}

export interface UseResilientWebSocketReturn {
  ws: ResilientWebSocket | null;
  isConnected: boolean;
  isConnecting: boolean;
  reconnectAttempts: number;
  circuitBreakerState: string;
  lastPongTime: Date | null;
  connect: () => Promise<void>;
  disconnect: () => void;
  send: (type: string, data: any) => Promise<void>;
  onMessage: (type: string, handler: WebSocketMessageHandler) => void;
  offMessage: (type: string, handler: WebSocketMessageHandler) => void;
}

/**
 * React hook for managing resilient WebSocket connections
 * Automatically handles reconnection and provides React state integration
 */
export function useResilientWebSocket(
  url: string, 
  options: UseResilientWebSocketOptions = {}
): UseResilientWebSocketReturn {
  const {
    autoConnect = true,
    onMessage,
    onConnection,
    onError,
    ...wsOptions
  } = options;

  const wsRef = useRef<ResilientWebSocket | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isConnecting, setIsConnecting] = useState(false);
  const [reconnectAttempts, setReconnectAttempts] = useState(0);
  const [circuitBreakerState, setCircuitBreakerState] = useState('closed');
  const [lastPongTime, setLastPongTime] = useState<Date | null>(null);

  // Create WebSocket instance
  const createWebSocket = useCallback(() => {
    if (wsRef.current) {
      wsRef.current.disconnect();
    }

    const ws = new ResilientWebSocket({
      url,
      ...wsOptions
    });

    // Set up connection handler
    ws.onConnection((connected) => {
      setIsConnected(connected);
      setIsConnecting(false);
      
      if (onConnection) {
        onConnection(connected);
      }
    });

    // Set up error handler
    ws.onError((error) => {
      console.error('WebSocket error:', error);
      if (onError) {
        onError(error);
      }
    });

    // Set up default message handler if provided
    if (onMessage) {
      ws.onMessage('*', onMessage); // Listen to all message types
    }

    wsRef.current = ws;
    return ws;
  }, [url, onConnection, onError, onMessage, wsOptions]);

  // Connect function
  const connect = useCallback(async () => {
    if (!wsRef.current) {
      createWebSocket();
    }

    if (wsRef.current) {
      setIsConnecting(true);
      try {
        await wsRef.current.connect();
      } catch (error) {
        setIsConnecting(false);
        throw error;
      }
    }
  }, [createWebSocket]);

  // Disconnect function
  const disconnect = useCallback(() => {
    if (wsRef.current) {
      wsRef.current.disconnect();
      setIsConnected(false);
      setIsConnecting(false);
    }
  }, []);

  // Send function
  const send = useCallback(async (type: string, data: any) => {
    if (wsRef.current) {
      await wsRef.current.send(type, data);
    } else {
      throw new Error('WebSocket is not initialized');
    }
  }, []);

  // Message handler management
  const onMessageHandler = useCallback((type: string, handler: WebSocketMessageHandler) => {
    if (wsRef.current) {
      wsRef.current.onMessage(type, handler);
    }
  }, []);

  const offMessageHandler = useCallback((type: string, handler: WebSocketMessageHandler) => {
    if (wsRef.current) {
      wsRef.current.offMessage(type, handler);
    }
  }, []);

  // Status update effect
  useEffect(() => {
    const updateStatus = () => {
      if (wsRef.current) {
        const status = wsRef.current.getStatus();
        setIsConnected(status.connected);
        setIsConnecting(status.connecting);
        setReconnectAttempts(status.reconnectAttempts);
        setCircuitBreakerState(status.circuitBreakerState);
        setLastPongTime(status.lastPongTime);
      }
    };

    // Update status every second
    const statusInterval = setInterval(updateStatus, 1000);
    updateStatus(); // Initial update

    return () => {
      clearInterval(statusInterval);
    };
  }, []);

  // Auto-connect effect
  useEffect(() => {
    if (autoConnect) {
      connect().catch(error => {
        console.error('Auto-connect failed:', error);
      });
    }

    return () => {
      disconnect();
    };
  }, [autoConnect, connect, disconnect]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (wsRef.current) {
        wsRef.current.disconnect();
        wsRef.current = null;
      }
    };
  }, []);

  return {
    ws: wsRef.current,
    isConnected,
    isConnecting,
    reconnectAttempts,
    circuitBreakerState,
    lastPongTime,
    connect,
    disconnect,
    send,
    onMessage: onMessageHandler,
    offMessage: offMessageHandler,
  };
}

/**
 * Hook specifically for OEE data WebSocket connections
 */
export function useOeeWebSocket(deviceId: string, options: UseResilientWebSocketOptions = {}) {
  const wsUrl = process.env.NEXT_PUBLIC_WS_URL || 
    (typeof window !== 'undefined' 
      ? `${window.location.protocol === 'https:' ? 'wss:' : 'ws:'}//${window.location.host}/ws`
      : 'ws://localhost:3000/ws'
    );

  return useResilientWebSocket(`${wsUrl}/oee/${deviceId}`, {
    maxReconnectAttempts: 15,
    reconnectInterval: 2000,
    maxReconnectInterval: 60000,
    heartbeatInterval: 15000,
    circuitBreakerFailureThreshold: 3,
    ...options
  });
}

/**
 * Example usage in a component:
 * 
 * const oeeWebSocket = useOeeWebSocket('device-001', {
 *   onMessage: (message) => {
 *     console.log('Received OEE data:', message);
 *   },
 *   onConnection: (connected) => {
 *     console.log('WebSocket connection status:', connected);
 *   }
 * });
 * 
 * // Send data
 * await oeeWebSocket.send('counter-update', { value: 100, timestamp: new Date() });
 * 
 * // Listen for specific message types
 * useEffect(() => {
 *   const handler = (message) => {
 *     console.log('Stoppage event:', message.data);
 *   };
 *   
 *   oeeWebSocket.onMessage('stoppage-event', handler);
 *   
 *   return () => {
 *     oeeWebSocket.offMessage('stoppage-event', handler);
 *   };
 * }, [oeeWebSocket]);
 */