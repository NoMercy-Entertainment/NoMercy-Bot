import React, {useEffect} from 'react';

import {useWidgetSocket} from './hooks/useWidgetSocket';
import StatusIndicator from './components/StatusIndicator';

import './style.css';

function App() {
    // Initialize WebSocket connection
    const socket = useWidgetSocket('{{WIDGET_ID}}');

    // Example event handlers - customize these for your widget
    const handleEvent = (data: any) => {
        console.log('Received event:', data);
        // Handle your custom events here
    };

    useEffect(() => {
        // Connect to the widget hub
        const connectSocket = async () => {
            await socket.connect();

            // Subscribe to events you need - examples:
            // socket.on('ChatMessage', handleEvent);
            // socket.on('SomeCustomEvent', handleEvent);

            console.log('{{WIDGET_NAME}} widget mounted and connected');
        };

        connectSocket().then();
    }, [socket]);

    return (
        <div className="widget-container">
            {/* Widget Header */}
            <header
                className="flex items-center justify-between mb-6 p-4 bg-neutral-800/50 rounded-lg backdrop-blur-sm">
                <h1 className="text-2xl font-bold text-white">{{WIDGET_NAME}}</h1>
                <StatusIndicator isConnected={socket.isConnected}/>
            </header>

            {/* Main Content Area */}
            <main className="flex-1 p-4">
                {/* Your widget content goes here */}
                <div className="text-center text-neutral-300">
                    <p className="text-lg mb-4">Widget is ready!</p>
                    <p className="text-sm">
                        Connected to widget hub:{' '}
                        <span
                            className={
                                socket.isConnected ? 'text-green-400' : 'text-red-400'
                            }
                        >
							{socket.isConnected ? 'Yes' : 'No'}
						</span>
                    </p>
                </div>
            </main>

            {/* Debug Info (remove in production) */}
            <div className="absolute top-2 right-2 text-xs text-neutral-500 bg-black/20 p-2 rounded">
                Widget ID: {{WIDGET_ID}}
            </div>
        </div>
    );
}

export default App;
