import {fileURLToPath, URL} from 'node:url';

import {defineConfig} from 'vite';
import vue from '@vitejs/plugin-vue';
import vueDevTools from 'vite-plugin-vue-devtools';
import tailwindcss from '@tailwindcss/vite';
import {ViteCspPlugin} from 'vite-plugin-csp';
import path from 'node:path';

// https://vite.dev/config/
export default defineConfig({
    plugins: [
        tailwindcss(),
        vue(),
        vueDevTools(),
        ViteCspPlugin(
            {
                'base-uri': ['self'],
                'object-src': ['self', 'blob:'],
                'script-src': [
                    'self',
                    'unsafe-eval',
                    'unsafe-inline',
                    'unsafe-hashes',
                    'https://www.youtube.com',
                    'https://nomercy.tv',
                    'http://localhost:5503',
                    'https://static.cloudflareinsights.com',
                ],
                'style-src-attr': ['unsafe-inline'],
                'style-src': ['self', 'unsafe-inline', 'unsafe-eval', 'https://fonts.bunny.net', 'https://rsms.me'],
                'img-src': [
                    'self',
                    'blob:',
                    'data:',
                    'https://nomercy.tv',
                    'https://*.nomercy.tv:*',
                    'https://*.nomercy.tv',
                    'https://static-cdn.jtvnw.net',
                    'https://*.spotify.com/*',
                    'https://i.ytimg.com',
                    'https://pub-a68768bb5b1045f296df9ea56bd53a7f.r2.dev',
                    'wss://*.nomercy.tv:*',
                ],
                'connect-src': [
                    'self',
                    'blob:',
                    'data:',
                    'https://nomercy.tv',
                    'https://*.nomercy.tv:*',
                    'https://*.nomercy.tv',
                    'https://*.spotify.com',
                    'ws://*.nomercy.tv:*',
                    'wss://*.nomercy.tv:*',
                    'ws://localhost:*',
                    'https://pub-a68768bb5b1045f296df9ea56bd53a7f.r2.dev',
                    'https://raw.githubusercontent.com',
                ],
                'frame-src': ['self', 'https://nomercy.tv', 'https://*.nomercy.tv:*', 'https://www.youtube.com'],
                'font-src': ['self', 'blob:', 'data:', 'https://fonts.bunny.net', 'https://rsms.me'],
                'media-src': [
                    'self',
                    'blob:',
                    'data:',
                    'https://nomercy.tv',
                    'https://*.nomercy.tv',
                    'https://*.nomercy.tv:*',
                    'wss://*.nomercy.tv:*',
                    'https://pub-a68768bb5b1045f296df9ea56bd53a7f.r2.dev',
                ],
                'worker-src': ['self', 'blob:'],
            },
            {
                enabled: true,
                hashingMethod: 'sha256',
                hashEnabled: {
                    'script-src': false,
                    'style-src': false,
                    'script-src-attr': false,
                    'style-src-attr': false,
                },
                nonceEnabled: {
                    'script-src': false,
                    'style-src': false,
                },
                // processFn: 'Nginx',
            },
        ),
    ],
    server: {
        host: '0.0.0.0',
        port: 6038,
        hmr: {
            // port: 6038,
        },
        allowedHosts: [],
        proxy: {
            '/api': {
                target: 'http://localhost:6037',
                changeOrigin: true,
                secure: false,
            },
        },
    },
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
            '@Icons': path.resolve(__dirname, './resources/icons'),
        },
    },
    css: {
        preprocessorOptions: {
            scss: {
                api: 'modern-compiler',
            },
        },
    },
    base: '/',
});
