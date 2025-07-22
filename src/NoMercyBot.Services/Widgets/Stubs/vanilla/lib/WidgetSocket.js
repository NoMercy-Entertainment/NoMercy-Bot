import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

/**
 * Generic WebSocket class for widget connections
 * Usage:
 * 
 * import { WidgetSocket } from './lib/WidgetSocket.js';
 * 
 * const socket = new WidgetSocket('{{WIDGET_ID}}');
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
export class WidgetSocket {
	constructor(widgetId) {
		this.widgetId = widgetId;
		this.connection = null;
		this.isConnected = false;
		this.eventHandlers = new Map();
	}

	async connect() {
		if (this.connection?.connectionState === 'Connected') {
			return;
		}

		try {
			this.connection = new HubConnectionBuilder()
				.withUrl(`/hubs/widgets?widgetId=${this.widgetId}`)
				.withAutomaticReconnect()
				.configureLogging(LogLevel.Information)
				.build();

			this.connection.onreconnecting(() => {
				this.isConnected = false;
				this.onConnectionStateChanged?.(false);
				console.log('Widget socket reconnecting...');
			});

			this.connection.onreconnected(() => {
				this.isConnected = true;
				this.onConnectionStateChanged?.(true);
				console.log('Widget socket reconnected');
			});

			this.connection.onclose(() => {
				this.isConnected = false;
				this.onConnectionStateChanged?.(false);
				console.log('Widget socket disconnected');
			});

			console.log('Starting SignalR connection...');
			await this.connection.start();
			console.log('SignalR connection established');

			// Join widget group
			console.log(`Joining widget group for widget: ${this.widgetId}`);
			await this.connection.invoke('JoinWidgetGroup', this.widgetId);
			console.log('Successfully joined widget group');
			
			this.isConnected = true;
			this.onConnectionStateChanged?.(true);
			console.log('Widget socket fully connected and ready');
		}
		catch (error) {
			console.error('Failed to connect to widget socket:', error);
			this.isConnected = false;
			this.onConnectionStateChanged?.(false);
		}
	}

	async disconnect() {
		if (this.connection) {
			// Leave widget group before disconnecting
			try {
				await this.connection.invoke('LeaveWidgetGroup', this.widgetId);
			}
			catch (error) {
				console.warn('Failed to leave widget group:', error);
			}
			
			await this.connection.stop();
			this.connection = null;
			this.isConnected = false;
			this.onConnectionStateChanged?.(false);
		}
	}

	on(methodName, callback) {
		this.connection?.on(methodName, callback);
		
		// Store handler for cleanup
		if (!this.eventHandlers.has(methodName)) {
			this.eventHandlers.set(methodName, []);
		}
		this.eventHandlers.get(methodName).push(callback);
	}

	off(methodName, callback = null) {
		if (callback) {
			this.connection?.off(methodName, callback);
			
			// Remove from stored handlers
			const handlers = this.eventHandlers.get(methodName) || [];
			const index = handlers.indexOf(callback);
			if (index > -1) {
				handlers.splice(index, 1);
			}
		}
		else {
			this.connection?.off(methodName);
			this.eventHandlers.delete(methodName);
		}
	}

	async invoke(methodName, ...args) {
		if (this.connection?.connectionState === 'Connected') {
			return await this.connection.invoke(methodName, ...args);
		}
		throw new Error('Connection not established');
	}

	// Optional callback for connection state changes
	onConnectionStateChanged = null;
}
