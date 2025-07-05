<script lang="ts" setup>
import { onMounted } from 'vue';
import { initializeUserSession } from '@/store/user';

import AppLayout from '@/components/AppLayout.vue';

onMounted(async () => {
	await initializeUserSession();
});
</script>

<template>
	<AppLayout />
</template>

<style lang="scss" scoped>
</style>
