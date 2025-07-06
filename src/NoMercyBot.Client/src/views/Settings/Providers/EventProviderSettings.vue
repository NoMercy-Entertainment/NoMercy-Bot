<script lang="ts" setup>
import { ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import type { Event, EventSubscription } from '@/types/eventsub';
import useServerClient from '@/lib/clients/useServerClient.ts';
import { useTranslation } from 'i18next-vue';
import DashboardLayout from '@/layout/DashboardLayout.vue';
import serverClient from '@/lib/clients/serverClient.ts';

const route = useRoute();

const eventTypes = ref<Event[]>([]);
const loading = ref(true);
const saving = ref<string | null>(null);
const { t } = useTranslation();

// Get all existing subscriptions for this provider
const {
	data: subscriptions,
	refetch,
} = useServerClient<Event[]>({ path: `settings/events/${route.params.provider}` });

// Get available event types for this provider
const { data: availableEventTypes } = useServerClient<string[]>({ path: `settings/events/types/${route.params.provider}` });

watch([subscriptions, availableEventTypes], ([subs, types]) => {
	if (!subs || !types)
		return;

	// Merge available event types with existing subscriptions
	eventTypes.value = types.map((type) => {
		const existingSub = subs.find(sub => sub.event_type === type);
		return {
			id: existingSub?.id,
			eventType: type,
			enabled: existingSub?.enabled ?? false,
			provider: route.params.provider,
			callback_url: existingSub?.callback_url,
			created_at: existingSub?.created_at,
			updated_at: existingSub?.updated_at,
			event_type: type,
			expires_at: existingSub?.expires_at,
			metadata: existingSub?.metadata,
			metadata_json: existingSub?.metadata_json,
			subscription_id: existingSub?.subscription_id,
		} as Event;
	});

	loading.value = false;
}, { immediate: true });

async function toggleEvent(eventTypeInfo: Event) {
	saving.value = eventTypeInfo.event_type;

	try {
		if (eventTypeInfo.id) {
			const value = eventTypeInfo.enabled;
			eventTypeInfo.enabled = !eventTypeInfo.enabled;
			// Update existing subscription
			await serverClient()
				.put <EventSubscription>(`/settings/events/${route.params.provider}/${eventTypeInfo.id}`, {
					enabled: !value,
				})
				.then(() => {
					refetch();
				});
		}
		else {
			eventTypeInfo.enabled = !eventTypeInfo.enabled;
			// Create new subscription
			await serverClient()
				.post<EventSubscription>(`/settings/events/${route.params.provider}`, {
					eventType: eventTypeInfo.event_type,
					enabled: true,
				})
				.then(() => {
					refetch();
				});
		}
	}
	catch (error) {
		console.error('Error toggling event subscription:', error);
	}
	finally {
		saving.value = false;
	}
}

async function deleteAllSubscriptions() {
	if (!confirm(t('settings.events.confirmDeleteAll', 'Are you sure you want to delete all event subscriptions for this provider?'))) {
		return;
	}

	saving.value = true;
	try {
		await serverClient()
			.delete(`/settings/events/${route.params.provider}`)
			.then(() => {
				refetch();
			});

		// Reset all event types
		eventTypes.value.forEach((et) => {
			et.id = undefined;
			et.enabled = false;
		});
	}
	catch (error) {
		console.error('Error deleting all subscriptions:', error);
	}
	finally {
		saving.value = null;
	}
}
</script>

<template>
	<DashboardLayout
		:description="$t('settings.events.desc', { provider: route.params.provider })"
		:title="$t('settings.events.title', { provider: route.params.provider })"
		v-bind="{ provider: route.params.provider }"
	>
		<div class="max-w-7xl w-full mx-auto mt-2 mb-4">
			<div v-if="loading" class="text-center text-gray-500 py-12">
				{{ $t('common.loading') }}
			</div>
			<div v-else>
				<div class="flex justify-between items-center mb-6">
					<h2 class="text-xl font-semibold text-white">
						{{ $t('settings.events.availableEvents') }}
					</h2>
					<button
						:disabled="saving || subscriptions?.length === 0"
						class="px-4 py-2 rounded-lg bg-red-900/50 text-red-300 hover:bg-red-800/50 disabled:opacity-50 disabled:cursor-not-allowed"
						@click="deleteAllSubscriptions"
					>
						{{ $t('settings.events.deleteAll') }}
					</button>
				</div>

				<div v-if="eventTypes.length === 0" class="text-center text-gray-500 py-12">
					{{ $t('settings.events.noEvents') }}
				</div>
				<div v-else-if="eventTypes" class="space-y-4">
					<div
						v-for="eventType in eventTypes?.toSorted((a, b) => a.event_type.localeCompare(b.event_type))"
						:key="eventType.id"
						class="p-5 rounded-xl bg-neutral-800 border border-white/10 hover:border-white/20"
					>
						<div class="flex justify-between items-center">
							<div class="flex-1">
								<h3 class="text-lg font-medium text-white">
									{{ eventType.event_type }}
								</h3>
								<p class="text-sm text-neutral-400">
									{{ eventType.description }}
								</p>
							</div>
							<div class="flex items-center">
								<span class="mr-3 text-sm text-neutral-400">
									{{
										saving === eventType.event_type ? $t('common.saving')
										: eventType.enabled
											? $t('common.enabled')
											: $t('common.disabled')
									}}
								</span>
								<button
									:class="eventType.enabled
										? `bg-theme-600 hover:bg-theme-700`
										: `bg-neutral-700 hover:bg-neutral-600`"
									:disabled="saving"
									class="relative inline-flex h-6 w-11 items-center rounded-full"
									@click="toggleEvent(eventType)"
								>
									<span
										:class="eventType.enabled ? 'translate-x-6' : 'translate-x-1'"
										class="inline-block h-4 w-4 transform rounded-full bg-white transition"
									/>
								</button>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>
	</DashboardLayout>
</template>

<style scoped>
/* Use your existing theme color variables here */
</style>
