<script lang="ts" setup>
import { computed, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { Menu, MenuButton, MenuItem, MenuItems } from '@headlessui/vue';

import routes from '@/router/routes.ts';
import { user } from '@/store/user.ts';

import AppLogoSquare from '@/components/icons/AppLogoSquare.vue';
import SidebarButton from '@/layout/Sidebar/SidebarButton.vue';
import { MoooomIcons } from '@Icons/icons';
import SidebarButtonGroup from '@/layout/Sidebar/SidebarButtonGroup.vue';

const sidebarOpen = ref(false);

const navigation = computed(() =>
	routes!.filter(route => route.meta?.group === 'main'),
);
const userNavigation = computed(() =>
	routes!.filter(route => route.meta?.group === 'profileMenu'),
);

const userDisplayName = computed(() => user.value?.display_name || user.value?.username || '');
const userAvatar = computed(() => user.value?.profile_image_url || '');
</script>

<template>
	<aside class="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-72 lg:flex-col">
		<!-- Sidebar component, swap this element with another sidebar if you like -->
		<div class="flex grow flex-col gap-y-5 overflow-y-auto bg-neutral-900 px-6">
			<div class="flex h-16 shrink-0 items-center">
				<AppLogoSquare class="mt-4 -ml-2 w-16 h-auto" />
			</div>
			<nav class="flex flex-1 flex-col text-neutral-400">
				<ul class="flex flex-1 flex-col gap-y-7">
					<li>
						<ul class="-mx-3 space-y-1">
							<li v-for="item in navigation" :key="item.name" class="space-y-1">
								<SidebarButton v-if="!item.meta?.collapsible as boolean"
									:href="item.path"
									:icon="item.meta?.icon as keyof typeof MoooomIcons"
									:name="item.name as string"
								/>

								<SidebarButtonGroup v-else
									:route="item"
								/>
							</li>
						</ul>
					</li>
				</ul>
				<Menu as="div" class="mt-auto -mx-6">
					<MenuButton
						class="flex items-center relative w-full gap-x-4 px-6 py-3 text-sm/6 font-semibold text-white hover:bg-neutral-800"
					>
						<img :src="userAvatar" alt="" class="size-8 rounded-full bg-neutral-800">
						<span class="sr-only">
							{{ $t('layout.yourProfile') }}
						</span>
						<span aria-hidden="true">{{ userDisplayName }}</span>
					</MenuButton>

					<transition
						enter-active-class="transition ease-out duration-200"
						enter-from-class="transform opacity-0 scale-95"
						enter-to-class="transform opacity-100 scale-100"
						leave-active-class="transition ease-in duration-75"
						leave-from-class="transform opacity-100 scale-100"
						leave-to-class="transform opacity-0 scale-95"
					>
						<MenuItems
							class="absolute left-3 z-10 mb-12 w-[90%] origin-bottom-right rounded-md bg-neutral-800 py-1 ring-1 shadow-lg ring-white/10 focus:outline-none sm:-translate-y-[calc(100%+3.5rem)]"
						>
							<MenuItem v-for="item in userNavigation" :key="item.name"
								v-slot="{ active, close }"
								as="button"
								class="w-full"
							>
								<RouterLink
									:class="[
										active ? 'bg-neutral-700 text-white' : 'text-neutral-400',
									]"
									:to="item.path"
									class="flex gap-2 items-center block px-4 py-2 text-sm hover:text-white"
									@click="close"
								>
									<component :is="item.meta?.icon" aria-hidden="true"
										class="size-6 shrink-0"
									/>
									<span>{{ item.name }}</span>
								</RouterLink>
							</MenuItem>
						</MenuItems>
					</transition>
				</Menu>
			</nav>
		</div>
	</aside>
</template>

<style scoped>

</style>
