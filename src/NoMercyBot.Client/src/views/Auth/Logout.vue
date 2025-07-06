<script lang="ts" setup>
import { ref } from 'vue';

import AppLogoSquare from '@/components/icons/AppLogoSquare.vue';
import authService from '@/services/authService';
import router from '@/router';

const isLoggingOut = ref(false);

async function handleLogout() {
	isLoggingOut.value = true;
	await authService.logout('twitch');
	await router.push({ name: 'Login', params: { provider: 'twitch' } });
}

function handleCancel() {
	router.back();
}
</script>

<template>
	<div
		class="absolute w-available max-w-lg h-min top-1/2 mx-4 -translate-y-1/2 space-y-8 place-self-center rounded-lg bg-neutral-800 p-8 shadow-lg"
	>
		<div class="text-center flex flex-col gap-6 justify-center items-center">
			<AppLogoSquare class="mx-auto w-24 h-auto" />
			<div class="text-center flex flex-col gap-4 justify-center items-center">
				<h2 class="text-center text-2xl font-bold text-white">
					{{ $t('auth.logout.title') }}
				</h2>
				<p class="text-center text-neutral-400">
					{{ $t('auth.logout.message') }}
				</p>
				<div class="flex gap-4 justify-center">
					<button
						class="cursor-pointer rounded-md bg-neutral-700 px-4 py-2 text-sm font-semibold text-white hover:bg-neutral-600"
						@click="handleCancel"
					>
						{{ $t('auth.logout.cancel') }}
					</button>
					<button
						class="cursor-pointer rounded-md bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-500"
						@click="handleLogout"
					>
						{{ $t('auth.logout.confirm') }}
					</button>
				</div>
			</div>
		</div>
	</div>
</template>

<style scoped>

</style>
