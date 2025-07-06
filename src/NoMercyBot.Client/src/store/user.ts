import { computed, ref } from 'vue';
import { useLocalStorage } from '@vueuse/core';

import authService from '@/services/authService';
import router from '@/router';

import type { User } from '@/types/auth';

const twitchUser = useLocalStorage<User>('twitchUser', <User>{});
const spotifyUser = useLocalStorage<User>('spotifyUser', <User>{});

const discordUser = useLocalStorage<User>('discordUser', <User>{});
const obsUser = useLocalStorage<User>('obsUser', <User>{});

export const userProviders = {
	twitch: twitchUser,
	spotify: spotifyUser,
	discord: discordUser,
	obs: obsUser,
};

export const user = computed(() => twitchUser.value);

export function getProviderUser(provider: string) {
	switch (provider) {
		case 'twitch':
			return twitchUser.value;
		case 'spotify':
			return spotifyUser.value;
		case 'discord':
			return discordUser.value;
		case 'obs':
			return obsUser.value;
		default:
			console.warn(`Unknown provider: ${provider}`);
			return twitchUser.value;
	}
}

if (user.value?.color) {
	document.documentElement.style.setProperty('--theme', user.value?.color);
}

export const isInitialized = ref(false);

export function storeUser(provider: string, user: User) {
	switch (provider) {
		case 'twitch':
			twitchUser.value = user;
			document.documentElement.style.setProperty('--theme', user.color);
			break;
		case 'spotify':
			spotifyUser.value = user;
			break;
		case 'discord':
			discordUser.value = user;
			break;
		case 'obs':
			obsUser.value = user;
			break;
		default:
			console.warn(`Unknown provider: ${provider}`);
	}
}

export function clearUserSession(provider: string) {
	switch (provider) {
		case 'twitch':
			twitchUser.value = null;
			document.documentElement.style.removeProperty('--theme');
			break;
		case 'spotify':
			spotifyUser.value = null;
			break;
		case 'discord':
			discordUser.value = null;
			break;
		case 'obs':
			obsUser.value = null;
			break;
		default:
			console.warn(`Unknown provider: ${provider}`);
	}
}

export async function initializeUserSession(provider = 'twitch') {
	try {
		if (!getProviderUser(provider)?.access_token) {
			return;
		}

		const data = await authService.validateSession(provider);
		storeUser(provider, data.user);
	}
	catch (error) {
		console.error(error);
		clearUserSession(provider);
		await router.push({ name: 'Login', params: { provider } });
	}
	finally {
		isInitialized.value = true;
	}
}
