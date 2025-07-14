<script lang="ts" setup>
import {ref, watch} from 'vue';
import router from "@/router";
import {useRoute} from "vue-router";

const route = useRoute();

const props = defineProps({
  tabs: {
    type: Array as () => { name: string; label: string }[],
    required: true,
  },
  initialTab: {
    type: String,
    default: '',
  },
});

const selectedTab = ref<string>(route.hash?.replace('#tab=', '') || props.initialTab || props.tabs[0].name);

router.afterEach((to, from, next) => {
  const tabName = to.hash.replace('#tab=', '');
  selectedTab.value = tabName || props.initialTab;
});

function selectTab(tabName: string) {
  selectedTab.value = tabName;

  router.replace({ 
    path: route.path,
    hash: `#tab=${tabName}` 
  });
}
</script>

<template>
  <nav class="flex overflow-x-auto py-4 w-full mt-1 border-b border-white/5 bg-neutral-900/50">
    <div
        class="flex min-w-full flex-none gap-x-6 px-4 text-sm/6 font-semibold text-neutral-400 sm:px-6 lg:px-8"
        role="list"
    >
      <button
          v-for="tab in tabs"
          :key="tab.name"
          :class="[selectedTab === tab.name
					? 'border-theme-500 text-theme-600'
					: 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-400',
				]"
          class="group inline-flex items-center border-b-2 px-1 py-4 text-sm font-medium transition-colors duration-100"
          @click="selectTab(tab.name)"
      >
        {{ $t(tab.label) }}
      </button>
    </div>
  </nav>
</template>
