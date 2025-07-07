<script lang="ts" setup>
defineProps({
  enabled: {
    type: Boolean,
    default: false,
  },
  disabled: {
    type: Boolean,
    default: false,
  },

});
</script>

<template>
  <button
      :class="enabled
			? `bg-theme-600 hover:bg-theme-700`
			: `bg-neutral-700 hover:bg-neutral-600`"
      :disabled="disabled"
      class="relative inline-flex h-6 w-11 items-center rounded-full"
  >
		<span
        :class="enabled ? 'translate-x-6' : 'translate-x-1'"
        class="inline-block h-4 w-4 transform rounded-full bg-white transition"
    />
  </button>
</template>
