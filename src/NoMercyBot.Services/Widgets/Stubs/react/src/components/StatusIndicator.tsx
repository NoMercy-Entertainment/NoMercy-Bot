import React from 'react';

interface StatusIndicatorProps {
    isConnected: boolean;
}

const StatusIndicator: React.FC<StatusIndicatorProps> = ({isConnected}) => {
    return (
        <div className="flex items-center gap-2">
            {/* Connection Status */}
            <div className="flex items-center gap-2">
                <div
                    className={`w-2 h-2 rounded-full transition-colors duration-300 ${
                        isConnected ? 'bg-green-500 animate-pulse' : 'bg-red-500'
                    }`}
                />
                <span className="text-sm text-neutral-300">
          {isConnected ? 'Connected' : 'Disconnected'}
        </span>
            </div>
        </div>
    );
};

export default StatusIndicator;
