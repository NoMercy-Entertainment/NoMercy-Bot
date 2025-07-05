<script lang="ts" setup>
import type { PropType } from 'vue';
import { ref } from 'vue';
import type { RouteRecordRaw } from 'vue-router';
import SidebarButton from '@/layout/Sidebar/SidebarButton.vue';
import { MoooomIcons } from '@Icons/icons.ts';
import MoooomIcon from '@/components/icons/MoooomIcon.vue';

const props = defineProps({
	route: {
		type: Object as PropType<RouteRecordRaw>,
		required: true,
	},
});

const open = ref<boolean>(true);

const toggle = () => (open.value = !open.value);
</script>

<template>
	<div class="space-y-1">
		<button
			class="group flex gap-x-3 rounded-md p-2 text-sm/6 font-semibold w-full text-neutral-400 hover:bg-neutral-800 hover:text-white"
			@click="toggle"
		>
			<MoooomIcon v-if="!!route.meta?.icon" :icon="route.meta?.icon as keyof typeof MoooomIcons"
				class="size-6 shrink-0"
			/>
			<span>
				{{ route.name as string }}
			</span>

			<MoooomIcon :class="{
					'rotate-90': open,
				}"
				class="size-6 shrink-0 transition-transform duration-200 ml-auto"
				icon="chevronRight"
			/>
		</button>
		<div
			:class="{
				'grid-rows-1': open,
				'grid-rows-[repeat(1,minmax(0,0fr))]': !open,
			}"
			:inert="!open"
			class="grid h-auto w-full transition-all duration-200"
		>
			<div
				class="flex flex-col gap-1 overflow-hidden transition-all duration-200 p-0.5"
			>
				<div v-for="child in route.children ?? []" :key="child.name" class="ml-6 space-y-1">
					<SidebarButton v-if="!child.meta?.collapsible as boolean"
						:href="`${route.path}${child.path !== '' ? `/${child.path}` : ''}`"
						:icon="child.meta?.icon as keyof typeof MoooomIcons"
						:name="child.name as string"
					/>

					<SidebarButtonGroup v-else
						:route="child"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<style scoped>

</style>
