<script setup lang="ts">

import {useWidgetSocket} from "@/hooks/useWidgetSocket";

import {widgetId} from "@/stores/config";

const socket = useWidgetSocket(widgetId);
</script>

<template>
  <div class="flex items-center gap-2">
    <!-- Connection Status -->
    <div class="flex items-center gap-2">
      <div
          class="w-2 h-2 rounded-full transition-colors duration-300"
          :class="{
					'bg-green-500 animate-pulse': socket.state.isConnected,
					'bg-red-500': !socket.state.isConnected
				}"
      />
      <span class="text-sm text-neutral-300">
				{{ socket.state.isConnected ? 'Connected' : 'Disconnected' }}
			</span>
    </div>

    <!-- Optional: Show additional status info -->
    <slot name="additional-status" />
  </div>
</template>

<style scoped>
@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.animate-pulse {
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}
</style>
