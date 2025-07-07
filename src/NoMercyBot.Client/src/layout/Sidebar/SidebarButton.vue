<script lang="ts" setup>
import type {PropType} from 'vue';
import {computed} from 'vue';
import {RouterLink, useRoute} from 'vue-router';

import MoooomIcon from '@/components/icons/MoooomIcon.vue';
import type {MoooomIcons} from '@Icons/icons';

const props = defineProps({
  name: {
    type: String,
    required: true,
  },
  href: {
    type: String,
    required: true,
  },
  show: {
    type: Boolean,
    required: false,
    default: true,
  },
  icon: {
    type: String as PropType<keyof typeof MoooomIcons | undefined>,
    required: false,
  },
});

const route = useRoute();

const classes = computed(() => route.path === props.href
    ? 'bg-neutral-800 text-white'
    : 'text-neutral-400 hover:bg-neutral-800 hover:text-white',
);
</script>

<template>
  <RouterLink :class="classes"
              :to="href"
              class="group flex gap-x-3 rounded-md p-2 text-sm/6 font-semibold"
  >
    <MoooomIcon v-if="!!icon" :icon="icon!" class="size-6 shrink-0"/>
    {{ name }}
  </RouterLink>
</template>

<style scoped>

</style>
