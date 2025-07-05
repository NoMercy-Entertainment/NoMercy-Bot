export interface AxiosResponse<T> {
	data: T;
	status: number;
	statusText: string;
	headers: any;
	config: any;
	request?: any;
}

export interface RequestTokenData {
	grant_type: string;
	client_id: string;
	client_secret: string;
	refresh_token: string;
	scope: string;
}

export interface TokenData {
	access_token: string;
	refresh_token: string;
	expires_in: number;
	token_type: string;
	scope: string;
}

export interface User {
	access_token: string;
	refresh_token: string;
	token_expiry: Date;
	id: string;
	username: string;
	display_name: string;
	timezone: null;
	profile_image_url: string;
	offline_image_url: string;
	color: string;
	link: string;
	enabled: boolean;
	is_live: boolean;
}
