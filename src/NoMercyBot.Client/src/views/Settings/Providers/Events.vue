<script lang="ts" setup>
import ProviderLogo from '@/components/icons/ProviderLogo.vue';
import { providerColor } from '@/lib/ui.ts';
import useServerClient from '@/lib/clients/useServerClient.ts';
import DashboardLayout from '@/layout/DashboardLayout.vue';

const { data: providers, isLoading } = useServerClient<string[]>({ path: 'settings/events/providers' });
</script>

<template>
	<DashboardLayout
		:description="$t('settings.events.desc', { provider: '' })"
		:title="$t('settings.events.title', { provider: '' })"
	>
		<div class="max-w-7xl w-full mx-auto mt-2 mb-4">
			<div v-if="isLoading" class="text-center text-gray-500 py-12">
				{{ $t('common.loading') }}
			</div>
			<div v-else-if="providers?.length === 0" class="text-center text-gray-500 py-12">
				{{ $t('settings.events.noProviders') }}
			</div>
			<div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-8">
				<RouterLink
					v-for="provider in providers"
					:key="provider"
					:style="{ boxShadow: `0 0 0 2px ${providerColor(provider)[0]}` }"
					:to="{ name: 'Event Provider Settings', params: { provider: provider.toLowerCase() } }"
					class="rounded-xl bg-neutral-800 border border-white/10 shadow-lg flex flex-col items-center p-6 cursor-pointer hover:border-theme-500 transition focus:outline-none focus:ring-2 focus:ring-theme-500"
				>
					<div class="flex items-center gap-4 w-full">
						<ProviderLogo
							v-if="provider"
							:provider="provider"
							class-name="h-12 w-12"
						/>
						<div class="flex flex-col flex-1 min-w-0">
							<div class="text-lg font-semibold text-white truncate">
								{{ provider }}
							</div>
							<div class="text-xs text-neutral-400 truncate">
								{{ $t('settings.events.provider', { provider: provider.toLowerCase() }) }}
							</div>
						</div>
					</div>
				</RouterLink>
			</div>
		</div>
	</DashboardLayout>
</template>

<style scoped>
</style>
