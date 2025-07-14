<script lang="ts" setup>
import type {Provider} from '@/types/providers.ts';

import useServerClient from '@/lib/clients/useServerClient.ts';
import ProviderLogo from '@/components/icons/ProviderLogo.vue';
import {providerColor} from '@/lib/ui.ts';
import DashboardLayout from '@/layout/DashboardLayout.vue';

const {data: providers, loading} = useServerClient<Provider[]>();
</script>

<template>
  <DashboardLayout
      :description="$t('settings.provider.desc', { provider: '' })"
      :title="$t('settings.provider.title', { provider: '' })"
  >
    <div class="max-w-7xl w-full mx-auto mt-2 mb-4">
      <div v-if="loading" class="text-center text-gray-500 py-12">
        {{ $t('common.loading') }}
      </div>
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-8">
        <RouterLink v-for="provider in providers" :key="provider.id"
                    :style="{ boxShadow: provider.enabled ? `0 0 0 2px ${providerColor(provider.name)[0]}` : '' }"
                    :to="{ name: 'Provider Settings', params: { provider: provider.name.toLowerCase() } }"
                    class="rounded-xl bg-neutral-800 border border-white/10 shadow-lg flex flex-col items-center p-6 cursor-pointer hover:border-theme-500 transition focus:outline-none focus:ring-2 focus:ring-theme-500"
        >
          <div class="flex items-center gap-4 w-full">
            <ProviderLogo
                v-if="provider"
                :provider="provider.name"
                :style="{ filter: provider.enabled ? '' : 'grayscale(1) opacity(0.5)' }"
                class-name="h-12 w-12"
            />
            <div class="flex flex-col flex-1 min-w-0">
              <div class="text-lg font-semibold text-white truncate">
                {{ provider.name }}
              </div>
              <div class="text-xs text-neutral-400 truncate">
                {{ provider.link }}
              </div>
            </div>
            <div class="ml-auto">
							<span v-if="provider.enabled"
                    class="inline-block rounded-full px-3 py-1 text-xs font-semibold bg-theme-700 text-white"
              >
                {{ $t('settings.providers.enabled') }}
              </span>
              <span v-else
                    class="inline-block rounded-full px-3 py-1 text-xs font-semibold bg-neutral-700 text-neutral-400"
              >
                {{ $t('settings.providers.disabled') }}
              </span>
            </div>
          </div>
        </RouterLink>
      </div>
    </div>
  </DashboardLayout>
</template>

<style scoped>
</style>
