<script lang="ts" setup>
import type {PropType} from 'vue';

import {useRoute} from 'vue-router';

defineProps({
  disabled: {
    type: Boolean,
    required: false,
    default: false,
  },
  text: {
    type: String,
    required: true,
  },
  theme: {
    type: Array as PropType<string[]>,
    required: true,
  },
  icon: {
    type: String,
    required: true,
  },
});

const route = useRoute();
</script>

<template>
  <button
      :disabled="disabled"
      :style="`
            color: ${theme[1]};
            --theme: ${theme[0]};
            --color-theme-50: hsl(from var(--theme) h 100% 92.2%);
            --color-theme-300: hsl(from var(--theme) h 100% 78.4%);
            --color-theme-400: hsl(from var(--theme) h 100% 73.7%);
            --color-theme-500: hsl(from var(--theme) h 100% 66.7%);
            --color-theme-600: hsl(from var(--theme) h 100% 59.4%);
            --color-theme-700: hsl(from var(--theme) h 100% 52.2%);
            --color-theme-800: hsl(from var(--theme) h 100% 44.9%);
            --color-logo-back: hsl(from var(--theme) h s calc(l / 1.3));
            background: ${theme[0] === 'back' ? 'rgb(39 39 39)' : ''};
        `"
      class="group relative flex w-full justify-center rounded-md bg-theme-600 px-4 py-3 text-sm font-semibold hover:bg-theme-500 focus:outline-none focus:ring-2 focus:ring-theme-500 focus:ring-offset-2 disabled:opacity-50"
  >
		<span class="absolute inset-y-0 left-0 flex items-center pl-3">
			<svg class="h-6 w-6" fill="currentColor"
           viewBox="0 0 24 24"
      >
				<path :d="icon"/>
			</svg>
		</span>
    <span>{{ text }}</span>
  </button>
</template>

<style scoped>

</style>
