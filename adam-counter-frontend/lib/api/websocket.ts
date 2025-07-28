import * as signalR from '@microsoft/signalr';
import { AdamDataReading, AdamDeviceHealth } from '@/lib/types/api';

const WS_BASE_URL = process.env.NEXT_PUBLIC_API_URL?.replace('/api', '') || 'http://localhost:5000';

export class CounterDataService {
  private connection: signalR.HubConnection;
  private connectionPromise: Promise<void> | null = null;
  
  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${WS_BASE_URL}/hubs/counter-data`)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Handle reconnection events
    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
    });

    this.connection.onclose(() => {
      console.log('SignalR connection closed');
      this.connectionPromise = null;
    });
  }
  
  async start(): Promise<void> {
    if (this.connectionPromise) {
      return this.connectionPromise;
    }

    this.connectionPromise = this.connection.start()
      .then(() => {
        console.log('SignalR Counter Data Hub connected');
      })
      .catch(err => {
        console.error('SignalR connection error:', err);
        this.connectionPromise = null;
        throw err;
      });

    return this.connectionPromise;
  }

  async stop(): Promise<void> {
    await this.connection.stop();
    this.connectionPromise = null;
  }
  
  onCounterUpdate(callback: (data: AdamDataReading) => void): void {
    this.connection.on('CounterUpdate', callback);
  }
  
  offCounterUpdate(callback: (data: AdamDataReading) => void): void {
    this.connection.off('CounterUpdate', callback);
  }
  
  async subscribeToDevice(deviceId: string): Promise<void> {
    await this.ensureConnected();
    await this.connection.invoke('SubscribeToDevice', deviceId);
  }
  
  async unsubscribeFromDevice(deviceId: string): Promise<void> {
    await this.ensureConnected();
    await this.connection.invoke('UnsubscribeFromDevice', deviceId);
  }

  private async ensureConnected(): Promise<void> {
    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      await this.start();
    }
  }

  get connectionState(): signalR.HubConnectionState {
    return this.connection.state;
  }
}

export class HealthStatusService {
  private connection: signalR.HubConnection;
  private connectionPromise: Promise<void> | null = null;
  
  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${WS_BASE_URL}/hubs/health-status`)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Handle reconnection events
    this.connection.onreconnecting(() => {
      console.log('SignalR Health reconnecting...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR Health reconnected');
    });

    this.connection.onclose(() => {
      console.log('SignalR Health connection closed');
      this.connectionPromise = null;
    });
  }
  
  async start(): Promise<void> {
    if (this.connectionPromise) {
      return this.connectionPromise;
    }

    this.connectionPromise = this.connection.start()
      .then(() => {
        console.log('SignalR Health Status Hub connected');
      })
      .catch(err => {
        console.error('SignalR Health connection error:', err);
        this.connectionPromise = null;
        throw err;
      });

    return this.connectionPromise;
  }

  async stop(): Promise<void> {
    await this.connection.stop();
    this.connectionPromise = null;
  }
  
  onHealthUpdate(callback: (health: AdamDeviceHealth) => void): void {
    this.connection.on('HealthUpdate', callback);
  }
  
  offHealthUpdate(callback: (health: AdamDeviceHealth) => void): void {
    this.connection.off('HealthUpdate', callback);
  }
  
  async subscribeToHealth(): Promise<void> {
    await this.ensureConnected();
    await this.connection.invoke('SubscribeToHealth');
  }

  private async ensureConnected(): Promise<void> {
    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      await this.start();
    }
  }

  get connectionState(): signalR.HubConnectionState {
    return this.connection.state;
  }
}

// Singleton instances
export const counterDataService = new CounterDataService();
export const healthStatusService = new HealthStatusService();