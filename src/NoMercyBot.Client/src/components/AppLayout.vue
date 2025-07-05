<script lang="ts" setup>
import { computed, ref } from 'vue';
import { RouterLink, RouterView, useRoute } from 'vue-router';
import {
	Dialog,
	DialogPanel,
	Menu,
	MenuButton,
	MenuItem,
	MenuItems,
	TransitionChild,
	TransitionRoot,
} from '@headlessui/vue';
import { Bars3Icon, XMarkIcon } from '@heroicons/vue/24/outline';

import { isInitialized, user } from '@/store/user';
import routes from '@/router/routes';
import LoadingScreen from '@/components/LoadingScreen.vue';
import AppLogoSquare from '@/components/icons/AppLogoSquare.vue';
import Backdrop from '@/components/Backdrop.vue';
import { ChevronDownIcon } from '@heroicons/vue/20/solid';

const route = useRoute();

const sidebarOpen = ref(false);

const navigation = computed(() =>
	routes.filter(route => route.meta?.group === 'main'),
);

const userNavigation = computed(() =>
	routes.filter(route => route.meta?.group === 'profileMenu'),
);

const userDisplayName = computed(() => user.value?.display_name || user.value?.username || '');
const userAvatar = computed(() => user.value?.profile_image_url || '');
</script>

<template>
	<div aria-hidden="true" class="absolute w-available h-available overflow-hidden isolate flex flex-col">
		<Backdrop class="opacity-40" />
		<LoadingScreen v-if="!isInitialized">
			<h2 class="text-3xl font-bold tracking-tight text-white">
				{{ $t('layout.welcomeBack', { name: userDisplayName }) }}
			</h2>
			<div
				class="relative flex h-8 w-8 animate-spin rounded-full border-4 border-solid border-current border-r-transparent align-[-0.125em] motion-reduce:animate-[spin_1.5s_linear_infinite]"
				role="status"
			>
				<span
					class="!absolute !-m-px !h-px !w-px !overflow-hidden !whitespace-nowrap !border-0 !p-0 ![clip:rect(0,0,0,0)]"
				>
					Loading...
				</span>
			</div>
			<p class="mt-2 text-neutral-400">
				{{ $t('auth.validating') }}
			</p>
		</LoadingScreen>

		<template v-else-if="!user?.access_token">
			<RouterView :key="route.path" />
		</template>

		<!-- Show dashboard layout when authenticated -->
		<template v-else>
			<TransitionRoot :show="sidebarOpen" as="template">
				<Dialog class="relative z-50 lg:hidden">
					<TransitionChild as="template" enter="transition-opacity ease-linear duration-300"
						enter-from="opacity-0"
						enter-to="opacity-100" leave="transition-opacity ease-linear duration-300"
						leave-from="opacity-100" leave-to="opacity-0"
					>
						<div class="fixed inset-0 bg-neutral-900/80" />
					</TransitionChild>

					<div class="fixed inset-0 flex">
						<TransitionChild as="template" enter="transition ease-in-out duration-300 transform"
							enter-from="-translate-x-full" enter-to="translate-x-0"
							leave="transition ease-in-out duration-300 transform"
							leave-from="translate-x-0"
							leave-to="-translate-x-full"
						>
							<DialogPanel class="relative mr-16 flex w-full max-w-xs flex-1">
								<TransitionChild as="template" enter="ease-in-out duration-300" enter-from="opacity-0"
									enter-to="opacity-100" leave="ease-in-out duration-300"
									leave-from="opacity-100"
									leave-to="opacity-0"
								>
									<div class="absolute top-0 left-full flex w-16 justify-center pt-5">
										<button class="-m-2.5 p-2.5" type="button" @click="sidebarOpen = false">
											<span class="sr-only">
												{{ $t('layout.closeSidebar') }}
											</span>
											<XMarkIcon aria-hidden="true" class="size-6 text-white" />
										</button>
									</div>
								</TransitionChild>
								<!-- Sidebar component, swap this element with another sidebar if you like -->
								<div
									class="flex grow flex-col gap-y-5 overflow-y-auto bg-neutral-900 px-6 pb-2 ring-1 ring-white/10"
								>
									<div class="flex h-16 shrink-0 items-center">
										<AppLogoSquare class="mt-4 -ml-2 w-16 h-auto" />
									</div>
									<nav class="flex flex-1 flex-col">
										<ul class="flex flex-1 flex-col gap-y-7" role="list">
											<li>
												<ul class="-mx-2 space-y-1" role="list">
													<li v-for="item in navigation" :key="item.name">
														<RouterLink class="group flex gap-x-3 rounded-md p-2 text-sm/6 font-semibold" :class="[
																route.path === item.path
																	? 'bg-neutral-800 text-white'
																	: 'text-neutral-400 hover:bg-neutral-800 hover:text-white',
															]"
															:to="item.path"
															@click="sidebarOpen = false"
														>
															<component :is="item.meta?.icon" aria-hidden="true"
																class="size-6 shrink-0"
															/>
															<span>{{ item.name }}</span>
														</RouterLink>
													</li>
												</ul>
											</li>
										</ul>
									</nav>
								</div>
							</DialogPanel>
						</TransitionChild>
					</div>
				</Dialog>
			</TransitionRoot>

			<!-- Static sidebar for desktop -->
			<aside class="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-72 lg:flex-col">
				<!-- Sidebar component, swap this element with another sidebar if you like -->
				<div class="flex grow flex-col gap-y-5 overflow-y-auto bg-neutral-900 px-6">
					<div class="flex h-16 shrink-0 items-center">
						<AppLogoSquare class="mt-4 -ml-2 w-16 h-auto" />
					</div>
					<nav class="flex flex-1 flex-col">
						<ul class="flex flex-1 flex-col gap-y-7" role="list">
							<li>
								<ul class="-mx-2 space-y-1" role="list">
									<li v-for="item in navigation" :key="item.name">
										<RouterLink class="group flex gap-x-3 rounded-md p-2 text-sm/6 font-semibold" :class="[
											route.path === item.path
												? 'bg-neutral-800 text-white'
												: 'text-neutral-400 hover:bg-neutral-800 hover:text-white',
										]" :to="item.path"
										>
											<component :is="item.meta?.icon" aria-hidden="true"
												class="size-6 shrink-0"
											/>
											{{ item.name }}
										</RouterLink>
									</li>
								</ul>
							</li>
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
										<MenuItem v-for="item in userNavigation" :key="item.name" v-slot="{ active, close }"
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
						</ul>
					</nav>
				</div>
			</aside>

			<div class="sticky top-0 z-40 flex items-center gap-x-6 px-4 py-4 shadow-xs sm:px-6 lg:hidden">
				<button class="-m-2.5 p-2.5 text-neutral-400 lg:hidden" type="button" @click="sidebarOpen = true">
					<span class="sr-only">{{ $t('layout.closeSidebar') }}</span>
					<Bars3Icon aria-hidden="true" class="size-6" />
				</button>
				<div class="flex-1 text-sm/6 font-semibold text-white">
					{{ route?.name }}
				</div>

				<Menu as="div" class="relative">
					<MenuButton class="-m-1.5 flex items-center p-1.5">
						<img :src="userAvatar" alt="" class="size-8 rounded-full bg-neutral-800">
						<span class="hidden lg:flex lg:items-center">
							<span aria-hidden="true" class="ml-4 text-sm/6 font-semibold text-gray-900">Tom Cook</span>
							<ChevronDownIcon aria-hidden="true" class="ml-2 size-5 text-gray-400" />
						</span>
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
							class="absolute right-0 z-10 mt-2.5 w-44 origin-top-right rounded-md bg-white dark:bg-neutral-800  py-2 ring-1 shadow-lg ring-gray-900/5 focus:outline-hidden"
						>
							<MenuItem v-for="item in userNavigation" :key="item.name" v-slot="{ active, close }" as="button"
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
									<component :is="item.meta?.icon" aria-hidden="true" class="size-6 shrink-0" />
									<span>{{ item.name }}</span>
								</RouterLink>
							</MenuItem>
						</MenuItems>
					</transition>
				</Menu>
			</div>

			<main class="lg:pl-72 h-px flex-1 flex flex-col overflow-clip sm:overflow-auto">
				<div class="h-available flex flex-col overflow-auto">
					<RouterView :key="route.path" />
				</div>
			</main>
		</template>
	</div>
</template>
