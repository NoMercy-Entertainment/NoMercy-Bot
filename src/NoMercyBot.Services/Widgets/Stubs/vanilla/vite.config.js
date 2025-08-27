import {fileURLToPath, URL} from 'node:url';

import {defineConfig} from 'vite';

export default defineConfig({
    plugins: [],
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
