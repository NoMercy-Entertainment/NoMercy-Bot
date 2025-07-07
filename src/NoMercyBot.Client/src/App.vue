<script lang="ts" setup>
import {onMounted} from 'vue';
import {useRoute} from 'vue-router';

import {initializeUserSession} from '@/store/user';

const route = useRoute();

onMounted(async () => {
  await initializeUserSession(route.params.provider);
});
</script>

<template>
  <router-view/>
</template>

<style lang="scss" scoped>
</style>
