export interface EventSubscription {
	id: string;
	provider: string;
	eventType: string;
	enabled: boolean;
	subscriptionId?: string;
	callbackUrl?: string;
	createdAt: string;
	updatedAt: string;
	expiresAt?: string;
	metadata?: Record<string, string>;
}

export interface EventTypeInfo {
	eventType: string;
	description?: string;
	subscriptionId?: string;
	enabled?: boolean;
}

export interface Event {
	id: string;
	provider: string;
	event_type: string;
	enabled: boolean;
	subscription_id: null;
	callback_url: string;
	created_at: Date;
	updated_at: Date;
	expires_at: null;
	metadata_json: null;
	metadata: any;
}
