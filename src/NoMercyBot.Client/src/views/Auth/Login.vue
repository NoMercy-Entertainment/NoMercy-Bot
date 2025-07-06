<script lang="ts" setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';

import authService from '@/services/authService';
import { storeUser } from '@/store/user';
import router from '@/router';

import LoadingScreen from '../../layout/LoadingScreen.vue';
import LoginHeader from './components/LoginHeader.vue';
import AuthButton from '@/views/Auth/components/AuthButton.vue';
import LoginFooter from '@/views/Auth/components/LoginFooter.vue';
import ProviderSetup from '@/views/Auth/components/ProviderSetup.vue';
import { providerColor, providerIcon } from '@/lib/ui.ts';
import { useSessionStorage } from '@vueuse/core';
import {ConfigurationStatus} from "@/types/providers.ts";

const route = useRoute();
const isLoading = ref(false);
const errorMessage = ref('');
const redirect = useSessionStorage('redirect', '/home');
const providerConfiguration = ref<ConfigurationStatus>(<ConfigurationStatus>{});
const isCheckingConfig = ref(true);

const theme = computed(() => providerColor(route.params.provider as string));
const icon = computed(() => providerIcon(route.params.provider as string));

watch(theme, (value) => {
    console.log(`Setting theme color to: ${value[0]}`);
    document.documentElement.style.setProperty('--theme', value[0]);
});

async function checkProviderConfiguration() {
    isCheckingConfig.value = true;
    
    // Only perform this check for Twitch as it's the main provider that needs configuration
    if (route.params.provider === 'twitch') {
        try {
            providerConfiguration.value = await authService.getProviderConfigStatus(route.params.provider as string);
        } catch (error) {
            console.error('Error checking provider configuration:', error);
        }
    } else {
        // For other providers, assume they're configured
        providerConfiguration.value = {
          isConfigured: true,
        }
    }
    
    isCheckingConfig.value = false;
}

function startAuth() {
    if (isLoading.value)
        return;

    errorMessage.value = '';
    isLoading.value = true;
    window.location.href = `/api/oauth/${route.params.provider}/login`;
}

async function handleCallback() {
    const code = route.query.code as string;
    const error = route.query.error as string;

    if (error) {
        errorMessage.value = error;
        await router.replace({ query: {} });
        return;
    }

    if (!code)
        return;

    try {
        isLoading.value = true;

        const response = await authService.callback(<string>route.params.provider, code);
        if (response.user) {
            storeUser(<string>route.params.provider, response.user);
            await router.replace(redirect.value);
        }
    } catch (error: any) {
        const detail = error?.response?.data?.detail
            ? JSON.parse(error.response.data.detail)
            : null;

        errorMessage.value = detail?.message || detail?.error_description || error?.message || 'Authentication failed';
    } finally {
        isLoading.value = false;
    }
}

onMounted(() => {
    document.documentElement.style.setProperty('--theme', theme.value[0]);
    checkProviderConfiguration().then(() => {
        if (providerConfiguration.value.isConfigured) {
            handleCallback();
        }
    });
});

function handleProviderSetupComplete() {
    providerConfiguration.value.isConfigured = true;
    // Give the system a moment to register the configuration
    setTimeout(() => {
        startAuth();
    }, 1000);
}

const isProcessingAuth = computed(() => {
    return route.name === 'Login' && (!!route.query.code || !!route.query.error);
});

function retry() {
    errorMessage.value = '';
    isLoading.value = false;
    startAuth();
}

function handleBack() {
    if (route.name === 'Login') {
        router.push({ name: 'Provider Settings', params: { provider: route.params.provider } });
    } else {
        router.back();
    }
}
</script>

<template>
    <LoadingScreen>
        <LoginHeader 
            :is-processing-auth="isProcessingAuth" 
            :hide-header-content="!providerConfiguration.isConfigured && !isCheckingConfig" 
        />

        <div v-if="errorMessage" class="mt-4 text-center flex flex-col gap-2 w-full">
            <div class="rounded-md bg-red-900/50 p-4">
                <div class="flex">
                    <div class="flex-shrink-0">
                        <svg class="h-5 w-5 text-red-400" fill="currentColor" viewBox="0 0 20 20">
                            <path clip-rule="evenodd"
                                  d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z"
                                  fill-rule="evenodd"
                            />
                        </svg>
                    </div>
                    <span class="text-sm font-medium text-red-400 whitespace-nowrap flex-1">
                        {{ errorMessage }}
                    </span>
                </div>
            </div>
            <AuthButton
                :text="$t('auth.login.retry', { provider: route.params.provider })"
                :theme="theme"
                @click="retry" />
        </div>

        <div v-else-if="isCheckingConfig" class="mt-8 text-center">
            <div
                class="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-theme-400 border-r-transparent align-[-0.125em] motion-reduce:animate-[spin_1.5s_linear_infinite]"
            />
            <p class="mt-4 text-sm text-neutral-400">
                {{ $t('auth.login.checking') }}...
            </p>
        </div>

        <div v-else-if="!providerConfiguration.isConfigured" class="mt-8 w-full">
            <ProviderSetup 
                :providerConfiguration="providerConfiguration"
                :provider="route.params.provider as string"
                @setup-complete="handleProviderSetupComplete"
            />
        </div>

        <div v-else-if="isLoading" class="mt-8 text-center">
            <div
                class="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-theme-400 border-r-transparent align-[-0.125em] motion-reduce:animate-[spin_1.5s_linear_infinite]"
            />
            <p class="mt-4 text-sm text-neutral-400">
                {{ $t('auth.login.connecting', { provider: route.params.provider }) }}...
            </p>
        </div>

        <div v-else-if="!isProcessingAuth" class="mt-8 w-full gap-2 flex flex-col items-center">
            <AuthButton
                :disabled="isProcessingAuth"
                :icon="icon"
                :text="$t('auth.login.connect', { provider: route.params.provider })"
                :theme="theme"
                @click="startAuth"
            />
            <AuthButton v-if="route.params.provider !== 'twitch'"
                        :disabled="isProcessingAuth"
                        :text="$t('auth.login.back')"
                        :theme="['back', '#fff']"
                        icon="M10.586 16.586L5.172 11.172a1 1 0 010-1.414l5.414-5.414a1 1 0 011.414 1.414L8.414 9H18a1 1 0 110 2H8.414l3.586 3.586a1 1 0 01-1.414 1.414z"
                        @click="handleBack"
            />
        </div>

        <LoginFooter />
    </LoadingScreen>
</template>
