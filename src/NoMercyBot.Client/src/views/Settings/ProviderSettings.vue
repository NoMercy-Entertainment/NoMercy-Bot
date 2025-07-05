<script lang="ts" setup>
import type { Provider } from '@/types/providers.ts';

import useServerClient from '@/lib/clients/useServerClient.ts';

const { data: provider, loading } = useServerClient<Provider>({});
</script>

<template>
	<div class="flex flex-col min-h-available items-center justify-center">
		<div class="w-full max-w-md space-y-8 rounded-lg bg-neutral-800 p-8 shadow-lg">
			Settings Provider View
		</div>
		<div v-if="loading" class="text-center text-gray-500">
			Loading...
		</div>
		<div v-else class="flex flex-col items-center justify-center space-y-4 whitespace-pre">
			{{ provider }}
		</div>
	</div>
</template>

<style scoped>

</style>
