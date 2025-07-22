import './style.css';
import { WidgetSocket } from './lib/WidgetSocket.js';

// Initialize WebSocket connection
const socket = new WidgetSocket('{{WIDGET_ID}}');

// DOM elements
const statusIndicator = document.getElementById('status-indicator');
const statusText = document.getElementById('status-text');
const connectionStatus = document.getElementById('connection-status');

// Update UI based on connection state
socket.onConnectionStateChanged = (isConnected) => {
  statusIndicator.className = `w-2 h-2 rounded-full transition-colors duration-300 ${
    isConnected ? 'bg-green-500 animate-pulse' : 'bg-red-500'
  }`;
  statusText.textContent = isConnected ? 'Connected' : 'Disconnected';
  connectionStatus.textContent = isConnected ? 'Yes' : 'No';
  connectionStatus.className = isConnected ? 'text-green-400' : 'text-red-400';
};

// Example event handlers - customize these for your widget
const handleEvent = (data) => {
  console.log('Received event:', data);
  // Handle your custom events here
};

// Initialize widget
async function initializeWidget() {
  try {
    // Connect to the widget hub
    await socket.connect();
    
    // Subscribe to events you need - examples:
    // socket.on('ChatMessage', handleEvent);
    // socket.on('SomeCustomEvent', handleEvent);
    
    console.log('{{WIDGET_NAME}} widget mounted and connected');
  } catch (error) {
    console.error('Failed to initialize widget:', error);
  }
}

// Start the widget when DOM is loaded
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initializeWidget);
} else {
  initializeWidget().then();
}

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
  socket.disconnect().then();
});
