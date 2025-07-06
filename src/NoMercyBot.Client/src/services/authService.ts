import axios from 'axios';

import type { User } from '@/types/auth';

import serverClient from '@/lib/clients/serverClient';
import { clearUserSession, getProviderUser, storeUser } from '@/store/user';
import type { ConfigurationStatus } from '@/types/providers.ts';

class AuthService {
	private refreshTimer: number | null = null;
	private readonly REFRESH_BUFFER = 5 * 60 * 1000;

	async authorize(provider: string) {
		const response = await serverClient().get(`oauth/${provider}/authorize`);
		return response.data;
	}

	async pollToken(provider: string, deviceCode: string) {
		const response = await axios.post(`oauth/${provider}/poll`, {
			device_code: deviceCode,
		});
		return response.data;
	}

	async callback(provider: string, code: string): Promise<{ user: User }> {
		const response = await serverClient().get<{ user: User }>(`oauth/${provider}/callback`, {
			params: { code },
		});

		if (response.data.user.token_expiry) {
			this.scheduleTokenRefresh(provider, response.data.user.token_expiry);
		}

		return response.data;
	}

	async validateSession(provider: string): Promise<{ user: User }> {
		const response = await serverClient().post<{ user: User }>(`oauth/${provider}/validate`, {
			AccessToken: getProviderUser(provider).access_token,
		});

		if (getProviderUser(provider)?.token_expiry) {
			this.scheduleTokenRefresh(provider, getProviderUser(provider).token_expiry);
		}

		return response.data;
	}

	async refreshToken(provider: string): Promise<{ user: User }> {
		const response = await serverClient().post<{ user: User }>(`oauth/${provider}/refresh`, {
			RefreshToken: getProviderUser(provider).refresh_token,
		});

		return response.data;
	}

	async logout(provider: string) {
		clearUserSession(provider);
	}

	async deleteAccount(provider: string) {
		await serverClient().delete(`oauth/${provider}/account`);
		clearUserSession(provider);
	}

	async getProviderConfigStatus(provider: string): Promise<ConfigurationStatus | undefined> {
		try {
			const response = await serverClient().get<ConfigurationStatus>(`oauth/${provider}/config-status`);
			return response.data;
		}
		catch (error) {
			console.error('Failed to check provider config status:', error);
			return undefined;
		}
	}

	async configureProvider(provider: string, config: {
		clientId: string;
		clientSecret: string;
		scopes: string[];
	}): Promise<boolean> {
		try {
			const response = await serverClient().post<{ success: boolean }, ConfigurationStatus>(`oauth/${provider}/configure`, {
				clientId: config.clientId,
				clientSecret: config.clientSecret,
				scopes: config.scopes,
			});
			return response.data?.success;
		}
		catch (error) {
			console.error('Failed to configure provider:', error);
			return false;
		}
	}

	private scheduleTokenRefresh(provider: string, expiryTime: Date) {
		if (this.refreshTimer) {
			window.clearTimeout(this.refreshTimer);
		}

		const expiry = new Date(expiryTime).getTime();
		const now = Date.now();
		const timeUntilRefresh = expiry - now - this.REFRESH_BUFFER;

		if (timeUntilRefresh > 0) {
			this.refreshTimer = window.setTimeout(async () => {
				const response = await this.refreshToken(provider);

				storeUser(provider, response.user);

				if (response.user.token_expiry) {
					this.scheduleTokenRefresh(provider, response.user.token_expiry);
				}
			}, timeUntilRefresh);
		}
	}
}

export default new AuthService();
