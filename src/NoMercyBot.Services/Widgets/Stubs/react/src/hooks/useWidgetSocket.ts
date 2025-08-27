import {useEffect, useRef, useState} from 'react';
import {HubConnection, HubConnectionBuilder, LogLevel} from '@microsoft/signalr';

export interface WidgetConnection {
    connection: HubConnection | null;
    isConnected: boolean;
    connect: () => Promise<void>;
    disconnect: () => Promise<void>;
    on: (methodName: string, callback: (...args: any[]) => void) => void;
    off: (methodName: string, callback?: (...args: any[]) => void) => void;
    invoke: (methodName: string, ...args: any[]) => Promise<any>;
}

/**
 * Generic WebSocket hook for widget connections
 * Usage:
 *
 * import { useWidgetSocket } from './hooks/useWidgetSocket';
 *
 * const socket = useWidgetSocket('{{WIDGET_ID}}');
 *
 * // Connect to widget hub
 * await socket.connect();
 *
 * // Listen for events
 * socket.on('ChatMessage', (data) => {
 *   console.log('Received:', data);
 * });
 *
 * // Send messages
 * await socket.invoke('SomeMethod', { data: 'example' });
 */
export function useWidgetSocket(widgetId: string): WidgetConnection {
    const connectionRef = useRef<HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);

    const connect = async () => {
        if (connectionRef.current?.state === 'Connected') {
            return;
        }

        try {
            connectionRef.current = new HubConnectionBuilder()
                .withUrl(`/hubs/widgets?widgetId=${widgetId}`)
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Information)
                .build();

            connectionRef.current.onreconnecting(() => {
                setIsConnected(false);
                console.log('Widget socket reconnecting...');
            });

            connectionRef.current.onreconnected(() => {
                setIsConnected(true);
                console.log('Widget socket reconnected');
            });

            connectionRef.current.onclose(() => {
                setIsConnected(false);
                console.log('Widget socket disconnected');
            });

            console.log('Starting SignalR connection...');
            await connectionRef.current.start();
            console.log('SignalR connection established');

            // Join widget group
            console.log(`Joining widget group for widget: ${widgetId}`);
            await connectionRef.current.invoke('JoinWidgetGroup', widgetId);
            console.log('Successfully joined widget group');

            setIsConnected(true);
            console.log('Widget socket fully connected and ready');
        } catch (error) {
            console.error('Failed to connect to widget socket:', error);
            setIsConnected(false);
        }
    };

    const disconnect = async () => {
        if (connectionRef.current) {
            // Leave widget group before disconnecting
            try {
                await connectionRef.current.invoke('LeaveWidgetGroup', widgetId);
            } catch (error) {
                console.warn('Failed to leave widget group:', error);
            }

            await connectionRef.current.stop();
            connectionRef.current = null;
            setIsConnected(false);
        }
    };

    const on = (methodName: string, callback: (...args: any[]) => void) => {
        connectionRef.current?.on(methodName, callback);
    };

    const off = (methodName: string, callback?: (...args: any[]) => void) => {
        if (callback) {
            connectionRef.current?.off(methodName, callback);
        } else {
            connectionRef.current?.off(methodName);
        }
    };

    const invoke = async (methodName: string, ...args: any[]) => {
        if (connectionRef.current?.state === 'Connected') {
            return await connectionRef.current.invoke(methodName, ...args);
        }
        throw new Error('Connection not established');
    };

    // Auto cleanup on component unmount
    useEffect(() => {
        return () => {
            disconnect();
        };
    }, []);

    return {
        connection: connectionRef.current,
        isConnected,
        connect,
        disconnect,
        on,
        off,
        invoke,
    };
}
