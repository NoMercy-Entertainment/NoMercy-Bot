export interface Provider {
	id: string;
	name: string;
	link: string;
	enabled: boolean;
	clientId: null | string;
	clientSecret: null | string;
	scopes: string[];
	accessToken: null | string;
	refreshToken: null | string;
	tokenExpiry: Date | null;
	created_at: Date;
	updated_at: Date;
	availableScopes: {
		[key: string]: string;
	};
}

export interface ConfigurationStatus {
	isConfigured: boolean;
	name: string;
	enabled: boolean;
	clientId: string;
	clientSecret: string;
}
