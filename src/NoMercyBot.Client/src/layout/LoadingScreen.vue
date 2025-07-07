<script lang="ts" setup>
  import {useRoute} from 'vue-router';

  import {isInitialized} from '@/store/user.ts';

  import Backdrop from '@/layout/Backdrop.vue';

  import AppLogoSquare from '@/components/icons/AppLogoSquare.vue';
  import ProviderLogo from '@/components/icons/ProviderLogo.vue';

  const route = useRoute();
</script>

<template>
  <div class="flex h-screen items-center justify-center relative overflow-hidden isolate">
    <Backdrop/>
    <div class="w-screen max-w-lg mx-4 z-10 space-y-8 rounded-lg bg-neutral-900 p-8 shadow-lg">
      <div class="text-center flex flex-col gap-6 justify-center items-center">
        <div class="flex gap-4 justify-center items-center h-24">
          <AppLogoSquare class="mx-auto h-16"/>

          <svg v-if="isInitialized" class="w-8" fill="none" viewBox="0 0 45 47"
               xmlns="http://www.w3.org/2000/svg"
          >
            <path
                d="M3.83373 45.8968C1.26016 45.2254 0.141221 42.6518 1.37206 40.1902C2.26721 38.6237 2.491 38.5118 17.261 23.6298C18.6038 22.1752 18.7157 20.8325 17.8205 18.9303C17.3729 18.0351 13.121 10.0906 12.1139 8.41222C10.3236 5.39107 10.8831 2.36993 13.2328 1.36288C17.7086 -0.539317 19.2751 4.27213 23.5271 11.8809C24.8698 14.1188 26.1007 14.2307 27.891 12.4404C29.0099 11.3215 35.8355 3.82456 37.5139 2.59372C39.4161 1.36288 41.7659 1.47478 42.9967 2.81751C45.0108 4.83161 44.3394 6.95759 40.9826 10.762C40.3112 11.4334 34.0452 18.2589 31.1359 21.0563C29.4575 22.8466 29.2337 24.6369 30.2408 26.651L33.5976 33.9241C35.4998 37.8404 34.1571 40.9734 30.8002 41.1972C27.6672 41.3091 26.8839 39.1831 24.1985 33.7003C22.7439 31.1267 21.7368 30.903 19.7227 32.9171C9.54034 43.0994 6.96677 46.9038 3.83373 45.8968Z"
                fill="white"
            />
          </svg>

          <ProviderLogo v-if="isInitialized" :provider="route.params.provider as string" class-name="h-16"/>
        </div>
        <div class="text-center flex flex-col gap-4 justify-center items-center flex-1 w-full">
          <slot/>
        </div>
      </div>
    </div>
  </div>
</template>
