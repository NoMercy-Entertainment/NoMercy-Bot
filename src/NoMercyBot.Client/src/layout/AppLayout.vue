<script lang="ts" setup>
import { computed, ref } from 'vue';
import { RouterLink, RouterView, useRoute } from 'vue-router';
import { Dialog, DialogPanel, TransitionChild, TransitionRoot } from '@headlessui/vue';
import { XMarkIcon } from '@heroicons/vue/24/outline';

import { isInitialized, user } from '@/store/user.ts';
import LoadingScreen from '@/layout/LoadingScreen.vue';
import AppLogoSquare from '@/components/icons/AppLogoSquare.vue';
import Backdrop from '@/layout/Backdrop.vue';
import Sidebar from '@/layout/Sidebar/Sidebar.vue';

const route = useRoute();

const sidebarOpen = ref(false);

const userDisplayName = computed(() => user.value?.display_name || user.value?.username || '');
</script>

<template>
	<div class="absolute w-available h-available overflow-hidden isolate flex flex-col">
		<Backdrop class="opacity-40" />

		<LoadingScreen v-if="!isInitialized">
			<h2 class="text-3xl font-bold tracking-tight text-white h-12">
				{{ $t('layout.welcomeBack', { name: userDisplayName }) }}
			</h2>
			<div
				class="relative flex h-8 w-8 animate-spin rounded-full border-4 border-solid border-current border-r-transparent align-[-0.125em] motion-reduce:animate-[spin_1.5s_linear_infinite]"
				role="status"
			>
				<span
					class="!absolute !-m-px !h-px !w-px !overflow-hidden !whitespace-nowrap !border-0 !p-0 ![clip:rect(0,0,0,0)]"
				>
					{{ $t('auth.loading') }}
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
										<ul class="flex flex-1 flex-col gap-y-7">
											<li>
												<ul class="-mx-2 space-y-1">
													<li v-for="item in navigation" :key="item.name">
														<RouterLink
															:class="[
																route.path === item.path
																	? 'bg-neutral-800 text-white'
																	: 'text-neutral-400 hover:bg-neutral-800 hover:text-white',
															]"
															:to="item.path"
															class="group flex gap-x-3 rounded-md p-2 text-sm/6 font-semibold"
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
			<Sidebar />

			<main class="lg:pl-72 h-px flex-1 flex flex-col overflow-clip sm:overflow-auto">
				<div class="h-available flex flex-col overflow-auto">
					<RouterView :key="route.path" />
				</div>
			</main>
		</template>
	</div>
</template>
