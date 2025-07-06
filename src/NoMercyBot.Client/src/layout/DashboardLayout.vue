<script lang="ts" setup>
import { MagnifyingGlassIcon } from '@heroicons/vue/20/solid';
import ProviderLogo from '@/components/icons/ProviderLogo.vue';
import { useRoute } from 'vue-router';

defineProps({
	title: {
		type: String,
		required: true,
	},
	description: {
		type: String,
		required: true,
	},
	icon: {
		type: Object,
		default: null,
	},
	showSearch: {
		type: Boolean,
		default: false,
	},
});
const route = useRoute();
</script>

<template>
	<div class="h-inherit flex flex-col mb-auto w-full">
		<!-- Search bar (optional) -->
		<div v-if="showSearch"
			class="sticky top-0 z-40 flex h-16 shrink-0 items-center gap-x-6 border-b border-white/5 bg-neutral-900/50 px-4 shadow-xs sm:px-6 lg:px-8"
		>
			<div class="flex flex-1 gap-x-4 self-stretch lg:gap-x-6">
				<form action="#" class="grid flex-1 grid-cols-1" method="GET">
					<input aria-label="Search"
						class="col-start-1 row-start-1 block size-full bg-transparent pl-8 text-base text-white outline-hidden placeholder:text-neutral-300 sm:text-sm"
						name="search"
						placeholder="Search"
						type="search"
					>
					<MagnifyingGlassIcon
						aria-hidden="true"
						class="pointer-events-none col-start-1 row-start-1 size-5 self-center text-neutral-300"
					/>
				</form>
			</div>
		</div>

		<!-- Page header -->
		<header class="border-b border-white/5 w-full sticky top-0 h-24">
			<div class="flex items-center px-8 h-full">
				<ProviderLogo v-if="route.params?.provider"
					:provider="route.params?.provider as string"
					class-name="h-10 w-10"
				/>
				<div class="flex flex-col">
					<h1 class="text-base/7 font-semibold text-white px-8 pt-4 pb-2">
						{{ title }}
					</h1>
					<p class="text-neutral-400 px-8 pb-6">
						{{ description }}
					</p>
				</div>
			</div>
		</header>

		<!-- Main content area -->
		<div class="absolute top-0 mt-24 w-available mx-auto overflow-auto h-available">
			<slot />
		</div>
	</div>
</template>

<style scoped>
/* Add any specific styling if needed */
</style>
