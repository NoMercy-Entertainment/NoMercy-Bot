<script lang="ts" setup>
import { computed, ref, watch } from 'vue';
import type { Event, EventSubscription } from '@/types/eventsub';
import serverClient from '@/lib/clients/serverClient.ts';
import useServerClient from '@/lib/clients/useServerClient.ts';
import Checkbox from '@/components/Checkbox.vue';

const props = defineProps({
	provider: {
		type: String,
		required: true,
	},
});

const events = ref<Event[]>([]);
const saving = ref<string | null>(null);

const {
	data: subscriptions,
	isLoading,
	refetch,
} = useServerClient<Event[]>({ path: `settings/events/${props.provider}` });

watch(subscriptions, (value) => {
	if (!value)
		return;

	events.value = value;
}, { immediate: true });

async function toggleEvent(eventInfo: Event) {
	saving.value = eventInfo.event_type;

	try {
		if (eventInfo.id) {
			const value = eventInfo.enabled;
			eventInfo.enabled = !eventInfo.enabled;
			// Update existing subscription
			await serverClient()
				.put<EventSubscription>(`/settings/events/${props.provider}/${eventInfo.id}`, {
					enabled: !value,
				})
				.then(() => {
					refetch();
				});
		}
		else {
			eventInfo.enabled = !eventInfo.enabled;
			// Create new subscription
			await serverClient()
				.post<EventSubscription>(`/settings/events/${props.provider}`, {
					event: eventInfo.event_type,
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
		saving.value = null;
	}
}

const allEvents = computed(() => events.value.every(event => event.enabled));

function toggleAllEvent() {
	// Determine target state - if all are enabled, disable all; otherwise enable all
	const targetState = !allEvents.value;
	saving.value = 'all';

	// Prepare the array of events to update
	const eventsToUpdate = events.value.map(event => ({
		id: event.id,
		enabled: targetState,
	}));

	// Update local UI state immediately for responsiveness
	events.value.forEach((event) => {
		event.enabled = targetState;
	});

	// Use the batch update endpoint with the array of events
	serverClient()
		.put(`/settings/events/${props.provider}`, eventsToUpdate)
		.then(() => {
			// Refresh data to ensure UI is in sync with server
			refetch();
		})
		.catch((error) => {
			console.error('Error toggling all events:', error);
			// Revert UI state on error
			events.value.forEach((event) => {
				event.enabled = !targetState;
			});
		})
		.finally(() => {
			saving.value = null;
		});
}
</script>

<template>
	<div class="max-w-7xl w-full mx-auto">
		<div v-if="isLoading" class="text-center text-gray-500 py-12">
			{{ $t('common.loading') }}
		</div>
		<div v-else>
			<div class="flex justify-between items-center mb-6">
				<h2 class="text-xl font-semibold text-white">
					{{ $t('settings.events.availableEvents') }}
				</h2>

				<div class="flex items-center gap-2 ml-auto mr-2">
					<span class="text-sm text-neutral-400">
						{{ allEvents ? $t('common.disableAll') : $t('common.enableAll') }}
					</span>
					<Checkbox
						:disabled="!!saving || events.length === 0"
						:enabled="allEvents"
						@click="toggleAllEvent"
					/>
				</div>
			</div>

			<div v-if="events.length === 0" class="text-center text-gray-500 py-12">
				{{ $t('settings.events.noEvents') }}
			</div>
			<div v-else-if="events" class="space-y-4">
				<div
					v-for="event in events?.toSorted((a, b) => a.event_type.localeCompare(b.event_type))"
					:key="event.id"
					class="p-5 rounded-xl bg-neutral-800 border border-white/10 hover:border-white/20"
				>
					<div class="flex justify-between items-center">
						<div class="flex-1">
							<h3 class="text-lg font-medium text-white">
								{{ event.event_type }}
							</h3>
							<p class="text-sm text-neutral-400">
								{{ event.description }}
							</p>
						</div>
						<div class="flex items-center">
							<span class="mr-3 text-sm text-neutral-400">
								{{
									saving === event.event_type ? $t('common.saving')
									: event.enabled
										? $t('common.enabled')
										: $t('common.disabled')
								}}
							</span>
							<Checkbox :data-enabled="event.enabled" :data-type="event.event_type" :disabled="!!saving"
								:enabled="event.enabled"
								@click="toggleEvent(event)"
							/>
						</div>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>
