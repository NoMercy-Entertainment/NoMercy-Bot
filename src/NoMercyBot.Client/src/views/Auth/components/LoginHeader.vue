<script lang="ts" setup>
import { useRoute } from 'vue-router';

import { useTranslation } from 'i18next-vue';

defineProps({
	isProcessingAuth: {
		type: Boolean,
		default: false,
	},
});

const { t } = useTranslation();

const route = useRoute();

function dynamicT(key: string) {
	return t(key as any);
}
</script>

<template>
	<h2 class="text-3xl font-bold tracking-tight text-white">
		{{ dynamicT(`providers.${route.params.provider}.welcome`) }}
	</h2>
	<p class="mt-2 text-sm text-neutral-400">
		{{ dynamicT(`providers.${route.params.provider}.description`) }}
	</p>
	<p class="mt-2 text-sm text-neutral-400">
		{{
			isProcessingAuth
				? $t('auth.login.processing')
				: $t('auth.login.pleaseSignIn')
		}}
	</p>
</template>

<style scoped>

</style>
