<script lang="ts" setup>
import type {Provider} from '@/types/providers.ts';

import useServerClient from '@/lib/clients/useServerClient.ts';
import {onMounted, ref} from 'vue';
import {useRoute} from 'vue-router';
import ProviderLogo from '@/components/icons/ProviderLogo.vue';
import DashboardLayout from '@/layout/DashboardLayout.vue';
import TabsNavigation from './components/TabsNavigation.vue';
import EventsTab from './components/EventsTab.vue';
import ConfigurationTab from './components/ConfigurationTab.vue';

const route = useRoute();

const {data: provider, isLoading, refetch} = useServerClient<Provider>({});

const selectedTab = ref('Configuration');
const tabs = [
  {name: 'Configuration', label: 'settings.provider.tabConfig'},
  {name: 'Events', label: 'settings.provider.tabEvents'},
  {name: 'Advanced', label: 'settings.provider.tabAdvanced'},
];

// Set the initial tab based on the query parameter if available
onMounted(() => {
  if (route.query.tab && typeof route.query.tab === 'string') {
    const tabName = route.query.tab;
    // Verify the tab exists before setting it
    if (tabs.some(tab => tab.name === tabName)) {
      selectedTab.value = tabName;
    }
  }
});
</script>

<template>
  <DashboardLayout
      :description="$t('settings.provider.desc', { provider: '' })"
      :icon="ProviderLogo"
      :title="$t('settings.provider.title', { provider: '' })"
      v-bind="{ provider: route.params.provider }"
  >
    <!-- Tabs -->
    <TabsNavigation
        v-model:tab="selectedTab"
        :tabs="tabs"
    />

    <div v-if="selectedTab === 'Configuration'" class="w-full">
      <ConfigurationTab
          v-if="provider"
          :is-loading="isLoading"
          :provider="provider"
          @refetch="refetch"
      />
    </div>
    <div v-else-if="selectedTab === 'Events'" class="w-full px-8 py-12">
      <EventsTab :provider="route.params.provider as string"/>
    </div>
    <div v-else-if="selectedTab === 'Advanced'" class="w-full px-8 py-12">
      <!-- Advanced tab content placeholder -->
      <div class="text-neutral-400">
        {{ $t('settings.provider.advancedComingSoon') }}
      </div>
    </div>
    <div v-if="isLoading" class="text-center text-gray-500 mt-6">
      Loading...
    </div>
  </DashboardLayout>
</template>

<style scoped>
</style>
