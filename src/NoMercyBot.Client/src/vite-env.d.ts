/// <reference types="vite/client" />

import type { CompilerOptions } from '@vue/compiler-dom';
import type { RenderFunction } from '@vue/runtime-dom';

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

declare module 'vue' {
	function compileToFunction(template: string | HTMLElement, options?: CompilerOptions): RenderFunction;
}

type DotPrefix<T extends string> = T extends '' ? '' : `.${T}`;
type DotNestedKeys<T, Depth extends number = 4> = [Depth] extends [never]
	? never
	: T extends object
		? {
				[K in keyof T]: K extends string ? `${K}${DotPrefix<DotNestedKeys<T[K], Prev[Depth]>>}` : never;
			}[keyof T]
		: '';
type Prev = [never, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

type Translations = typeof import('../public/locales/en/translation.json');
type TranslationPath = DotNestedKeys<Translations>;

declare module '@vue/runtime-core' {
	interface ComponentCustomProperties {
		$t: (key: TranslationPath, args?: Record<string, any>) => string;
	}
}
