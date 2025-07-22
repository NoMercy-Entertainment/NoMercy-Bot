import { fileURLToPath, URL } from 'node:url';

import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [react()],
	base: './',
	resolve: {
		alias: {
			'@': fileURLToPath(new URL('./src', import.meta.url)),
		},
	},
	build: {
		outDir: '../dist',
		target: 'ES2022',
		emptyOutDir: true,
		minify: 'esbuild',
		cssMinify: 'esbuild',
		rollupOptions: {
			input: {
				main: 'index.html',
			},
		},
	},
});
