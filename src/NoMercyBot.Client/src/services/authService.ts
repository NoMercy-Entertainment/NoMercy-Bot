import axios from 'axios';

import type { User } from '@/types/auth';

import serverClient from '@/lib/clients/serverClient';
import { clearUserSession, storeUser, user } from '@/store/user';

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
		const response = await serverClient().get<{ user: User }>(`oauth/${provider}/validate`, {
			headers: {
				Authorization: `Bearer ${user.value?.access_token}`,
			},
		});

		if (user.value?.token_expiry) {
			this.scheduleTokenRefresh(provider, user.value?.token_expiry);
		}

		return response.data;
	}

	async refreshToken(provider: string): Promise<{ user: User }> {
		const response = await serverClient().post<{ user: User }>(`oauth/${provider}/refresh`, {
			refresh_token: localStorage.getItem('refresh_token'),
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
