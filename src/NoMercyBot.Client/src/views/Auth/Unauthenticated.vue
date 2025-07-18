<script lang="ts" setup>
import {ref} from 'vue';
import {useRoute} from 'vue-router';

import {routePaths} from '@/router/routes';

import LoadingScreen from '@/layout/LoadingScreen.vue';

const isLoading = ref(false);
const route = useRoute();

function startAuth() {
  isLoading.value = true;
  window.location.href = `/api/oauth/${route.params.provider}/login`;
}
</script>

<template>
  <LoadingScreen>
    <h2 class="text-3xl font-bold tracking-tight text-white">
      {{ $t('auth.login.title') }}
    </h2>
    <p class="text-sm text-neutral-400">
      {{ $t('auth.subtitle') }}
    </p>

    <div class="mt-8 w-3/4 mx-auto">
      <button
          :disabled="isLoading"
          class="group relative flex gap-2 justify-between w-full items-center rounded-md bg-theme-700 px-4 py-3 text-sm font-semibold text-white hover:bg-theme-600 focus:outline-none focus:ring-2 focus:ring-theme-600 focus:ring-offset-2 disabled:opacity-50"
          @click="startAuth"
      >
				<span class="flex items-center">
					<svg class="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
						<path
                d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714Z"
            />
					</svg>
				</span>
        <span class="whitespace-nowrap">
					{{
            isLoading
                ? $t('auth.login.connecting')
                : $t('auth.login.connect')
          }}
				</span>
      </button>
    </div>

    <div class="mt-6 text-center text-sm text-neutral-400">
      {{ $t('auth.login.terms') }}<br>
      <RouterLink :to="routePaths.terms_of_service" class="font-medium text-theme-500 hover:text-theme-300">
        {{ $t('auth.login.termsLink') }}
      </RouterLink>
      {{ $t('auth.login.and') }}
      <RouterLink :to="routePaths.privacy_policy" class="font-medium text-theme-500 hover:text-theme-300">
        {{ $t('auth.login.privacyLink') }}
      </RouterLink>
    </div>
  </LoadingScreen>
</template>
