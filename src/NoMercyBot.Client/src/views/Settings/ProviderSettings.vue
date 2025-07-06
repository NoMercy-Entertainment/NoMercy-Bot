<script lang="ts" setup>
import type { Provider } from '@/types/providers.ts';

import useServerClient from '@/lib/clients/useServerClient.ts';
import { ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useTranslation } from 'i18next-vue';
import ProviderLogo from '@/components/icons/ProviderLogo.vue';
import serverClient from '@/lib/clients/serverClient.ts';
import { useSessionStorage } from '@vueuse/core';

const { t } = useTranslation();
const router = useRouter();
const redirect = useSessionStorage('redirect', '/home');

const { data: provider, isLoading, refetch } = useServerClient<Provider>({});

const form = ref<Provider | null>(null);
const isSaving = ref(false);
const error = ref('');
const scopesString = ref('');

const selectedTab = ref('Configuration');
const tabs = [
	{ name: 'Configuration', label: t('settings.provider.tabConfig') },
	{ name: 'Advanced', label: t('settings.provider.tabAdvanced') },
];

watch(provider, (val) => {
	if (val)
		form.value = { ...val };
}, { immediate: true });

watch(form, (val) => {
	if (val && Array.isArray(val.scopes)) {
		scopesString.value = val.scopes.join(', ');
	}
}, { immediate: true });

watch(scopesString, (val) => {
	if (form.value) {
		form.value.scopes = val.split(',').map(s => s.trim()).filter(Boolean);
	}
});

watch(() => form.value?.enabled, () => {
	// eslint-disable-next-line no-self-assign
	isSaving.value = isSaving.value;
});

async function save() {
	if (!form.value)
		return;
	isSaving.value = true;
	error.value = '';
	try {
		await serverClient()
			.put(`/settings/providers/${form.value.name.toLowerCase()}`, form.value);
		await refetch();

		if (form.value.enabled) {
			redirect.value = router.currentRoute.value.fullPath;
			await router.push({
				name: 'Login',
				params: { provider: form.value.name.toLowerCase() },
			});
		}
		else {
			router.back();
		}
	}
	catch (e: any) {
		error.value = e?.message || 'Failed to save changes.';
	}
	finally {
		isSaving.value = false;
	}
}

function cancel() {
	router.back();
}

function toggleScope(scope: string) {
	if (!form.value)
		return;
	const scopes = form.value.scopes || [];
	if (scopes.includes(scope)) {
		form.value.scopes = scopes.filter(s => s !== scope);
	}
	else {
		form.value.scopes = [...scopes, scope];
	}
}
</script>

<template>
	<div class="h-inherit flex flex-col mb-auto w-full">
		<header class="border-b border-white/5 w-full">
			<h1 class="text-base/7 font-semibold text-white px-8 pt-4 pb-2">
				{{ t('settings.provider.title') }}
			</h1>
			<p class="text-neutral-400 px-8 pb-6">
				{{ t('settings.provider.desc') }}
			</p>
		</header>

		<!-- Tabs -->
		<nav class="flex overflow-x-auto py-4 w-full border-b border-white/5 bg-neutral-900/50">
			<div class="flex min-w-full flex-none gap-x-6 px-4 text-sm/6 font-semibold text-neutral-400 sm:px-6 lg:px-8"
				role="list"
			>
				<button v-for="tab in tabs" :key="tab.name"
					:class="[selectedTab === tab.name ? 'border-theme-500 text-theme-600' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-400']"
					class="group inline-flex items-center border-b-2 px-1 py-4 text-sm font-medium transition-colors duration-100"
					@click="selectedTab = tab.name"
				>
					{{ tab.label }}
				</button>
			</div>
		</nav>

		<div v-if="selectedTab === 'Configuration'" class="w-full">
			<div class="w-full grid grid-cols-1 md:grid-cols-3 gap-x-8 gap-y-10 px-8 py-12">
				<div>
					<h2 class="text-base/7 font-semibold text-white">
						{{ t('settings.provider.configTitle') }}
					</h2>
					<p class="mt-1 text-sm/6 text-neutral-400">
						{{ t('settings.provider.configSubtitle') }}
					</p>
				</div>

				<form v-if="form" class="md:col-span-2 space-y-8" @submit.prevent="save">
					<div class="grid grid-cols-1 gap-x-6 gap-y-8 sm:max-w-3xl sm:grid-cols-6">
						<div class="col-span-full flex items-center gap-x-8">
							<div class="size-16 flex-none rounded-lg  flex items-center justify-center">
								<ProviderLogo :provider="form.name" class-name="h-16" />
							</div>
							<div>
								<div class="text-lg font-semibold text-white">
									{{ form.name }}
								</div>
							</div>
						</div>

						<div class="sm:col-span-3">
							<label class="block text-sm/6 font-medium text-white">
								{{ t('settings.provider.clientId') }}
							</label>
							<div class="mt-2">
								<input v-model="form.clientId"
									:placeholder="t('settings.provider.clientIdPlaceholder')"
									autocomplete="off"
									class="block w-full rounded-md bg-white/5 px-3 py-1.5 text-base text-white outline-1 -outline-offset-1 outline-white/10 placeholder:text-neutral-500 focus:outline-2 focus:-outline-offset-2 focus:outline-theme-500 sm:text-sm/6"
								>
							</div>
						</div>

						<div class="sm:col-span-3">
							<label class="block text-sm/6 font-medium text-white">
								{{ t('settings.provider.clientSecret') }}
							</label>
							<div class="mt-2">
								<input v-model="form.clientSecret"
									:placeholder="t('settings.provider.clientSecretPlaceholder')"
									autocomplete="off"
									class="block w-full rounded-md bg-white/5 px-3 py-1.5 text-base text-white outline-1 -outline-offset-1 outline-white/10 placeholder:text-neutral-500 focus:outline-2 focus:-outline-offset-2 focus:outline-theme-500 sm:text-sm/6"
									type="password"
								>
							</div>
						</div>

						<div class="col-span-full">
							<label class="block text-sm/6 font-medium text-white">{{
								t('settings.provider.scopes')
							}}</label>
							<div
								class="mt-2 grid grid-cols-1 gap-4 max-h-64 w-full overflow-y-auto p-2 bg-neutral-900/60 rounded-md border border-white/10"
							>
								<div v-for="(desc, scope) in form.availableScopes" :key="scope"
									class="flex items-center gap-2 min-w-0"
								>
									<input
										:id="`scope-${scope}`"
										:checked="form.scopes.includes(scope)"
										class="accent-theme-500 h-5 w-5 mt-1 rounded"
										type="checkbox"
										@change="toggleScope(scope)"
									>
									<label :for="`scope-${scope}`"
										class="text-sm text-white select-none flex flex-col min-w-0"
									>
										<span :title="scope" class="font-mono truncate">{{ scope }}</span>
										<span :title="desc" class="ml-0.5 text-xs text-neutral-400 truncate">
											{{ desc }}
										</span>
									</label>
								</div>
							</div>
							<p class="mt-1 text-xs text-neutral-400">
								{{ t('settings.provider.scopesHelp') }}
							</p>
						</div>

						<div class="col-span-full flex items-center gap-4 mt-4">
							<input id="enabled" v-model="form.enabled" class="accent-theme-500 h-5 w-5 rounded"
								type="checkbox"
							>
							<label class="text-sm font-medium text-white select-none" for="enabled">
								{{ t('settings.provider.enabled') }}
							</label>
						</div>

						<div class="col-span-full flex gap-3 mt-8">
							<button :disabled="isSaving || !form.clientId || !form.clientSecret"
								class="rounded-md bg-theme-700 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-theme-600 active:bg-theme-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-theme-600 flex-1"
								type="submit"
							>
								{{
									isSaving
										? t('common.saving')
										: form.enabled
											? t('settings.provider.saveAndLogin')
											: t('settings.provider.save')
								}}
							</button>
							<button :disabled="isSaving"
								class="rounded-md bg-white/10 px-3 py-2 text-sm font-semibold text-white shadow-xs ring-1 ring-inset ring-white/10 hover:bg-white/20 flex-1"
								type="button"
								@click="cancel"
							>
								{{ t('common.cancel') }}
							</button>
						</div>
						<div v-if="error" class="text-red-400 text-sm mt-2 col-span-full">
							{{ error }}
						</div>
					</div>
				</form>
				<div v-else class="md:col-span-2 text-center text-gray-500 py-12">
					{{ t('settings.provider.noData') }}
				</div>
			</div>
		</div>
		<div v-else-if="selectedTab === 'Advanced'" class="w-full px-8 py-12">
			<!-- Advanced tab content placeholder -->
			<div class="text-neutral-400">
				{{ t('settings.provider.advancedComingSoon') }}
			</div>
		</div>
		<div v-if="isLoading" class="text-center text-gray-500 mt-6">
			Loading...
		</div>
	</div>
</template>

<style scoped>

</style>
