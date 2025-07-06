<script lang="ts" setup>
import { computed } from 'vue';
import { useTranslation } from 'i18next-vue';
import { user } from '@/store/user.ts';
import ProviderLogo from '@/components/icons/ProviderLogo.vue';

const { t } = useTranslation();

const userDisplayName = computed(() => user.value?.display_name || user.value?.username || '');

// Mocked data - replace with real data in production
const stats = [
	{ name: 'Total Streams', value: '24', change: '+10%', changeType: 'positive' },
	{ name: 'Active Hours', value: '145', change: '+24%', changeType: 'positive' },
	{ name: 'New Followers', value: '89', change: '-5%', changeType: 'negative' },
	{ name: 'Total Viewers', value: '2.4k', change: '+35%', changeType: 'positive' },
];

const connectedProviders = computed(() => {
	return [
		{ name: 'Twitch', enabled: true },
		{ name: 'Discord', enabled: true },
		{ name: 'Spotify', enabled: false },
		{ name: 'YouTube', enabled: false },
	];
});
</script>

<template>
	<div class="h-inherit flex flex-col mb-auto w-full">
		<!-- Welcome Header -->
		<header class="border-b border-white/5 w-full sticky top-0">
			<div class="px-8 pt-8 pb-6">
				<h1 class="text-2xl font-bold text-white">
					{{ t('home.welcome', { name: userDisplayName }) }}
				</h1>
				<p class="text-neutral-400 mt-2">
					{{ t('home.subtitle', 'Your stream dashboard overview') }}
				</p>
			</div>
		</header>

		<!-- Dashboard Content -->
		<div class="flex-1 px-8 py-6">
			<!-- Stats Grid -->
			<div class="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
				<div v-for="item in stats" :key="item.name"
					class="overflow-hidden rounded-lg bg-neutral-800 border border-white/10 px-4 py-5 shadow sm:p-6"
				>
					<dt class="truncate text-sm font-medium text-neutral-400">
						{{ item.name }}
					</dt>
					<dd class="mt-1 flex items-baseline justify-between md:block lg:flex">
						<div class="flex items-baseline text-2xl font-semibold text-white">
							{{ item.value }}
						</div>
						<div
							:class="[
								item.changeType === 'positive' ? 'bg-green-900/40 text-green-400' : 'bg-red-900/40 text-red-400',
							]"
							class="inline-flex items-baseline rounded-full px-2.5 py-0.5 text-sm font-medium md:mt-2 lg:mt-0"
						>
							{{ item.change }}
						</div>
					</dd>
				</div>
			</div>

			<!-- Connected Providers -->
			<div class="mt-8">
				<h2 class="text-lg font-medium text-white mb-4">
					{{
						t('home.connectedServices', 'Connected Services')
					}}
				</h2>
				<div class="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5">
					<div v-for="provider in connectedProviders" :key="provider.name"
						class="flex gap-4 justify-center items-center rounded-lg bg-neutral-800 border border-white/10 p-4 shadow-sm hover:border-theme-500 transition"
					>
						<ProviderLogo
							:provider="provider.name"
							:style="{ filter: provider.enabled ? '' : 'grayscale(1) opacity(0.5)' }"
							class-name="h-10 w-10"
						/>
						<div class="flex flex-col flex-1">
							<span class="text-sm text-white">{{ provider.name }}</span>
							<span
								:class="provider.enabled ? 'text-theme-500' : 'text-neutral-500'"
								class="text-xs mt-1"
							>
								{{
									provider.enabled ? t('home.connected', 'Connected') : t('home.disconnected', 'Disconnected')
								}}
							</span>
						</div>
					</div>
				</div>
			</div>

			<!-- Quick Actions -->
			<div class="mt-8">
				<h2 class="text-lg font-medium text-white mb-4">
					{{ t('home.quickActions', 'Quick Actions') }}
				</h2>
				<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-3">
					<div
						class="rounded-lg bg-neutral-800 border border-white/10 p-4 shadow-sm hover:bg-neutral-700/50 hover:border-theme-500 transition cursor-pointer"
					>
						<h3 class="text-white font-medium">
							{{ t('home.startStream', 'Start Stream') }}
						</h3>
						<p class="text-neutral-400 text-sm mt-1">
							{{ t('home.startStreamDesc', 'Go live on your channels') }}
						</p>
					</div>
					<div
						class="rounded-lg bg-neutral-800 border border-white/10 p-4 shadow-sm hover:bg-neutral-700/50 hover:border-theme-500 transition cursor-pointer"
					>
						<h3 class="text-white font-medium">
							{{ t('home.manageAlerts', 'Manage Alerts') }}
						</h3>
						<p class="text-neutral-400 text-sm mt-1">
							{{ t('home.manageAlertsDesc', 'Configure your stream alerts') }}
						</p>
					</div>
					<div
						class="rounded-lg bg-neutral-800 border border-white/10 p-4 shadow-sm hover:bg-neutral-700/50 hover:border-theme-500 transition cursor-pointer"
					>
						<h3 class="text-white font-medium">
							{{ t('home.connectProvider', 'Connect Provider') }}
						</h3>
						<p class="text-neutral-400 text-sm mt-1">
							{{ t('home.connectProviderDesc', 'Add a new service integration') }}
						</p>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<style scoped>
/* Add any specific styles here */
</style>
