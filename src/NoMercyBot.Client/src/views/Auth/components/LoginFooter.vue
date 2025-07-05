<script lang="ts" setup>
import { useRoute } from 'vue-router';
import { routePaths } from '@/router/routes.ts';

const route = useRoute();
</script>

<template>
	<div v-if="route.params.provider === 'twitch'" class="mt-6 text-center text-sm text-neutral-400">
		{{ $t('auth.login.terms') }}<br>
		<RouterLink :to="routePaths.terms_of_service" class="font-medium text-theme-500 hover:text-theme-300">
			{{ $t('auth.login.termsLink') }}
		</RouterLink>
		{{ $t('auth.login.and') }}
		<RouterLink :to="routePaths.privacy_policy" class="font-medium text-theme-500 hover:text-theme-300">
			{{ $t('auth.login.privacyLink') }}
		</RouterLink>
	</div>
</template>

<style scoped>

</style>
