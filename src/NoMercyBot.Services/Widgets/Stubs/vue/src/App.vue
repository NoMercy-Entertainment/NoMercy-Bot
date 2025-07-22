<script setup lang="ts">
import { onMounted } from 'vue';

import { useWidgetSocket } from './hooks/useWidgetSocket';
import StatusIndicator from './components/StatusIndicator.vue';

// Initialize WebSocket connection
const socket = useWidgetSocket('{{WIDGET_ID}}');

// Example event handlers - customize these for your widget
const handleEvent = (data: any) => {
  console.log('Received event:', data);
  // Handle your custom events here
};

onMounted(async () => {
  // Connect to the widget hub
  await socket.connect();

  // Subscribe to events you need - examples:
  // socket.on('ChatMessage', handleEvent);
  // socket.on('SomeCustomEvent', handleEvent);

  console.log('{{WIDGET_NAME}} widget mounted and connected');
});
</script>

<template>
	<div class="widget-container">
		<!-- Widget Header -->
		<header class="flex items-center justify-between mb-6 p-4 bg-neutral-800/50 rounded-lg backdrop-blur-sm">
			<h1 class="text-2xl font-bold text-white">{{WIDGET_NAME}}</h1>
			<StatusIndicator :is-connected="socket.isConnected" />
		</header>

		<!-- Main Content Area -->
		<main class="flex-1 p-4">
			<!-- Your widget content goes here -->
			<div class="text-center text-neutral-300">
				<p class="text-lg mb-4">Widget is ready!</p>
				<p class="text-sm">
					Connected to widget hub: <span :class="socket.isConnected ? 'text-green-400' : 'text-red-400'">
						{{ socket.isConnected ? 'Yes' : 'No' }}
					</span>
				</p>
			</div>
		</main>

		<!-- Debug Info (remove in production) -->
		<div class="absolute top-2 right-2 text-xs text-neutral-500 bg-black/20 p-2 rounded">
			Widget ID: {{WIDGET_ID}}
		</div>
	</div>
</template>
