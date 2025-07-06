<script lang="ts" setup>
import type { PropType } from 'vue';
import { computed, ref } from 'vue';
import authService from '@/services/authService';
import { useTranslation } from 'i18next-vue';
import { useRoute } from 'vue-router';
import type { ConfigurationStatus } from '@/types/providers.ts';

const props = defineProps({
	provider: {
		type: String,
		required: true,
	},
	providerConfiguration: {
		type: Object as PropType<ConfigurationStatus>,
		required: true,
	},
});
const emit = defineEmits<{
	(e: 'setup-complete'): void;
}>();
const { t } = useTranslation();
const route = useRoute();

const currentStep = ref('intro');
const clientId = ref(props.providerConfiguration.clientId || '');
const clientSecret = ref(props.providerConfiguration.clientSecret || '');
const isSubmitting = ref(false);

// These are the minimum required scopes for your bot to function properly
const requiredScopes = computed(() => {
	if (props.provider === 'twitch') {
		return [
			'channel:read:subscriptions',
			'moderation:read',
			'channel:moderate',
			'chat:read',
			'chat:edit',
		];
	}
	return [];
});

async function handleSetup() {
	isSubmitting.value = true;

	try {
		const success = await authService.configureProvider(props.provider, {
			clientId: clientId.value,
			clientSecret: clientSecret.value,
			scopes: requiredScopes.value, // Only use the required scopes
		});

		if (success) {
			emit('setup-complete');
		}
	}
	catch (error) {
		console.error('Failed to configure provider:', error);
	}
	finally {
		isSubmitting.value = false;
	}
}

function dynamicT(key: string) {
	return t(key as any);
}
</script>

<template>
	<div class="w-full">
		<!-- Step 1: Introduction Screen -->
		<div v-if="currentStep === 'intro'" class="text-center">
			<h2 class="text-3xl font-bold tracking-tight text-white mb-4">
				{{ dynamicT(`providers.${provider}.welcome`) }}
			</h2>
			<h3 class="text-2xl font-medium text-white mb-4">
				{{ $t('auth.setup.requiredSetup', { provider: provider.toTitleCase() }) }}
			</h3>
			<p class="text-sm text-neutral-400 mb-8">
				{{ $t('auth.setup.setupNeeded', { provider }) }}
			</p>

			<button
				class="py-2 px-6 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-theme-600 hover:bg-theme-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-theme-500"
				@click="currentStep = 'form'"
			>
				{{ $t('auth.setup.proceed') }}
			</button>
		</div>

		<!-- Step 2: Setup Form -->
		<div v-else-if="currentStep === 'form'" class="w-full">
			<form class="space-y-4" @submit.prevent="handleSetup">
				<div>
					<label for="client-id" class="block text-sm font-medium text-white">
						{{ $t('auth.setup.clientId') }}
					</label>
					<input
						id="client-id"
						v-model="clientId"
						type="text"
						class="mt-1 block w-full rounded-md border-gray-600 bg-gray-700 shadow-sm focus:border-theme-500 focus:ring-theme-500 text-white p-2"
						required
						placeholder="Enter your Twitch Client ID"
					>
				</div>

				<div>
					<label for="client-secret" class="block text-sm font-medium text-white">
						{{ $t('auth.setup.clientSecret') }}
					</label>
					<input
						id="client-secret"
						v-model="clientSecret"
						type="password"
						class="mt-1 block w-full rounded-md border-gray-600 bg-gray-700 shadow-sm focus:border-theme-500 focus:ring-theme-500 text-white p-2"
						required
						placeholder="Enter your Twitch Client Secret"
					>
				</div>

				<div class="mt-2 text-xs text-neutral-400">
					<p>{{ $t('auth.setup.scopeInfo') }}</p>
				</div>

				<div class="mt-6 flex space-x-3">
					<button
						type="button"
						class="flex-1 py-2 px-4 border border-gray-600 rounded-md shadow-sm text-sm font-medium text-white bg-gray-800 hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500"
						@click="currentStep = 'intro'"
					>
						{{ $t('common.back') }}
					</button>
					<button
						type="submit"
						class="flex-1 flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-theme-600 hover:bg-theme-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-theme-500"
						:disabled="isSubmitting"
					>
						<span v-if="isSubmitting" class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-solid border-white border-r-transparent align-[-0.125em] mr-2" />
						{{ $t('auth.setup.submit') }}
					</button>
				</div>
			</form>
		</div>
	</div>
</template>
