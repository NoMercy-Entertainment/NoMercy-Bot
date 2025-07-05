/// <reference types="vite/client" />

declare module '*.svg' {
	const src: string;
	export default src;
}
declare module '*.svg?import' {
	const src: string;
	export default src;
}
declare module '*.svg?url' {
	const src: string;
	export default src;
}
declare module '*.svg?raw' {
	const src: string;
	export default src;
}
declare module '*.svg?inline' {
	const src: string;
	export default src;
}
declare module '*.scss';
declare module '*.jpg';
declare module '*.webp';
declare module '*.png';
declare module '*.gif';

declare module '@vue/runtime-core' {
	interface ComponentCustomProperties {
		$keycloak: VueKeycloakInstance;
		$t: (key?: string, args?: unknown) => string;
	}
}
