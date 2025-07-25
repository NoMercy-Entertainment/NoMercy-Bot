<script lang="ts" setup>
import {computed, ref} from 'vue';
import {useTranslation} from 'i18next-vue';
import {Dialog, DialogPanel, DialogTitle, TransitionChild, TransitionRoot} from '@headlessui/vue';

import {BellIcon, ExclamationTriangleIcon, UserIcon} from '@heroicons/vue/20/solid';
import {ChevronDownIcon} from '@heroicons/vue/16/solid';
import router from '@/router';
import {timezones} from '@/config/timezones.ts';
import authService from '@/services/authService.ts';
import {user} from '@/store/user.ts';
import DashboardLayout from '@/layout/DashboardLayout.vue';

const {t} = useTranslation();

const deleteConfirmDialogOpen = ref(false);
const currentTab = ref('Account');

const secondaryNavigation = computed(() => [
  {name: t('settings.tabs.account'), icon: UserIcon},
  {name: t('settings.tabs.notifications'), icon: BellIcon},
]);

async function handleDeleteAccount() {
  deleteConfirmDialogOpen.value = true;
}

async function confirmDelete() {
  await authService.deleteAccount();
  deleteConfirmDialogOpen.value = false;
  await router.replace({name: 'Login'});
}
</script>

<template>
  <DashboardLayout
      :description="$t('settings.description')"
      :title="$t('settings.title')"
  >
    <!-- Secondary navigation -->
    <nav class="flex overflow-x-auto py-4 w-full border-b border-white/5">
      <div
          class="flex min-w-full flex-none gap-x-6 px-4 text-sm/6 font-semibold text-neutral-400 sm:px-6 lg:px-8"
          role="list"
      >
        <button v-for="item in secondaryNavigation" :key="item.name"
                :aria-current="currentTab ? 'page' : undefined"
                :class="[currentTab === item.name
						? 'border-theme-500 text-theme-600'
						: 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-400']"
                class="group inline-flex items-center border-b-2 px-1 py-4 text-sm font-medium transition-colors duration-100"
                @click="currentTab = item.name"
        >
          <component :is="item.icon"
                     :class="[currentTab == item.name
							? 'text-theme-500'
							: 'text-gray-500 group-hover:text-gray-400',
						]" aria-hidden="true"
                     class="mr-2 -ml-0.5 size-5 transition-colors duration-100"
          />

          {{ item.name }}
        </button>
      </div>
    </nav>

    <template v-if="currentTab === 'Account'">
      <div class="divide-y divide-white/5">
        <div class="grid max-w-7xl grid-cols-1 gap-x-8 gap-y-10 px-4 py-16 sm:px-6 md:grid-cols-3 lg:px-8">
          <div>
            <h2 class="text-base/7 font-semibold text-white">
              {{ $t('settings.personal.title') }}
            </h2>
            <p class="mt-1 text-sm/6 text-neutral-400">
              {{ $t('settings.personal.subtitle') }}
            </p>
          </div>

          <div class="md:col-span-2">
            <div class="grid grid-cols-1 gap-x-6 gap-y-8 sm:max-w-xl sm:grid-cols-6">
              <div class="col-span-full flex items-center gap-x-8">
                <img :src="user.profile_image_url" alt=""
                     class="size-24 flex-none rounded-lg bg-neutral-800 object-cover"
                >
              </div>

              <div class="sm:col-span-3">
                <label class="block text-sm/6 font-medium text-white" for="display-name">
                  {{ $t('settings.personal.displayName') }}
                </label>
                <div class="mt-2">
                  <input id="display-name" :value="user.display_name" autocomplete="display-name"
                         class="block w-full rounded-md bg-white/5 px-3 py-1.5 text-base text-white outline-1 -outline-offset-1 outline-white/10 placeholder:text-neutral-500 focus:outline-2 focus:-outline-offset-2 focus:outline-theme-500 sm:text-sm/6"
                         disabled
                         name="display-name"
                         type="text"
                  >
                </div>
              </div>

              <div class="sm:col-span-3">
                <label class="block text-sm/6 font-medium text-white" for="username">
                  {{ $t('settings.personal.username') }}
                </label>
                <div class="mt-2">
                  <div
                      class="flex items-center rounded-md bg-white/5 pl-3 outline-1 -outline-offset-1 outline-white/10 focus-within:outline-2 focus-within:-outline-offset-2 focus-within:outline-theme-500"
                  >
                    <div class="shrink-0 text-base text-neutral-500 select-none sm:text-sm/6">
                      twitch.tv/
                    </div>
                    <input id="username" :value="user.username"
                           class="block min-w-0 grow bg-transparent py-1.5 pr-3 pl-1 text-base text-white placeholder:text-neutral-500 focus:outline-none sm:text-sm/6"
                           disabled
                           name="username"
                           type="text"
                    >
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="grid max-w-7xl grid-cols-1 gap-x-8 gap-y-10 px-4 py-16 sm:px-6 md:grid-cols-3 lg:px-8">
          <div>
            <h2 class="text-base/7 font-semibold text-white">
              {{ $t('settings.display.title') }}
            </h2>
            <p class="mt-1 text-sm/6 text-neutral-400">
              {{ $t('settings.display.subtitle') }}
            </p>
          </div>

          <form class="md:col-span-2">
            <div class="grid grid-cols-1 gap-x-6 gap-y-8 sm:max-w-xl sm:grid-cols-6">
              <div class="col-span-full">
                <label class="block text-sm/6 font-medium text-white" for="timezone">
                  {{ $t('settings.display.timezone') }}
                </label>
                <div class="mt-2 grid grid-cols-1">
                  <select id="timezone"
                          class="col-start-1 row-start-1 w-full appearance-none rounded-md bg-white/5 py-1.5 pr-8 pl-3 text-base text-white outline-1 -outline-offset-1 outline-white/10 *:bg-neutral-800 focus:outline-2 focus:-outline-offset-2 focus:outline-theme-500 sm:text-sm/6"
                          name="timezone"
                  >
                    <template v-for="timezone in timezones" :key="timezone">
                      <option :selected="timezone.timezone == user.timezone" :value="timezone">
                        {{ timezone.name }}
                      </option>
                    </template>
                  </select>
                  <ChevronDownIcon
                      aria-hidden="true"
                      class="pointer-events-none col-start-1 row-start-1 mr-2 size-5 self-center justify-self-end text-neutral-400 sm:size-4"
                  />
                </div>
              </div>
            </div>

            <div class="mt-8 flex">
              <button
                  class="rounded-md bg-theme-700 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-theme-600 active:bg-theme-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-theme-600"
                  type="submit"
              >
                {{ $t('settings.display.save') }}
              </button>
            </div>
          </form>
        </div>

        <div class="grid max-w-7xl grid-cols-1 gap-x-8 gap-y-10 px-4 py-16 sm:px-6 md:grid-cols-3 lg:px-8">
          <div>
            <h2 class="text-base/7 font-semibold text-white">
              {{ $t('settings.delete.title') }}
            </h2>
            <p class="mt-1 text-sm/6 text-neutral-400">
              {{ $t('settings.delete.subtitle') }}
            </p>
          </div>

          <div class="flex items-start md:col-span-2">
            <button
                class="rounded-md bg-red-700 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-red-600 active:bg-red-800"
                @click="handleDeleteAccount"
            >
              {{ $t('settings.delete.button') }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- Delete confirmation dialog -->
    <TransitionRoot :show="deleteConfirmDialogOpen" as="template">
      <Dialog class="relative z-10" @close="deleteConfirmDialogOpen = false">
        <!-- Dialog backdrop -->
        <TransitionChild as="template" enter="ease-out duration-300" enter-from="opacity-0"
                         enter-to="opacity-100"
                         leave="ease-in duration-200" leave-from="opacity-100" leave-to="opacity-0"
        >
          <div class="fixed inset-0 bg-neutral-950/75 transition-opacity"/>
        </TransitionChild>

        <div class="fixed inset-0 z-10 w-screen overflow-y-auto">
          <div class="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
            <TransitionChild as="template" enter="ease-out duration-300"
                             enter-from="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
                             enter-to="opacity-100 translate-y-0 sm:scale-100" leave="ease-in duration-200"
                             leave-from="opacity-100 translate-y-0 sm:scale-100"
                             leave-to="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
            >
              <DialogPanel
                  class="relative transform overflow-hidden rounded-lg bg-neutral-900 px-4 pb-4 pt-5 text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-lg sm:p-6"
              >
                <div class="sm:flex sm:items-start">
                  <div
                      class="mx-auto flex size-12 shrink-0 items-center justify-center rounded-full bg-red-900/20 sm:mx-0 sm:size-10"
                  >
                    <ExclamationTriangleIcon aria-hidden="true" class="size-6 text-red-600"/>
                  </div>
                  <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left">
                    <DialogTitle as="h3" class="text-base font-semibold text-white">
                      {{ $t('settings.delete.title') }}
                    </DialogTitle>
                    <div class="mt-2">
                      <p class="text-sm text-neutral-400 whitespace-pre-wrap">
                        {{ $t('settings.dialog.subtitle') }}
                      </p>
                    </div>
                  </div>
                </div>
                <div class="mt-5 sm:mt-4 sm:flex sm:flex-row-reverse">
                  <button
                      class="inline-flex w-full justify-center rounded-md bg-red-700 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-red-600 active:bg-red-800 sm:ml-3 sm:w-auto"
                      type="button"
                      @click="confirmDelete"
                  >
                    {{ $t('settings.delete.button') }}
                  </button>
                  <button
                      class="mt-3 inline-flex w-full justify-center rounded-md bg-white/10 px-3 py-2 text-sm font-semibold text-white shadow-xs ring-1 ring-inset ring-white/10 hover:bg-white/20 sm:mt-0 sm:w-auto"
                      type="button"
                      @click="deleteConfirmDialogOpen = false"
                  >
                    {{ $t('settings.dialog.cancel') }}
                  </button>
                </div>
              </DialogPanel>
            </TransitionChild>
          </div>
        </div>
      </Dialog>
    </TransitionRoot>
  </DashboardLayout>
</template>

<style scoped>
</style>
